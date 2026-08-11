using System.Collections;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Sets global rendering resolution.
    /// </summary>
    /// <remarks>
    /// This script sets global rendering resolution based on the predefined settings inside scriptable object that is attached
    /// to the component with this script.
    /// This is needed for tests consistency. For example by the date of writing this new Android devices are added to the
    /// test rig, which have different resolution (2280x1080) compared to the old ones (1920x1080). This difference causes
    /// majority of tests fail.
    /// </remarks>
    public class GlobalResolutionSetter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Scriptable object with custom resolution settings.")]
        CustomResolutionSettings customResolutionSettings;

        // Used for per-scene asset-style resolution setting.
        void Awake()
        {
            foreach (var resolutionSettingsField in customResolutionSettings.fields)
            {
                if (SetResolution(resolutionSettingsField))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Sets rendering resolution.
        /// </summary>
        /// <param name="resolutionFields">
        /// Resolution fields to set.
        /// </param>
        /// <returns>
        /// True if resolution was set, false otherwise.
        /// </returns>
        public static bool SetResolution(CustomResolutionFields resolutionFields)
        {
            if (resolutionFields.platform != Application.platform)
            {
                GraphicsTestLogger.DebugLog(
                    $"Skipping setting rendering resolution, target platform: {resolutionFields.platform}, current platform: {Application.platform}"
                );
                return false;
            }

            GraphicsTestLogger.DebugLog(
                $"Setting new rendering resolution: {resolutionFields.width}x{resolutionFields.height}"
            );
            Screen.SetResolution(resolutionFields.width, resolutionFields.height, resolutionFields.isFullScreen);
            return true;
        }

        /// <summary>
        /// Sets rendering resolution.
        /// </summary>
        /// <param name="platformFilter">
        /// Platform to set resolution for.
        /// </param>
        /// <param name="width">
        /// Width of the resolution.
        /// </param>
        /// <param name="height">
        /// Height of the resolution.
        /// </param>
        /// <param name="fullscreen">
        /// Whether to set fullscreen or not.
        /// </param>
        /// <returns>
        /// True if resolution was set, false otherwise.
        /// </returns>
        public static bool SetResolution(
            RuntimePlatform platformFilter,
            int width = 1920,
            int height = 1080,
            bool fullscreen = true
        )
        {
            return SetResolution(
                new CustomResolutionFields
                {
                    platform = platformFilter,
                    width = width,
                    height = height,
                    isFullScreen = fullscreen,
                }
            );
        }

        /// <summary>
        /// Sets the screen resolution with retry and verification logic.
        /// </summary>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <param name="fullscreen">Fullscreen mode</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>
        /// Coroutine enumerator
        /// </returns>
        public static IEnumerator SetResolutionWithRetry(int width, int height, bool fullscreen, int maxRetries = 3)
        {
            if (Application.isEditor)
            {
                GraphicsTestLogger.Log("Running test in Editor, skipping Player resolution change");
                yield break;
            }

#if UNITY_WEBGL
            GraphicsTestLogger.Log("Running test on WebGL platform, skipping Player resolution change");
            yield break;
#else
            // Check if resolution is already correct
            if (Screen.width == width && Screen.height == height)
            {
                if (s_SettledWidth == width && s_SettledHeight == height)
                {
                    GraphicsTestLogger.Log($"Resolution already set to {width}x{height}, skipping resolution change entirely");
                    yield break;
                }

                // Applied elsewhere (e.g. plain Screen.SetResolution in OneTimeSetUp); may still be in flight.
                GraphicsTestLogger.Log($"Resolution already reports {width}x{height}, waiting for it to settle");
                yield return WaitForModeSwitchToSettle(Application.platform, () => Time.realtimeSinceStartup);
                s_SettledWidth = width;
                s_SettledHeight = height;
                yield break;
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                int framesToWait = attempt switch
                {
                    1 => 60,
                    2 => 600,
                    _ => 6000
                };

                GraphicsTestLogger.Log(
                    $"Attempting to set resolution to {width}x{height} " +
                    $"(Attempt {attempt}/{maxRetries}, waiting up to {framesToWait} frame(s) for it to apply)");

                Screen.SetResolution(width, height, fullscreen);

                for (int frame = 0; frame <= framesToWait; frame++)
                {
                    if (Screen.width == width && Screen.height == height)
                    {
                        GraphicsTestLogger.Log(
                            $"Resolution successfully set to {width}x{height} " +
                            $"on attempt {attempt} after {frame} frame(s)");
                        yield return WaitForModeSwitchToSettle(Application.platform, () => Time.realtimeSinceStartup);
                        s_SettledWidth = width;
                        s_SettledHeight = height;
                        yield break;
                    }

                    yield return null;
                }

                GraphicsTestLogger.Log(
                    $"Resolution not applied after attempt {attempt}. " +
                    $"Current: {Screen.width}x{Screen.height}, Expected: {width}x{height}");
            }

            throw new System.InvalidOperationException(
                $"Failed to set resolution to {width}x{height} after {maxRetries} attempts. " +
                $"Current resolution: {Screen.width}x{Screen.height}. " +
                "Graphics tests require this resolution for valid image comparison."
            );
#endif
        }

        static int s_SettledWidth;
        static int s_SettledHeight;

        internal static IEnumerator WaitForModeSwitchToSettle(RuntimePlatform platform, System.Func<float> now)
        {
            const float settleSeconds = 5f;

            if (platform != RuntimePlatform.OSXPlayer)
                yield break;

            float start = now();
            while (now() - start < settleSeconds)
                yield return null;
        }
    }
}
