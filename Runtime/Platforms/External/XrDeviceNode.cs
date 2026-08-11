using System;
#if UNITY_EDITOR
using UnityEditor;
#if UNITY_XR
using UnityEditor.XR.Management;
#endif
#endif
#if UNITY_XR
using UnityEngine.XR.Management;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class XrDeviceNode : IPlatformNode
    {
        public Type DataType { get; } = typeof(XrDevice);
        public Enum Current =>
#if UNITY_EDITOR && UNITY_XR
            GetXrSdk(GetEditorXrSettings());
#elif !UNITY_EDITOR && UNITY_XR
            RuntimeDevice;
#else
            XrDevice.None;
#endif

        public Enum Build =>
#if UNITY_XR && UNITY_EDITOR
            GetXrSdk(GetBuildXrSettings(EditorUserBuildSettings.activeBuildTarget));
#else
            XrDevice.None;
#endif

        static XrDevice FromString(string xrDevice)
        {
            if (string.IsNullOrEmpty(xrDevice))
                return XrDevice.None;

            return (XrDevice)Enum.Parse(typeof(XrDevice), xrDevice);
        }

        XrDevice RuntimeDevice
        {
            get
            {
#if ENABLE_VR && UNITY_XR
                // Reuse standard (non-VR) reference images
                if (RuntimeSettings.reuseTestsForXR)
                    return XrDevice.None;

                // XR SDK path
                var activeLoader = XRGeneralSettings.Instance?.Manager?.activeLoader;
                if (activeLoader != null)
                    return FromString(activeLoader.name);

                // Legacy VR path
                if (XR.XRSettings.enabled && XR.XRSettings.loadedDeviceName.Length > 0)
                    return FromString(XR.XRSettings.loadedDeviceName);
#endif
                return XrDevice.None;
            }
        }

#if UNITY_EDITOR && UNITY_XR
        static XrDevice GetXrSdk(XRGeneralSettings settings)
        {
            if (RuntimeSettings.reuseTestsForXR)
                return XrDevice.None;

            if (IsXrActive(settings))
            {
                var firstLoader = settings.Manager.activeLoaders[0];
                return FromString(firstLoader?.name);
            }
            return XrDevice.None;
        }

        static bool IsXrActive(XRGeneralSettings settings)
        {
            if (settings?.InitManagerOnStart ?? false)
                return (settings.Manager?.activeLoaders?.Count ?? 0) > 0;
            else
                return false;
        }

        static XRGeneralSettings GetEditorXrSettings() =>
            XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

        static XRGeneralSettings GetBuildXrSettings(BuildTarget buildPlatform) =>
            XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                BuildPipeline.GetBuildTargetGroup(buildPlatform)
            );
#endif
    }
}
