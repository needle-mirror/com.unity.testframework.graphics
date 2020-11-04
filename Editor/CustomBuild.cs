using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

public static class CustomBuild
{

    [MenuItem("Tools/Build Android (GLES2)")]
    static void BuildAndroidGLES2()
    {
        string path = EditorUtility.SaveFolderPanel("Choose Location of Build", "", "");
        //PlayerSettings.colorSpace = ColorSpace.Gamma;
        BuildTarget buildTarget = BuildTarget.Android;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.OpenGLES2 };
        BuildScenes(path, graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    [MenuItem("Tools/Build Android (Vulkan)")]
    static void BuildAndroidVulkan()
    {
        string path = EditorUtility.SaveFolderPanel("Choose Location of Build", "", "");
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.Android;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Vulkan };
        BuildScenes(path, graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }
    
    [MenuItem("Tools/Build Android (GLES3)")]
    static void BuildAndroidGLES3()
    {
        string path = EditorUtility.SaveFolderPanel("Choose Location of Build", "", "");
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.Android;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.OpenGLES3 };
        BuildScenes(path, graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    [MenuItem("Tools/Build iOS (Metal)")]
    static void BuildiOSMetal()
    {
        string path = EditorUtility.SaveFolderPanel("Choose Location of Build", "", "");
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.iOS;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Metal };
        BuildScenes(path, graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);        
    }

    [MenuItem("Tools/Build OSX (Metal)")]
    static void BuildOSXMetal()
    {
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.StandaloneOSX;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Metal };
        BuildScenes(".", graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);        
    }

    static void BuildWindowsVulkan()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Vulkan };
        BuildScenes(".", graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    static void BuildLinuxVulkan()
    {
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.StandaloneLinux64;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Vulkan };
        BuildScenes(".", graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    static void BuildLinuxOpenGLCore()
    {
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.StandaloneLinux64;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.OpenGLCore };
        BuildScenes(".", graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    static void BuildWindowsDX11()
    {
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Direct3D11 };
        BuildScenes(".", graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    static void BuildWindowsDX12()
    {
        //PlayerSettings.colorSpace = ColorSpace.Linear;
        BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        BuildOptions buildOptions = BuildOptions.None;

        GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Direct3D12 };
        BuildScenes(".", graphicsAPIs[0].ToString(), buildTarget, buildOptions, graphicsAPIs);
    }

    static void BuildScenes(string path, string name, BuildTarget buildTarget, BuildOptions buildOptions, GraphicsDeviceType[] graphicsAPIs)
    {
        string buildName = string.Format("{0}{1}", "TestScenes", name);

        PlayerSettings.SetGraphicsAPIs(buildTarget, graphicsAPIs);
        PlayerSettings.productName = buildName;
        PlayerSettings.applicationIdentifier = string.Format("com.unity.{0}", buildName);
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenHeight = ImageAssert.kBackBufferHeight;
        PlayerSettings.defaultScreenWidth = ImageAssert.kBackBufferWidth;
        PlayerSettings.SetAspectRatio(AspectRatio.Aspect16by9, true);

        List<string> scenesToBuild = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                scenesToBuild.Add(scene.path);
        
        string suffix = (buildTarget == BuildTarget.Android) ? ".apk" : "";

        BuildPipeline.BuildPlayer(scenesToBuild.ToArray(), string.Format("{0}/{1}{2}", path, buildName, suffix), buildTarget, buildOptions);
    }

    [MenuItem("Tools/Set Color Space - Linear")]
    static void SetColorSpaceLinear()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
    }

    [MenuItem("Tools/Set Color Space - Gamma")]
    static void SetColorSpaceGamma()
    {
        PlayerSettings.colorSpace = ColorSpace.Gamma;
    }
}

