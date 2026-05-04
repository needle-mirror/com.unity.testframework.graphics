using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.Services
{
    /// <summary>
    /// Wires <see cref="GraphicsTestBuildSettings.AssetOps"/> through an
    /// <see cref="IAssetService"/> so the runtime assembly never references the
    /// editor assembly directly.
    /// </summary>
    [InitializeOnLoad]
    static class BuildSettingsAssetServiceBridge
    {
        internal static IAssetService Service { get; set; } = new AssetDatabaseService();

        static BuildSettingsAssetServiceBridge()
        {
            Bind(Service);
        }

        internal static void Bind(IAssetService service)
        {
            GraphicsTestBuildSettings.AssetOps.LoadSettings =
                path => service.LoadAssetAtPath<GraphicsTestBuildSettings>(path);
            GraphicsTestBuildSettings.AssetOps.LoadAllAssetsAtPath = service.LoadAllAssetsAtPath;
            GraphicsTestBuildSettings.AssetOps.GetAssetPath = service.GetAssetPath;
            GraphicsTestBuildSettings.AssetOps.CreateAsset = service.CreateAsset;
            GraphicsTestBuildSettings.AssetOps.AddObjectToAsset = service.AddObjectToAsset;
            GraphicsTestBuildSettings.AssetOps.SetDirty = service.SetDirty;
            GraphicsTestBuildSettings.AssetOps.SaveAssets = service.SaveAssets;
            GraphicsTestBuildSettings.AssetOps.Refresh = service.Refresh;
        }
    }
}
