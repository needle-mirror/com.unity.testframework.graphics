using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    sealed class AssetBundleBuilder : IPlayerContentBuilder, IPerPlatformBundleSource
    {
        const string k_AssetBundlePath = "Assets/StreamingAssets";

        readonly List<(string BundleName, GraphicsTestPlatform Platform)> m_BuiltBundles = new();

        public IEnumerable<(string BundleName, GraphicsTestPlatform Platform)> BuiltBundles => m_BuiltBundles;

        public IEnumerable<string> BuildContent(
            IList<GraphicsTestCase> testCases,
            IEnumerable<GraphicsTestPlatform> platforms,
            BuildTarget buildTarget
        )
        {
            var tracker = new ReferenceImageDedupTracker();
            m_BuiltBundles.Clear();

            var groups = new List<ResolvedImageGroup>();
            var groupsByDirectory = new Dictionary<string, ResolvedImageGroup>();

            var platformList = platforms as IReadOnlyList<GraphicsTestPlatform>
                ?? new List<GraphicsTestPlatform>(platforms);
            var fallbackSchema = platformList.Count > 0 ? platformList[platformList.Count - 1]?.Schema : null;

            foreach (var platform in platformList)
            {
                GraphicsTestLogger.Log($"Searching for reference images for platform {platform}...");
                tracker.BeginPlatform(platform, SameSchemaFamily(platform?.Schema, fallbackSchema));

                var filteredTestCases = new List<GraphicsTestCase>();
                foreach (var tc in testCases)
                {
                    if (tc.ReferenceImageDescriptor == null)
                    {
                        throw new InvalidOperationException($"Test case '{tc.Name}' has null ReferenceImageDescriptor. This is a bug in test setup.");
                    }

                    if (tracker.ShouldCollect(tc))
                    {
                        filteredTestCases.Add(tc);
                    }
                }

                var images = ReferenceImageUtility.Default.CollectReferenceImagePathsFor(filteredTestCases, platform);
                var newImages = tracker.FilterNewImages(images);

                GroupImagesByResolvedPath(platform, newImages, groups, groupsByDirectory);
            }

            var groupSchemata = new List<PlatformSchema>(groups.Count);
            foreach (var group in groups)
                groupSchemata.Add(group.Platform?.Schema);
            var disambiguators = ComputeBundleDisambiguators(groupSchemata);
            LogDisambiguators(disambiguators);

            var assetBundlesToBuild = new List<AssetBundleBuild>();
            foreach (var group in groups)
            {
                disambiguators.TryGetValue(group.Platform.Schema, out var disambiguator);
                foreach (var bundle in GetAssetBundlesForPlatform(group.Images, group.Platform, disambiguator))
                {
                    assetBundlesToBuild.Add(bundle);
                    m_BuiltBundles.Add((bundle.assetBundleName, group.Platform));
                }
            }

            if (assetBundlesToBuild.Count == 0)
            {
                GraphicsTestLogger.Log(LogType.Warning, "No reference images found for any platform.");
                return Array.Empty<string>();
            }

            var bundleNames = new string[assetBundlesToBuild.Count];
            for (var i = 0; i < assetBundlesToBuild.Count; i++)
                bundleNames[i] = assetBundlesToBuild[i].assetBundleName;
            GraphicsTestLogger.Log(
                $"Will build {assetBundlesToBuild.Count} reference image bundles:"
                    + string.Join(Environment.NewLine, bundleNames)
            );
            BuildAssetBundles(assetBundlesToBuild, buildTarget);

            return bundleNames;
        }

        public void CleanUp()
        {
            // Nothing to clean up
        }

        /// <summary>
        /// Whether two schemata are the same family: same name and root path (combination expansion
        /// appends node types but keeps both). The last platform's family is the universal fallback
        /// and always collects (see <see cref="ReferenceImageDedupTracker.BeginPlatform"/>).
        /// </summary>
        internal static bool SameSchemaFamily(PlatformSchema a, PlatformSchema b) =>
            a != null && b != null && a.name == b.name && a.rootPath == b.rootPath;

        /// <summary>
        /// One bundle-worth of reference images that all resolved from the same folder, tagged with
        /// the platform level that folder encodes.
        /// </summary>
        internal sealed class ResolvedImageGroup
        {
            internal GraphicsTestPlatform Platform;
            internal readonly Dictionary<string, string> Images = new();
        }

        /// <summary>
        /// Sorts each resolved image into the group for the folder it was found in. The group's
        /// platform tag comes from that folder, not from the platform that searched for the image: an
        /// image in a shared fallback folder must not carry characteristics its folder does not assert.
        /// </summary>
        internal static void GroupImagesByResolvedPath(
            GraphicsTestPlatform platform,
            Dictionary<string, string> images,
            List<ResolvedImageGroup> groups,
            Dictionary<string, ResolvedImageGroup> groupsByDirectory
        )
        {
            foreach (var pair in images)
            {
                var directory = Path.GetDirectoryName(pair.Value)?.Replace('\\', '/') ?? string.Empty;
                if (!groupsByDirectory.TryGetValue(directory, out var group))
                {
                    var levelPlatform = platform.ForResultsPath(directory);
                    if (levelPlatform == null)
                    {
                        GraphicsTestLogger.Log(
                            LogType.Warning,
                            $"Reference image '{pair.Value}' does not sit in any results path of "
                                + $"platform {platform}; tagging its bundle with the full platform."
                        );
                        levelPlatform = platform;
                    }

                    group = new ResolvedImageGroup { Platform = levelPlatform };
                    groupsByDirectory.Add(directory, group);
                    groups.Add(group);
                }

                group.Images.Add(pair.Key, pair.Value);
            }
        }

        internal IEnumerable<AssetBundleBuild> GetAssetBundlesForPlatform(
            Dictionary<string, string> images,
            GraphicsTestPlatform platform,
            string disambiguator = ""
        )
        {
            var assetBundles = new List<AssetBundleBuild>();

            ReferenceImageUtility.Default.SetupReferenceImageImportSettings(images.Values);
            var imageLines = new List<string>();
            foreach (var pair in images)
                imageLines.Add($"{pair.Key} => {pair.Value}");
            GraphicsTestLogger.Log(
                LogType.Log,
                $"Found {images.Count} reference images for platform {platform}:\n" + string.Join("\n", imageLines)
            );

            if (images.Count > 0)
            {
                var sortedKeys = new List<string>(images.Keys);
                sortedKeys.Sort(StringComparer.Ordinal);

                const int k_BundleChunkSize = 8;
                var chunkNumber = 0;

                for (var startIndex = 0; startIndex < sortedKeys.Count; startIndex += k_BundleChunkSize)
                {
                    var chunkEnd = Math.Min(startIndex + k_BundleChunkSize, sortedKeys.Count);
                    var chunkCount = chunkEnd - startIndex;
                    var chunkKeys = new string[chunkCount];
                    var chunkValues = new string[chunkCount];
                    for (var j = 0; j < chunkCount; j++)
                    {
                        chunkKeys[j] = sortedKeys[startIndex + j];
                        chunkValues[j] = images[sortedKeys[startIndex + j]];
                    }

                    var name = BuildBundleName(platform.Schema.name, platform.Name, disambiguator, chunkNumber++);
                    assetBundles.Add(CreateAssetBundleBuild(chunkKeys, chunkValues, name));
                }
            }

            return assetBundles;
        }

        /// <summary>
        /// Composes {schemaName}-{platformName}[-{disambiguator}]-{chunk}. The chunk stays last:
        /// <see cref="TestContentBundle.Priority"/> parses it off the end.
        /// </summary>
        internal static string BuildBundleName(string schemaName, string platformName, string disambiguator, int chunk)
        {
            var namePart = (schemaName ?? string.Empty).ToLower().Replace(' ', '-');
            var disambiguatorPart = string.IsNullOrEmpty(disambiguator) ? string.Empty : disambiguator + "-";
            return (namePart + "-" + platformName + "-" + disambiguatorPart + chunk).Replace("--", "-");
        }

        /// <summary>
        /// Maps each schema to a bundle-name suffix, empty unless its name is shared by a schema that
        /// resolves elsewhere. Bundle names derive from the schema name alone, so those would collide.
        /// </summary>
        internal static Dictionary<PlatformSchema, string> ComputeBundleDisambiguators(
            IEnumerable<PlatformSchema> schemata
        )
        {
            var result = new Dictionary<PlatformSchema, string>();
            var byName = new Dictionary<string, List<PlatformSchema>>();
            foreach (var schema in schemata)
            {
                if (schema == null)
                    continue;
                var key = (schema.name ?? string.Empty).ToLower();
                if (!byName.TryGetValue(key, out var group))
                {
                    group = new List<PlatformSchema>();
                    byName[key] = group;
                }
                group.Add(schema);
            }

            foreach (var group in byName.Values)
            {
                var distinctResolutions = new HashSet<string>();
                foreach (var schema in group)
                    distinctResolutions.Add(StableResolutionHash(schema));

                var collides = distinctResolutions.Count > 1;
                foreach (var schema in group)
                    result[schema] = collides ? StableResolutionHash(schema) : string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Hashes root path and nodes. FNV-1a, not <see cref="string.GetHashCode"/>, which is
        /// randomized per process and would rename bundles on every build.
        /// </summary>
        internal static string StableResolutionHash(PlatformSchema schema)
        {
            var nodes = schema.nodes == null ? string.Empty : string.Join(",", schema.nodes);
            var identity = (schema.rootPath ?? string.Empty) + "\n" + nodes;

            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;
            var hash = offsetBasis;
            foreach (var c in identity)
            {
                hash ^= c;
                hash *= prime;
            }

            return hash.ToString("x8").Substring(0, 6);
        }

        static void LogDisambiguators(Dictionary<PlatformSchema, string> disambiguators)
        {
            var lines = new List<string>();
            foreach (var pair in disambiguators)
            {
                if (!string.IsNullOrEmpty(pair.Value))
                    lines.Add($"  {pair.Value} -> {pair.Key.rootPath}");
            }

            if (lines.Count == 0)
                return;

            lines.Sort(StringComparer.Ordinal);
            GraphicsTestLogger.Log(
                LogType.Warning,
                "Reference-image bundle names for same-named schemata were disambiguated by resolution hash:\n"
                    + string.Join("\n", lines)
                    + "\nRename the conflicting schema in its Image Build Settings or Graphics Test Build "
                    + "Settings asset so each schema name is unique."
            );
        }

        static AssetBundleBuild CreateAssetBundleBuild(string[] addressableNames, string[] assetNames, string name)
        {
            return new AssetBundleBuild
            {
                assetBundleName = name,
                addressableNames = addressableNames,
                assetNames = assetNames,
            };
        }

        void BuildAssetBundles(List<AssetBundleBuild> assetBundlesToBuild, BuildTarget buildPlatform)
        {
            if (!Directory.Exists(k_AssetBundlePath))
                Directory.CreateDirectory(k_AssetBundlePath);

            BuildPipeline.BuildAssetBundles(
                k_AssetBundlePath,
                assetBundlesToBuild.ToArray(),
                BuildAssetBundleOptions.None,
                buildPlatform
            );
        }
    }
}
