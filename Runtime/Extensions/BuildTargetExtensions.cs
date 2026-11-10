#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_6000_4_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace UnityEditor.TestTools.Graphics.Platforms
{
    public static class BuildTargetExtensions
    {
        const string k_DefaultWsaArchitecture = "ARM64";
        const EmbeddedArchitecture k_DefaultEmbeddedArchitecture = EmbeddedArchitecture.Arm64;

        /// <summary>
        /// Converts the BuildTarget to a RuntimePlatform.
        /// </summary>
        /// <param name="target">The BuildTarget to convert.</param>
        /// <returns>The RuntimePlatform equivalent of the BuildTarget.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the BuildTarget is unknown.</exception>
        public static RuntimePlatform ToRuntimePlatform(this BuildTarget target)
        {
            return target.ToRuntimePlatform(k_DefaultWsaArchitecture);
        }

        /// <summary>
        /// Converts the BuildTarget to a RuntimePlatform.
        /// </summary>
        /// <param name="target">The BuildTarget to convert.</param>
        /// <param name="wsaArchitecture">
        /// The UWP architecture BuildTarget.WSAPlayer is built for, as in EditorUserBuildSettings.wsaArchitecture:
        /// "x64", "x86" or "ARM64". Other BuildTargets ignore it.
        /// </param>
        /// <param name="embeddedArchitecture">
        /// The architecture BuildTarget.EmbeddedLinux or BuildTarget.QNX is built for: Arm64 or X64. Other BuildTargets
        /// ignore it.
        /// </param>
        /// <returns>The RuntimePlatform equivalent of the BuildTarget.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the BuildTarget is unknown, or when the architecture of BuildTarget.WSAPlayer,
        /// BuildTarget.EmbeddedLinux or BuildTarget.QNX is not supported.
        /// </exception>
        internal static RuntimePlatform ToRuntimePlatform(
            this BuildTarget target,
            string wsaArchitecture,
            EmbeddedArchitecture embeddedArchitecture = k_DefaultEmbeddedArchitecture
        )
        {
            return target.ToRuntimePlatformOrNull(wsaArchitecture, embeddedArchitecture)
                ?? throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown BuildTarget");
        }

        /// <summary>
        /// Converts the BuildTarget to a RuntimePlatform, without throwing on an unknown BuildTarget.
        /// </summary>
        /// <param name="target">The BuildTarget to convert.</param>
        /// <param name="wsaArchitecture">
        /// The UWP architecture BuildTarget.WSAPlayer is built for, as in EditorUserBuildSettings.wsaArchitecture:
        /// "x64", "x86" or "ARM64". Other BuildTargets ignore it.
        /// </param>
        /// <param name="embeddedArchitecture">
        /// The architecture BuildTarget.EmbeddedLinux or BuildTarget.QNX is built for: Arm64 or X64. Other BuildTargets
        /// ignore it.
        /// </param>
        /// <returns>The RuntimePlatform equivalent of the BuildTarget, or null when the BuildTarget is unknown.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the architecture of BuildTarget.WSAPlayer, BuildTarget.EmbeddedLinux or BuildTarget.QNX is not
        /// supported.
        /// </exception>
        internal static RuntimePlatform? ToRuntimePlatformOrNull(
            this BuildTarget target,
            string wsaArchitecture = k_DefaultWsaArchitecture,
            EmbeddedArchitecture embeddedArchitecture = k_DefaultEmbeddedArchitecture
        )
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
                BuildTarget.WSAPlayer => WsaPlayerRuntimePlatform(wsaArchitecture),
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                BuildTarget.XboxOne => RuntimePlatform.XboxOne,
                BuildTarget.tvOS => RuntimePlatform.tvOS,
                BuildTarget.LinuxHeadlessSimulation => RuntimePlatform.LinuxPlayer,
                BuildTarget.GameCoreXboxSeries => RuntimePlatform.GameCoreXboxSeries,
                BuildTarget.GameCoreXboxOne => RuntimePlatform.GameCoreXboxOne,
                BuildTarget.PS5 => RuntimePlatform.PS5,
                BuildTarget.EmbeddedLinux => EmbeddedRuntimePlatform(
                    embeddedArchitecture,
                    RuntimePlatform.EmbeddedLinuxArm64,
                    RuntimePlatform.EmbeddedLinuxX64
                ),
                BuildTarget.QNX => EmbeddedRuntimePlatform(
                    embeddedArchitecture,
                    RuntimePlatform.QNXArm64,
                    RuntimePlatform.QNXX64
                ),
                BuildTarget.VisionOS => RuntimePlatform.VisionOS,
                _ => null,
            };
        }

        static RuntimePlatform WsaPlayerRuntimePlatform(string wsaArchitecture)
        {
            if (string.Equals(wsaArchitecture, "x64", StringComparison.OrdinalIgnoreCase))
                return RuntimePlatform.WSAPlayerX64;
            if (string.Equals(wsaArchitecture, "x86", StringComparison.OrdinalIgnoreCase))
                return RuntimePlatform.WSAPlayerX86;
            if (string.Equals(wsaArchitecture, "ARM64", StringComparison.OrdinalIgnoreCase))
                return RuntimePlatform.WSAPlayerARM;

            throw new ArgumentOutOfRangeException(
                nameof(wsaArchitecture),
                wsaArchitecture,
                "Unsupported UWP architecture. Expected x64, x86 or ARM64. An empty value usually means "
                    + "Universal Windows Platform build support is not installed."
            );
        }

        static RuntimePlatform EmbeddedRuntimePlatform(
            EmbeddedArchitecture embeddedArchitecture,
            RuntimePlatform arm64Platform,
            RuntimePlatform x64Platform
        )
        {
            return embeddedArchitecture switch
            {
                EmbeddedArchitecture.Arm64 => arm64Platform,
                EmbeddedArchitecture.X64 => x64Platform,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(embeddedArchitecture),
                    embeddedArchitecture,
                    "Unsupported embedded architecture. Expected Arm64 or X64."
                ),
            };
        }

        /// <summary>
        /// Gets the architecture the Editor builds BuildTarget.EmbeddedLinux or BuildTarget.QNX for.
        /// </summary>
        /// <param name="target">The BuildTarget to get the architecture of.</param>
        /// <returns>
        /// The selected architecture of BuildTarget.EmbeddedLinux or BuildTarget.QNX, honoring the active build
        /// profile, or Arm64 for other BuildTargets.
        /// </returns>
        /// <exception cref="MissingMemberException">
        /// Thrown when the platform extension assembly is loaded but its Settings.architecture property is missing.
        /// </exception>
        internal static EmbeddedArchitecture GetSelectedEmbeddedArchitecture(this BuildTarget target)
        {
            if (target != BuildTarget.EmbeddedLinux && target != BuildTarget.QNX)
                return k_DefaultEmbeddedArchitecture;

            var settingsAssembly = target.GetPlatformSettingsAssembly();
            if (settingsAssembly != null)
            {
                var settingsTypeName = target == BuildTarget.QNX
                    ? "UnityEditor.QNX.Settings"
                    : "UnityEditor.EmbeddedLinux.Settings";
                return ReadSettingsArchitecture(settingsAssembly, settingsTypeName);
            }

#pragma warning disable CS0618
            return target == BuildTarget.QNX
                ? (EmbeddedArchitecture)EditorUserBuildSettings.selectedQnxArchitecture
                : (EmbeddedArchitecture)EditorUserBuildSettings.selectedEmbeddedLinuxArchitecture;
#pragma warning restore CS0618
        }

        /// <summary>
        /// Gets the loaded platform extension assembly that holds the Settings of BuildTarget.EmbeddedLinux or
        /// BuildTarget.QNX.
        /// </summary>
        /// <param name="target">The BuildTarget to get the platform extension assembly of.</param>
        /// <returns>
        /// The platform extension assembly, or null when the platform support is not installed or the BuildTarget is
        /// neither BuildTarget.EmbeddedLinux nor BuildTarget.QNX.
        /// </returns>
        internal static Assembly GetPlatformSettingsAssembly(this BuildTarget target)
        {
            // The platform extension assemblies are only present with the platform support, so this package
            // cannot reference them
            var assemblyName = target switch
            {
                BuildTarget.EmbeddedLinux => "UnityEditor.EmbeddedLinux.Extensions",
                BuildTarget.QNX => "UnityEditor.QNX.Extensions",
                _ => null,
            };
            if (assemblyName == null)
                return null;

#if UNITY_6000_4_OR_NEWER
            var loadedAssemblies = CurrentAssemblies.GetLoadedAssemblies();
#else
            // CurrentAssemblies.GetLoadedAssemblies is public from 6000.4
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif
            return loadedAssemblies.FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
        }

        /// <summary>
        /// Reads the architecture property of a platform Settings type.
        /// </summary>
        /// <param name="settingsAssembly">The assembly that holds the Settings type.</param>
        /// <param name="settingsTypeName">The full name of the Settings type.</param>
        /// <returns>The value of the public static EmbeddedArchitecture architecture property.</returns>
        /// <exception cref="MissingMemberException">
        /// Thrown when the Settings type is missing, or has no public static EmbeddedArchitecture architecture
        /// property.
        /// </exception>
        internal static EmbeddedArchitecture ReadSettingsArchitecture(Assembly settingsAssembly, string settingsTypeName)
        {
            var architectureProperty = settingsAssembly
                .GetType(settingsTypeName)
                ?.GetProperty("architecture", BindingFlags.Public | BindingFlags.Static);
            if (architectureProperty == null || architectureProperty.PropertyType != typeof(EmbeddedArchitecture))
            {
                throw new MissingMemberException(
                    $"{settingsAssembly.GetName().Name} has no public static EmbeddedArchitecture property "
                        + $"{settingsTypeName}.architecture, so the graphics test framework cannot tell which "
                        + "architecture the player is built for. The platform extension API has likely changed."
                );
            }

            return (EmbeddedArchitecture)architectureProperty.GetValue(null);
        }
    }
}
#endif
