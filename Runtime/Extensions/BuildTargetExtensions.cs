#if UNITY_EDITOR
using UnityEngine;
using System;

namespace UnityEditor.TestTools.Graphics.Platforms
{
    public static class BuildTargetExtensions
    {
        /// <summary>
        /// Converts the BuildTarget to a RuntimePlatform.
        /// </summary>
        /// <param name="target">The BuildTarget to convert.</param>
        /// <returns>The RuntimePlatform equivalent of the BuildTarget.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the BuildTarget is unknown.</exception>
        public static RuntimePlatform ToRuntimePlatform(this BuildTarget target)
        {
            return target switch
            {
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.PS4 => RuntimePlatform.PS4,
                BuildTarget.Switch => RuntimePlatform.Switch,
                (BuildTarget)48 => (RuntimePlatform)51,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.WSAPlayer => RuntimePlatform.WSAPlayerARM,
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                BuildTarget.XboxOne => RuntimePlatform.XboxOne,
                BuildTarget.tvOS => RuntimePlatform.tvOS,
                BuildTarget.LinuxHeadlessSimulation => RuntimePlatform.LinuxPlayer,
                BuildTarget.GameCoreXboxSeries => RuntimePlatform.GameCoreXboxSeries,
                BuildTarget.GameCoreXboxOne => RuntimePlatform.GameCoreXboxOne,
                BuildTarget.PS5 => RuntimePlatform.PS5,
                BuildTarget.EmbeddedLinux => RuntimePlatform.EmbeddedLinuxArm64,
                BuildTarget.QNX => RuntimePlatform.QNXArm64,
                BuildTarget.VisionOS => RuntimePlatform.VisionOS,
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown BuildTarget"),
            };
        }
    }
}
#endif
