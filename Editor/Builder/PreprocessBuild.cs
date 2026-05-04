using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.Builder
{
    class PreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return 1; }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenHeight = ImageAssert.k_KBackBufferHeight;
            PlayerSettings.defaultScreenWidth = ImageAssert.k_KBackBufferWidth;
        }
    }
}
