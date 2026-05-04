using UnityEditor.TestTools;
using UnityEngine.TestTools.Graphics;

[assembly: TestPlayerBuildModifier(typeof(UnityEditor.TestTools.Graphics.Builder.GraphicsPlayerBuildModifier))]

namespace UnityEditor.TestTools.Graphics.Builder
{
    class GraphicsPlayerBuildModifier : ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
        {
            // Add an extra define to the player so that XR test code can be enabled while still using the regular (non-VR) reference images
            if (RuntimeSettings.reuseTestsForXR)
                AddExtraScriptingDefine(ref playerOptions, "XR_REUSE_TESTS_STANDALONE");

            // Add an extra define to the player so that RenderGraph test code can be enabled while still using the regular (non-RG) reference images
            if (RuntimeSettings.reuseTestsForRenderGraph)
                AddExtraScriptingDefine(ref playerOptions, "RENDER_GRAPH_REUSE_TESTS_STANDALONE");

#if !UNITY_6000_4_OR_NEWER
            // Add an extra define to the player to enable tests for URP Compatibility Mode
            if (RuntimeSettings.urpCompatibilityMode)
                AddExtraScriptingDefine(ref playerOptions, "URP_COMPATIBILITY_MODE");
#endif

            return playerOptions;
        }

        void AddExtraScriptingDefine(ref BuildPlayerOptions playerOptions, string extraScriptingDefine)
        {
            if (playerOptions.extraScriptingDefines != null)
            {
                var extraScriptingDefines = new string[1 + playerOptions.extraScriptingDefines.Length];
                playerOptions.extraScriptingDefines.CopyTo(extraScriptingDefines, 0);
                extraScriptingDefines[playerOptions.extraScriptingDefines.Length] = extraScriptingDefine;

                playerOptions.extraScriptingDefines = extraScriptingDefines;
            }
            else
            {
                playerOptions.extraScriptingDefines = new[] { extraScriptingDefine };
            }
        }
    }
}
