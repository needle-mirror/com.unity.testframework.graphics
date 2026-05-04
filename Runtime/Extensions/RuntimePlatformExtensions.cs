using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Extension methods for the RuntimePlatform enum.
    /// </summary>
    public static class RuntimePlatformExtensions
    {
        /// <summary>
        /// Converts the RuntimePlatform to a unique string value.
        /// </summary>
        /// <param name="platform">The RuntimePlatform to convert.</param>
        /// <param name="architecture">The architecture of the platform.</param>
        /// <returns>A unique string value for the RuntimePlatform.</returns>
        /// <remarks>
        /// This method is required to generate backward compatible unique string values for duplicated RuntimePlatform enum values.
        /// </remarks>
        public static string ToUniqueString(
            this RuntimePlatform platform,
            System.Runtime.InteropServices.Architecture architecture
        )
        {
            var platformUniqueString = platform switch
            {
                RuntimePlatform.WSAPlayerX86 => "MetroPlayerX86", //duplicate RuntimePlatform.MetroPlayerX86
                RuntimePlatform.WSAPlayerX64 => "MetroPlayerX64", //duplicate RuntimePlatform.MetroPlayerX64
                RuntimePlatform.WSAPlayerARM => "MetroPlayerARM", //duplicate RuntimePlatform.MetroPlayerARM
                _ => platform.ToString(), // Use the default enum value
            };

            if (architecture is not System.Runtime.InteropServices.Architecture.Arm64)
                return platformUniqueString;
            switch (platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    platformUniqueString += "_AppleSilicon";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    platformUniqueString += "_ARM64";
                    break;
                default:
                    break;
            }

            return platformUniqueString;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Converts the RuntimePlatform to a BuildTarget.
        /// </summary>
        /// <param name="platform">The RuntimePlatform to convert.</param>
        /// <returns>The BuildTarget equivalent of the RuntimePlatform.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the RuntimePlatform is unknown.</exception>
        public static BuildTarget ToBuildTarget(this RuntimePlatform platform)
        {
            return platform switch
            {
                RuntimePlatform.Android => BuildTarget.Android,
                RuntimePlatform.IPhonePlayer => BuildTarget.iOS,
                RuntimePlatform.LinuxEditor or RuntimePlatform.LinuxPlayer => BuildTarget.StandaloneLinux64,
                RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer => BuildTarget.StandaloneOSX,
                RuntimePlatform.PS4 => BuildTarget.PS4,
                RuntimePlatform.Switch => BuildTarget.Switch,
                (RuntimePlatform)51 => (BuildTarget)48,
                RuntimePlatform.tvOS => BuildTarget.tvOS,
                RuntimePlatform.WebGLPlayer => BuildTarget.WebGL,
                RuntimePlatform.WindowsEditor or RuntimePlatform.WindowsPlayer => BuildTarget.StandaloneWindows,
                RuntimePlatform.WSAPlayerARM or RuntimePlatform.WSAPlayerX64 or RuntimePlatform.WSAPlayerX86 =>
                    BuildTarget.WSAPlayer,
                RuntimePlatform.XboxOne => BuildTarget.XboxOne,
                RuntimePlatform.GameCoreXboxSeries => BuildTarget.GameCoreXboxSeries,
                RuntimePlatform.GameCoreXboxOne => BuildTarget.GameCoreXboxOne,
                RuntimePlatform.PS5 => BuildTarget.PS5,
                RuntimePlatform.EmbeddedLinuxArm64 => BuildTarget.EmbeddedLinux,
                RuntimePlatform.QNXArm64 => BuildTarget.QNX,
                RuntimePlatform.VisionOS => BuildTarget.VisionOS,
                _ => throw new ArgumentOutOfRangeException(nameof(platform), $"Unknown RuntimePlatform: {platform}"),
            };
        }
#endif
    }
}
