using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics
{
    class ReferenceImageUtility
    {
        internal static ReferenceImageUtility Default { get; set; } = new();

        internal IAssetService<Texture2D> AssetService { get; set; } = new AssetDatabaseAssetService<Texture2D>();

        internal Dictionary<string, string> CollectReferenceImagePathsFor(
            IList<GraphicsTestCase> testCases,
            GraphicsTestPlatform platform
        )
        {
            var images = new Dictionary<string, string>();
            if (testCases == null || testCases.Count == 0)
                return images;

            var sortedTestCases = new List<GraphicsTestCase>(testCases);
            sortedTestCases.Sort(
                (a, b) =>
                    StringComparer.InvariantCulture.Compare(
                        a.ReferenceImageDescriptor?.Root ?? string.Empty,
                        b.ReferenceImageDescriptor?.Root ?? string.Empty
                    )
            );

            foreach (var platformPath in platform.AllResultsPaths)
            {
                GraphicsTestLogger.Log(LogType.Log, $"Searching for reference images in {platformPath}...");

                var assetPaths = new List<string>();
                foreach (var p in AssetService.FindAssets(platformPath, string.Empty))
                {
                    if (Path.GetDirectoryName(p)?.Replace("\\", "/") == platformPath)
                        assetPaths.Add(p);
                }
                assetPaths.Sort(StringComparer.InvariantCulture);

                var testCaseIndex = 0;

                for (var i = 0; i < assetPaths.Count; )
                {
                    var assetPath = assetPaths[i];

                    var imageName = Path.GetFileNameWithoutExtension(assetPath);

                    // Advance to the test case alphabetically right before the current asset name
                    while (testCaseIndex < sortedTestCases.Count - 1)
                    {
                        var nextRoot =
                            sortedTestCases[testCaseIndex + 1].ReferenceImageDescriptor?.Root ?? string.Empty;
                        if (string.Compare(nextRoot, imageName, StringComparison.InvariantCulture) <= 0)
                            testCaseIndex++;
                        else
                            break;
                    }

                    var currentTestCase = sortedTestCases[testCaseIndex];
                    var currentReferenceImageDescriptor = currentTestCase.ReferenceImageDescriptor;
                    if (currentReferenceImageDescriptor == null)
                    {
                        GraphicsTestLogger.Log(
                            LogType.Error,
                            $"ReferenceImageDescriptor is null for test {currentTestCase.FullName} and therefore no reference image can be loaded."
                        );
                        i++; // move to next asset
                        continue;
                    }

                    // Collect this asset and any subsequent assets that match this test case if it has additional reference images
                    var j = i;
                    while (j < assetPaths.Count)
                    {
                        var matchingAssetPath = assetPaths[j];
                        var matchingFileName = Path.GetFileName(matchingAssetPath);
                        var matchingImageName = Path.GetFileNameWithoutExtension(matchingAssetPath);

                        if (
                            !currentReferenceImageDescriptor.TryParse(
                                matchingFileName,
                                currentReferenceImageDescriptor.BuildDefaultName()
                            )
                        )
                            break;

                        if (!images.TryAdd(matchingImageName, matchingAssetPath))
                        {
                            GraphicsTestLogger.Log(
                                LogType.Warning,
                                $"Found multiple images for image name {matchingImageName}. Skipping {matchingAssetPath}."
                            );
                        }

                        j++;
                    }

                    // Skip over any assets we consumed; otherwise advance by one
                    i = (j == i) ? i + 1 : j;
                }
            }

            return images;
        }

        internal void SetupReferenceImageImportSettings(IEnumerable<GraphicsTestCase> testCases)
        {
            var paths = new List<string>();
            foreach (var t in testCases)
                paths.Add(t.ReferenceImage.AssetPath);
            SetupReferenceImageImportSettings(paths);
        }

        internal void SetupReferenceImageImportSettings(IEnumerable<string> imageAssetPaths)
        {
            var seen = new HashSet<string>();
            var paths = new List<string>();
            foreach (var path in imageAssetPaths)
            {
                if (!string.IsNullOrWhiteSpace(path) && seen.Add(path))
                    paths.Add(path);
            }

            AssetService.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                {
                    SetupReferenceImageImportSettings(path);
                }
            }
            finally
            {
                AssetService.StopAssetEditing();
            }
        }

        /// <summary>
        /// Sets up reference image import settings for the texture at the specified path.
        /// </summary>
        /// <param name="path">Path to the texture asset</param>
        /// <param name="saveAndReimport">Whether to call SaveAndReimport after applying settings.
        /// Must be false when called from AssetPostprocessor callbacks to avoid "ImportAsset during importing" error.</param>
        internal void SetupReferenceImageImportSettings(string path, bool saveAndReimport = true)
        {
            if (AssetService.GetImporterAtPath(path) is not ITextureImporter importer)
                return;

            if (!NeedsImportSettingFix(importer))
                return;

            ApplyReferenceImageImportSettings(importer);

            if (saveAndReimport)
            {
                importer.SaveAndReImport();
            }
        }

        static bool NeedsImportSettingFix(ITextureImporter importer)
        {
            return importer
                is not {
                    isReadable: true, // readable for comparison
                    sRGBTexture: false, // sRGB disabled for accurate color comparison
                    textureCompression: TextureImporterCompression.Uncompressed, // no compression artifacts
                    mipmapEnabled: false, // no mipmaps for reference images
                    filterMode: FilterMode.Point, // point filtering for pixel-perfect comparison
                    npotScale: TextureImporterNPOTScale.None, // no scaling for non-power-of-two textures
                    wrapMode: TextureWrapMode.Clamp, // clamp to avoid wrapping artifacts
                };
        }

        static void ApplyReferenceImageImportSettings(ITextureImporter importer)
        {
            importer.isReadable = true;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
