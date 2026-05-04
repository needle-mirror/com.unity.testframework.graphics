using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Filtering
{
    [Serializable]
    class TestFilterConfig
    {
        [SerializeField]
        [FormerlySerializedAs("FilteredScene")]
        internal SceneAsset filteredScene;

        [SerializeField]
        [FormerlySerializedAs("FilteredScenes")]
        internal SceneAsset[] filteredScenes;

        [SerializeField]
        [FormerlySerializedAs("ColorSpace")]
        internal ColorSpace colorSpace = ColorSpace.Uninitialized;

        [SerializeField]
        [FormerlySerializedAs("BuildPlatform")]
        internal BuildTarget buildPlatform = BuildTarget.NoTarget;

        [SerializeField]
        [FormerlySerializedAs("GraphicsDevice")]
        internal GraphicsDeviceType graphicsDevice = GraphicsDeviceType.Null;

        [SerializeField]
        [FormerlySerializedAs("Architecture")]
        internal Architecture architecture = Architecture.Unknown;

        [SerializeField]
        [FormerlySerializedAs("XrSdk")]
        internal string xrSdk;

        [SerializeField]
        [FormerlySerializedAs("StereoModes")]
        internal StereoRenderingPaths stereoModes;

        [SerializeField]
        [FormerlySerializedAs("Reason")]
        internal string reason;
    }

    // Should be removed
    internal enum Architecture
    {
        Unknown = 0,
        ARM = 1,
        ARM64 = 2,
        x86 = 3,
        x86_64 = 4,
    }

    static class ArchitectureExtensions
    {
        internal static System.Runtime.InteropServices.Architecture ToInteropArchitecture(
            this Architecture architecture
        )
        {
            return architecture switch
            {
                Architecture.ARM => System.Runtime.InteropServices.Architecture.Arm,
                Architecture.ARM64 => System.Runtime.InteropServices.Architecture.Arm64,
                Architecture.x86 => System.Runtime.InteropServices.Architecture.X86,
                Architecture.x86_64 => System.Runtime.InteropServices.Architecture.X64,
                _ => (System.Runtime.InteropServices.Architecture)(-1),
            };
        }
    }
}
