using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Discovers <see cref="GraphicsTestImageBuildSettings"/> assets anywhere in the project.
    /// </summary>
    public static class ImageBuildSettingsUtility
    {
        static IAssetService s_AssetService;

        internal static IAssetService AssetService
        {
            get => s_AssetService ??= new AssetDatabaseService();
            set => s_AssetService = value;
        }

        /// <summary>Finds every <see cref="GraphicsTestImageBuildSettings"/> asset in the project.</summary>
        /// <returns>The discovered assets, GUID-ordered so builds are reproducible.</returns>
        public static GraphicsTestImageBuildSettings[] DiscoverImageBuildSettings()
        {
            var guids = AssetService.FindAssets($"t:{nameof(GraphicsTestImageBuildSettings)}", null);
            Array.Sort(guids, StringComparer.Ordinal);

            var settings = new List<GraphicsTestImageBuildSettings>(guids.Length);
            foreach (var guid in guids)
            {
                var assetPath = AssetService.GuidToAssetPath(guid);
                var asset = AssetService.LoadAssetAtPath<GraphicsTestImageBuildSettings>(assetPath);
                if (asset != null)
                    settings.Add(asset);
            }

            return settings.ToArray();
        }
    }
}
