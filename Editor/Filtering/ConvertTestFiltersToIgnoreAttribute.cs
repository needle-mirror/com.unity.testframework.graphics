using System.IO;
using System.Text;
using UnityEditor.TestTools.Graphics.Platforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Filtering
{
    static class ConvertTestFiltersToIgnoreAttribute
    {
        internal static IAssetService AssetService { get; set; } = new AssetDatabaseService();

        internal static string ConvertFiltersToIgnore(TestFilters testFiltersAsset)
        {
            var ignoreAttributes = new StringBuilder();

            if (testFiltersAsset.filters == null)
            {
                return string.Empty;
            }

            foreach (var filter in testFiltersAsset.filters)
            {
                ignoreAttributes.Append(GenerateIgnoreAttributeFromFilter(filter));
                ignoreAttributes.Append("\n");
            }

            return ignoreAttributes.ToString().Trim();
        }

        static string GenerateIgnoreAttributeFromFilter(TestFilterConfig filter)
        {
            var hasFilteredScenes = false;
            var allNull = true;
            if (filter.filteredScenes != null)
            {
                foreach (var scene in filter.filteredScenes)
                {
                    hasFilteredScenes = true;
                    if (scene != null)
                        allNull = false;
                }
            }
            if (!hasFilteredScenes || allNull)
            {
                return string.Empty;
            }

            var escapedReason = filter.reason?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
            return $"[IgnoreGraphicsTest(\"{TestSceneNames(filter.filteredScenes)}\", \"{escapedReason}\"{PlatformArguments(filter)})]";
        }

        static string TestSceneNames(SceneAsset[] sceneAssets)
        {
            var sceneNames = new StringBuilder();

            foreach (var scene in sceneAssets)
            {
                var scenePath = AssetService.GetAssetPath(scene);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);

                sceneNames.Append(sceneName);
                sceneNames.Append("|");
            }

            return sceneNames.ToString().Trim('|');
        }

        static string PlatformArguments(TestFilterConfig filter)
        {
            var platformArguments = new StringBuilder();

            if (filter.colorSpace != ColorSpace.Uninitialized)
                platformArguments.Append($", ColorSpace.{filter.colorSpace}");

            if (filter.buildPlatform != BuildTarget.NoTarget)
                platformArguments.Append($", RuntimePlatform.{filter.buildPlatform.ToRuntimePlatform()}");

            if (filter.graphicsDevice != GraphicsDeviceType.Null)
                platformArguments.Append($", GraphicsDeviceType.{filter.graphicsDevice}");

            if (filter.architecture != Architecture.Unknown)
                platformArguments.Append($", Architecture.{filter.architecture.ToInteropArchitecture()}");

            if (!string.IsNullOrEmpty(filter.xrSdk) && filter.xrSdk != "None")
                platformArguments.Append($", XrDevice.{filter.xrSdk}");

            if (filter.stereoModes == StereoRenderingPaths.None)
                return platformArguments.ToString();

            foreach (StereoRenderingPaths stereoMode in System.Enum.GetValues(typeof(StereoRenderingPaths)))
            {
                if (stereoMode != StereoRenderingPaths.None && filter.stereoModes.HasFlag(stereoMode))
                    platformArguments.Append($", {nameof(StereoRenderingPaths)}.{stereoMode}");
            }

            return platformArguments.ToString();
        }
    }
}
