using System;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics
{
    class ReferenceImageAssetPostprocessor : AssetPostprocessor
    {
        static readonly string[] k_ValidFolders =
        {
            PlatformSchema.k_DefaultReferenceImagesRoot,
            PlatformSchema.k_DefaultReferenceImagesBaseRoot,
            GraphicsTestBuildSettings.k_DefaultActualImagesPath,
        };

        static readonly string[] k_ValidExtensions = { ".png", ".exr" };

        void OnPreprocessTexture()
        {
            var path = assetPath.SanitizeBackslashes();

            var isInTargetFolder = Array.Exists(
                k_ValidFolders,
                folder => path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)
            );

            var hasValidExtension = Array.Exists(
                k_ValidExtensions,
                ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
            );

            if (!isInTargetFolder || !hasValidExtension)
                return;

            // Pass saveAndReimport: false because we're already in the import pipeline.
            // Calling SaveAndReimport during OnPreprocessTexture would cause "ImportAsset during importing" error.
            ReferenceImageUtility.Default.SetupReferenceImageImportSettings(path, saveAndReimport: false);
        }
    }
}
