using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Settings for capturing an EditorWindow.
    /// </summary>
    public class EditorWindowCaptureSettings
    {
        /// <summary>
        /// The default width and height used for window captures, in pixels.
        /// </summary>
        protected const int k_DefaultCaptureSize = 512;

        int m_Width;
        int m_Height;
        TimeSpan m_DelayBeforeCapture;
        IList<Action<EditorWindow>> m_AdditionalSetupActions;

        /// <summary>
        /// The width of the captured image. Defaults to 512.
        /// </summary>
        public int Width
        {
            get => m_Width;
            internal set => m_Width = value;
        }

        /// <summary>
        /// The height of the captured image. Defaults to 512.
        /// </summary>
        public int Height
        {
            get => m_Height;
            internal set => m_Height = value;
        }

        /// <summary>
        /// The delay before capturing the window. Defaults to zero.
        /// </summary>
        /// <remarks>
        /// This can be useful for waiting for async operations to complete before capturing. This wait step happens after all other setup actions.
        /// </remarks>
        public TimeSpan DelayBeforeCapture
        {
            get => m_DelayBeforeCapture;
            internal set => m_DelayBeforeCapture = value;
        }

        /// <summary>
        /// Additional setup actions to be performed before capturing the window.
        /// </summary>
        /// <remarks>
        /// These actions are performed after the window is set up and before the delay before capture.
        /// </remarks>
        public IList<Action<EditorWindow>> AdditionalSetupActions
        {
            get => m_AdditionalSetupActions;
            internal set => m_AdditionalSetupActions = value;
        }

        /// <summary>
        /// Creates a new instance of EditorWindowCaptureSettings.
        /// </summary>
        /// <param name="width">The width of the Editor window</param>
        /// <param name="height">The height of the Editor window</param>
        /// <param name="delayBeforeCapture">How long to wait immediately before capturing</param>
        /// <param name="additionalSetupActions">Additional actions to perform immediately right before the final delay</param>
        public EditorWindowCaptureSettings(
            int width,
            int height,
            TimeSpan delayBeforeCapture,
            params Action<EditorWindow>[] additionalSetupActions
        )
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width)) { Data = { { "width", width } } };

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height)) { Data = { { "height", height } } };

            if (additionalSetupActions is null || Array.Exists(additionalSetupActions, a => a is null))
                throw new ArgumentNullException(nameof(additionalSetupActions));

            this.m_Width = width;
            this.m_Height = height;
            this.m_DelayBeforeCapture = delayBeforeCapture;
            this.m_AdditionalSetupActions = new List<Action<EditorWindow>>(additionalSetupActions);
        }

        /// <summary>
        /// Default settings for capturing an EditorWindow.
        /// </summary>
        /// <remarks>
        /// These settings are used when no specific settings are provided.
        /// The default width and height are both 512, and the delay before capture is zero.
        /// </remarks>
        public static EditorWindowCaptureSettings Default => new EditorWindowCaptureSettings(k_DefaultCaptureSize, k_DefaultCaptureSize, TimeSpan.Zero);
    }

    /// <summary>
    /// Settings for capturing a SceneView.
    /// </summary>
    public class SceneViewCaptureSettings : EditorWindowCaptureSettings
    {
        Transform m_ImageComparisonViewpoint;
        TimeSpan m_Timeout;

        /// <summary>
        /// The viewpoint transform to use for the image comparison.
        /// </summary>
        public Transform ImageComparisonViewpoint
        {
            get => m_ImageComparisonViewpoint;
            internal set => m_ImageComparisonViewpoint = value;
        }

        /// <summary>
        /// The maximum time to wait for the scene view to update (for async shader compilation etc.)
        /// </summary>
        /// <remarks>
        /// This is used in the SceneView setup to wait for the viewpoint to update to the correct state and for shader compilation to finish.
        /// If you have a scene that takes a long time to compile shaders or the viewpoint is very far from the default scene view camera position,
        /// you may need to increase this value to ensure the scene view is ready for capture.
        /// An error will be logged if the timeout is exceeded.
        /// </remarks>
        public TimeSpan Timeout
        {
            get => m_Timeout;
            internal set => m_Timeout = value;
        }

        /// <summary>
        /// Creates a new instance of SceneViewCaptureSettings.
        /// </summary>
        /// <param name="width">The width of the SceneView window</param>
        /// <param name="height">The height of the SceneView window</param>
        /// <param name="delayBeforeCapture">How long to wait immediately before capturing</param>
        /// <param name="timeout">The maximum time to wait for async operations to complete</param>
        /// <param name="imageComparisonViewpoint">The world-space viewpoint to make the capture from</param>
        /// <param name="additionalSetupActions">Additional actions to perform immediately right before the final delay</param>
        public SceneViewCaptureSettings(
            int width,
            int height,
            TimeSpan delayBeforeCapture,
            TimeSpan timeout,
            Transform imageComparisonViewpoint,
            params Action<EditorWindow>[] additionalSetupActions
        )
            : base(width, height, delayBeforeCapture, additionalSetupActions)
        {
            if (imageComparisonViewpoint is null)
                throw new ArgumentNullException(nameof(imageComparisonViewpoint));

            this.m_Timeout = timeout;
            this.m_ImageComparisonViewpoint = imageComparisonViewpoint;
        }

        /// <summary>
        /// Default settings for capturing a SceneView.
        /// </summary>
        /// <remarks>
        /// These settings are used when no specific settings are provided.
        /// The default width and height are both 512, the delay before capture is zero,
        /// and the timeout is 10 seconds.
        /// The image comparison viewpoint is set to the last active scene view camera transform.
        /// </remarks>
        public new static SceneViewCaptureSettings Default =>
            new SceneViewCaptureSettings(
                width: k_DefaultCaptureSize,
                height: k_DefaultCaptureSize,
                delayBeforeCapture: TimeSpan.Zero,
                timeout: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 10),
                imageComparisonViewpoint: SceneView.lastActiveSceneView.camera.transform
            );
    }

    /// <summary>
    /// Capture the current scene view into a Texture2D for use in ImageAssert tests.
    /// </summary>
    public static class EditorWindowCapture
    {
        /// <summary>
        /// Captures a scene view from the perspective of the chosen viewpoint transform.
        /// </summary>
        /// <param name="window"> An existing window that will be used for the capture. </param>
        /// <param name="settings"> Settings for the capture. </param>
        /// <returns> A Texture2D containing the captured scene view. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when the window is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the width or height is less than or equal to zero. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the wait time is negative. </exception>
        /// <exception cref="ArgumentNullException"> Thrown when no viewpoint transform was provided. </exception>
        public static async Awaitable<Texture2D> CaptureAsync(EditorWindow window, EditorWindowCaptureSettings settings)
        {
            if (window is null)
                throw new ArgumentNullException(nameof(window));

            if (settings is null)
                throw new ArgumentNullException(nameof(settings));

            // Set up the window
            window.Focus();
            window.minSize = new Vector2(settings.Width, settings.Height);
            window.maxSize = new Vector2(settings.Width, settings.Height);
            await Task.Delay(1).ConfigureAwait(true);

            // Do any additional setup for specific window types
            if (window is SceneView sceneView)
            {
                await SetUpSceneViewAsync(sceneView, settings as SceneViewCaptureSettings).ConfigureAwait(true);
            }

            // Perform any additional setup actions provided
            if (settings.AdditionalSetupActions is not null)
                foreach (var action in settings.AdditionalSetupActions)
                    if (action is not null)
                        action(window);
                    else
                        GraphicsTestLogger.Log(
                            LogType.Warning,
                            "Null action provided in the additional setup actions list"
                        );

            // Additional wait
            await Task.Delay((int)settings.DelayBeforeCapture.TotalMilliseconds).ConfigureAwait(true);

            // Capture and set to the active RenderTexture
            var backBufferCapture = RenderTexture.GetTemporary(
                settings.Width,
                settings.Height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );
            var result = InternalEditorUtility.CaptureEditorWindow(window, backBufferCapture);

            if (!result)
            {
                GraphicsTestLogger.Log(LogType.Error, "Failed to capture the EditorWindow.");
                RenderTexture.ReleaseTemporary(backBufferCapture);
                return null;
            }

            RenderTexture.active = backBufferCapture;

            // Apply to our Tex2D
            var capturedTexture = new Texture2D(settings.Width, settings.Height, TextureFormat.RGB24, false, true);
            capturedTexture.ReadPixels(new Rect(0, 0, settings.Width, settings.Height), 0, 0);
            capturedTexture.Apply();

            // Clean up
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(backBufferCapture);

            return capturedTexture;
        }

        static async Task SetUpSceneViewAsync(SceneView sceneView, SceneViewCaptureSettings settings)
        {
            // Move the scene view camera to the scene's MainCamera
            sceneView.AlignViewToObject(settings.ImageComparisonViewpoint);

            await Task.WhenAll(
                    AlignTransformsAsync(
                        sceneView.camera.transform,
                        settings.ImageComparisonViewpoint,
                        (int)settings.Timeout.TotalMilliseconds
                    ),
                    WaitForShadersToCompileAsync((int)settings.Timeout.TotalMilliseconds)
                )
                .ConfigureAwait(true);
        }

        static async Task AlignTransformsAsync(Transform a, Transform b, int timeoutMillis)
        {
            while (!AreTransformsAligned(a, b))
            {
                if (timeoutMillis-- <= 0)
                {
                    GraphicsTestLogger.Log(LogType.Error, "Transforms did not align in time. Timeout exceeded.");
                    return;
                }

                await Task.Delay(1).ConfigureAwait(true);
            }
        }

        static async Task WaitForShadersToCompileAsync(int timeoutMillis)
        {
            var asyncAllowedPriorState = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (ShaderUtil.anythingCompiling)
                {
                    if (sw.ElapsedMilliseconds >= timeoutMillis)
                    {
                        GraphicsTestLogger.Log(
                            LogType.Error,
                            "Shaders did not finish compiling in time. Timeout exceeded."
                        );
                        return;
                    }

                    await Task.Delay(1).ConfigureAwait(true);
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncAllowedPriorState;
            }
        }

        static bool AreTransformsAligned(Transform a, Transform b) =>
            a.position == b.position && a.rotation.eulerAngles == b.rotation.eulerAngles;
    }
}
