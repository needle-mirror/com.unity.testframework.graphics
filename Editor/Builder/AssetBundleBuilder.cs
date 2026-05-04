using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    sealed class AssetBundleBuilder : IPlayerContentBuilder
    {
        const string k_AssetBundlePath = "Assets/StreamingAssets";

        public IEnumerable<string> BuildContent(
            IList<GraphicsTestCase> testCases,
            IEnumerable<GraphicsTestPlatform> platforms,
            BuildTarget buildTarget
        )
        {
            var assetBundlesToBuild = new List<AssetBundleBuild>();
            var alreadyFound = new HashSet<string>();

            foreach (var platform in platforms)
            {
                GraphicsTestLogger.Log($"Searching for reference images for platform {platform}...");

                var filteredTestCases = new List<GraphicsTestCase>();
                foreach (var tc in testCases)
                {
                    if (tc.ReferenceImageDescriptor == null)
                    {
                        throw new InvalidOperationException($"Test case '{tc.Name}' has null ReferenceImageDescriptor. This is a bug in test setup.");
                    }

                    if (!alreadyFound.Contains(tc.ReferenceImageDescriptor.BuildDefaultName()) &&
                        !alreadyFound.Contains(tc.ReferenceImageDescriptor.BuildVariant(0)))
                    {
                        filteredTestCases.Add(tc);
                    }
                }

                var images = ReferenceImageUtility.Default.CollectReferenceImagePathsFor(filteredTestCases, platform);

                foreach (var key in images.Keys)
                    alreadyFound.Add(key);

                var assetBundle = GetAssetBundlesForPlatform(images, platform);

                if (assetBundle != null)
                {
                    assetBundlesToBuild.AddRange(assetBundle);
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
