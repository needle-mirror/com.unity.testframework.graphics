using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Builds the test data declared with [RequireTestData] into AssetBundles for player builds.
    /// Declarations are grouped by logical bundle name across every test case; a declaration that
    /// resolves to nothing fails the build.
    /// </summary>
    sealed class TestDataBundleBuilder : IPlayerContentBuilder, ITestDataBundleSource
    {
        const string k_AssetBundlePath = "Assets/StreamingAssets";

        /// <summary>
        /// Builds the bundles and reports whether they were written. Settable so tests can exercise
        /// the failure path without driving a real AssetBundle build.
        /// </summary>
        internal static Func<string, AssetBundleBuild[], BuildAssetBundleOptions, BuildTarget, bool> TryBuildBundles =
            BuildBundlesWithPipeline;

        internal static void ResetBundleBuildCall() => TryBuildBundles = BuildBundlesWithPipeline;

        static bool BuildBundlesWithPipeline(
            string outputPath,
            AssetBundleBuild[] builds,
            BuildAssetBundleOptions options,
            BuildTarget buildTarget
        ) => BuildPipeline.BuildAssetBundles(outputPath, builds, options, buildTarget) != null;

        readonly List<(string BundleFileName, string LogicalName)> m_BuiltBundles = new();

        public IEnumerable<(string BundleFileName, string LogicalName)> BuiltTestDataBundles => m_BuiltBundles;

        public IEnumerable<string> BuildContent(
            IList<GraphicsTestCase> testCases,
            IEnumerable<GraphicsTestPlatform> platforms,
            BuildTarget buildTarget
        )
        {
            var bundleBuilds = PrepareBundleBuilds(testCases);
            if (bundleBuilds.Length == 0)
                return Array.Empty<string>();

            var bundleNames = new string[bundleBuilds.Length];
            for (var i = 0; i < bundleBuilds.Length; i++)
                bundleNames[i] = bundleBuilds[i].assetBundleName;

            GraphicsTestLogger.Log(
                $"Will build {bundleBuilds.Length} test data bundle(s):\n" + string.Join("\n", bundleNames)
            );

            if (!Directory.Exists(k_AssetBundlePath))
                Directory.CreateDirectory(k_AssetBundlePath);

            // Letting a failed build continue would hand the player a settings file naming content
            // that does not exist.
            if (!TryBuildBundles(k_AssetBundlePath, bundleBuilds, BuildAssetBundleOptions.None, buildTarget))
            {
                throw new InvalidOperationException(
                    "Failed to build the test data bundle(s):\n\t"
                        + string.Join("\n\t", bundleNames)
                        + "\nSee the preceding build errors for the cause."
                );
            }

            return bundleNames;
        }

        /// <summary>
        /// Resolves the declarations into AssetBundle builds, validating each; split from
        /// <see cref="BuildContent"/> for testability.
        /// </summary>
        /// <exception cref="InvalidOperationException">A declaration could not be resolved.</exception>
        internal AssetBundleBuild[] PrepareBundleBuilds(IEnumerable<GraphicsTestCase> testCases)
        {
            m_BuiltBundles.Clear();

            var descriptorsByBundle = CollectDescriptors(testCases);
            if (descriptorsByBundle.Count == 0)
                return Array.Empty<AssetBundleBuild>();

            var errors = new List<string>();
            var bundleBuilds = new List<AssetBundleBuild>();
            // Carried alongside: the file name is lower-cased, but the run time looks a bundle up by
            // its declared name and compares ordinally.
            var logicalNames = new List<string>();
            var logicalNamesByFileName = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var pair in descriptorsByBundle)
            {
                var logicalName = pair.Key;
                var descriptors = pair.Value;

                ValidateBundleName(logicalName, errors);

                // Bundle file names are case-insensitive while logical names are not; a case-only
                // clash would silently drop one bundle's runtime mapping.
                var bundleFileName = BuildBundleFileName(logicalName);
                if (logicalNamesByFileName.TryGetValue(bundleFileName, out var clashingName))
                {
                    errors.Add(
                        $"Test data bundle names '{clashingName}' and '{logicalName}' differ only by case "
                            + "and would produce the same bundle file."
                    );
                    continue;
                }
                logicalNamesByFileName[bundleFileName] = logicalName;

                // Addressable names inside a bundle are case-insensitive, so case-only variants
                // must collide here rather than producing an entry the run time cannot reach.
                var assetPathsByAddressableName = new SortedDictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var descriptor in descriptors)
                {
                    // Filesystem semantics apply only to the default descriptor's patterns; custom
                    // descriptors enumerate programmatically and are validated on their results.
                    if (descriptor is TestDataDescriptor)
                        ValidatePatterns(descriptor, errors);

                    var resolvedAny = false;
                    foreach (var assetPath in descriptor.GetAssetPaths())
                    {
                        resolvedAny = true;
                        if (!File.Exists(assetPath))
                        {
                            errors.Add(
                                $"Test data bundle '{logicalName}': resolved asset '{assetPath}' does not exist."
                            );
                            continue;
                        }

                        var addressableName = descriptor.GetAddressableName(assetPath);
                        if (
                            assetPathsByAddressableName.TryGetValue(addressableName, out var existingPath)
                            && !string.Equals(existingPath, assetPath, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            GraphicsTestLogger.LogWarning(
                                $"Test data bundle '{logicalName}' maps addressable name '{addressableName}' to "
                                    + $"both {existingPath} and {assetPath}; keeping the first."
                            );
                            continue;
                        }

                        assetPathsByAddressableName[addressableName] = assetPath;
                    }

                    // Checked per declaration, not per bundle: a declaration resolving to nothing
                    // must fail even when another declaration fills the shared bundle.
                    if (!resolvedAny)
                    {
                        errors.Add(
                            $"Test data bundle '{logicalName}' resolved to no assets. Declared patterns: "
                                + DescribePatterns(new List<ITestDataDescriptor> { descriptor })
                        );
                    }
                }

                if (assetPathsByAddressableName.Count == 0)
                    continue;

                bundleBuilds.Add(CreateBundleBuild(logicalName, assetPathsByAddressableName));
                logicalNames.Add(logicalName);
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Test data declared with [RequireTestData] could not be resolved; tests must always have "
                        + "access to their declared assets, so the build fails:\n\t"
                        + string.Join("\n\t", errors)
                );
            }

            for (var i = 0; i < bundleBuilds.Count; i++)
                m_BuiltBundles.Add((bundleBuilds[i].assetBundleName, logicalNames[i]));

            return bundleBuilds.ToArray();
        }

        public void CleanUp()
        {
            // Nothing to clean up
        }

        /// <summary>
        /// The distinct descriptors grouped by bundle name. Fixture-level declarations reach every
        /// test case of the fixture, so descriptors are deduplicated by reference first.
        /// </summary>
        internal static SortedDictionary<string, List<ITestDataDescriptor>> CollectDescriptors(
            IEnumerable<GraphicsTestCase> testCases
        )
        {
            var seen = new HashSet<ITestDataDescriptor>();
            var result = new SortedDictionary<string, List<ITestDataDescriptor>>(StringComparer.Ordinal);

            foreach (var testCase in testCases)
            {
                if (testCase.TestDataDescriptors == null)
                    continue;

                foreach (var descriptor in testCase.TestDataDescriptors)
                {
                    if (descriptor == null || !seen.Add(descriptor))
                        continue;

                    var logicalName = descriptor.BundleName ?? string.Empty;
                    if (!result.TryGetValue(logicalName, out var group))
                    {
                        group = new List<ITestDataDescriptor>();
                        result[logicalName] = group;
                    }

                    group.Add(descriptor);
                }
            }

            return result;
        }

        /// <summary>
        /// The bundle file name, e.g. "testdata-ssao-testdata-0". The trailing "-0" keeps
        /// <c>TestContentBundle.Priority</c> parsing consistent with reference image bundle names.
        /// </summary>
        internal static string BuildBundleFileName(string logicalName) =>
            $"testdata-{logicalName.ToLowerInvariant()}-0";

        AssetBundleBuild CreateBundleBuild(
            string logicalName,
            SortedDictionary<string, string> assetPathsByAddressableName
        )
        {
            var addressableNames = new string[assetPathsByAddressableName.Count];
            var assetPaths = new string[assetPathsByAddressableName.Count];
            var i = 0;
            foreach (var entry in assetPathsByAddressableName)
            {
                addressableNames[i] = entry.Key;
                assetPaths[i] = entry.Value;
                i++;
            }

            GraphicsTestLogger.Log(
                $"Test data bundle '{logicalName}' contains {assetPaths.Length} asset(s):\n\t"
                    + string.Join("\n\t", assetPaths)
            );

            var bundleFileName = BuildBundleFileName(logicalName);

            return new AssetBundleBuild
            {
                assetBundleName = bundleFileName,
                addressableNames = addressableNames,
                assetNames = assetPaths,
            };
        }

        static void ValidateBundleName(string logicalName, List<string> errors)
        {
            if (string.IsNullOrEmpty(logicalName))
            {
                errors.Add("A test data declaration has an empty bundle name.");
                return;
            }

            foreach (var c in logicalName)
            {
                if (char.IsLetterOrDigit(c) || c is '-' or '_')
                    continue;

                errors.Add(
                    $"Test data bundle name '{logicalName}' contains the invalid character '{c}'. Bundle "
                        + "names may contain letters, digits, '-' and '_'. Note that the first argument "
                        + "of [RequireTestData] is the bundle name, not an asset path."
                );
                return;
            }
        }

        static void ValidatePatterns(ITestDataDescriptor descriptor, List<string> errors)
        {
            foreach (var pattern in descriptor.AssetPatterns)
            {
                if (string.IsNullOrEmpty(pattern))
                {
                    errors.Add($"Test data bundle '{descriptor.BundleName}' declares an empty asset pattern.");
                    continue;
                }

                if (pattern.Contains('*'))
                {
                    var directory = Path.GetDirectoryName(pattern);
                    var searchPattern = Path.GetFileName(pattern);
                    if (
                        !Directory.Exists(directory)
                        || Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly).Length == 0
                    )
                    {
                        errors.Add(
                            $"Test data bundle '{descriptor.BundleName}': pattern '{pattern}' matches no files."
                        );
                    }
                }
                else if (!File.Exists(pattern))
                {
                    errors.Add(
                        $"Test data bundle '{descriptor.BundleName}': declared asset '{pattern}' does not exist."
                    );
                }
            }
        }

        static string DescribePatterns(List<ITestDataDescriptor> descriptors)
        {
            var patterns = new List<string>();
            foreach (var descriptor in descriptors)
                patterns.AddRange(descriptor.AssetPatterns);
            return patterns.Count > 0 ? string.Join(", ", patterns) : "(none)";
        }
    }
}
