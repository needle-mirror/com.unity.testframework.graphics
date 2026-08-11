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

            var assetBundlesToBuild = new List<AssetBundleBuild>();
            foreach (var group in groups)
            {
                foreach (var bundle in GetAssetBundlesForPlatform(group.Images, group.Platform))
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
            GraphicsTestPlatform platform
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

                    var name = (
                        platform.Schema.name.ToLower().Replace(' ', '-') + "-" + platform.Name + "-" + chunkNumber++
                    ).Replace("--", "-");
                    assetBundles.Add(CreateAssetBundleBuild(chunkKeys, chunkValues, name));
                }
            }

            return assetBundles;
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
