using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Utility class for capturing images from the back buffer or render textures.
    /// </summary>
    public static class ImageCapture
    {
        /// <summary>
        /// Captures frames from the backbuffer into the specified Texture2D format. The resolution of the screen will be used for the texture's dimensions.
        /// </summary>
        /// <remarks>Supported format are RGBA32, ARGB32, RGB24, ARGBFloat, RGBAFloat, RGBAHalf, R16, RG16, R8.
        /// Capture might fail on some platforms if using other texture formats.</remarks>
        /// <param name="textureOutputFormat">Texture output format</param>
        /// <returns>An enumerable collection of Texture2Ds</returns>
        public static IEnumerable<Texture2D> CaptureBackbuffer(TextureFormat textureOutputFormat)
        {
            yield return CaptureBackBuffer(textureOutputFormat, new Rect(0, 0, Screen.width, Screen.height));
        }

        /// <summary>
        /// Captures frames from the backbuffer into the specified Texture2D format, using the capture rectangle within the current screen resolution.
        /// </summary>
        /// <param name="textureOutputFormat">Texture output format</param>
        /// <param name="captureRect">The rectangle that corresponds to the location and dimension of the capture</param>
        /// <returns>A texture2D of the specified texture format and rectangle size</returns>
        public static Texture2D CaptureBackBuffer(TextureFormat textureOutputFormat, Rect captureRect)
        {
            // Capture the back buffer into a new texture
            var actual = new Texture2D((int)captureRect.width, (int)captureRect.height, textureOutputFormat, false);
            actual.ReadPixels(captureRect, 0, 0, false); // grab screen pixels
            actual.Apply();

            return actual;
        }

        /// <summary>
        /// Captures frames from the specified camera into a Texture2D of the specified format.
        /// </summary>
        /// <param name="captureCam">Camera from which the texture will be captured</param>
        /// <param name="textureOutputFormat">Texture output format</param>
        /// <param name="settings">Camera capture settings</param>
        /// <param name="numberOfRenderedFramesBeforeCapture">Number of frames to render before capturing. When running within the editor, an extra frame needs to be rendered in order to make sure shader variants are ready.</param>
        /// <returns>An enumerable collection of Texture2Ds</returns>
        public static IEnumerable<Texture2D> CaptureFromCamera(
            Camera captureCam,
            TextureFormat textureOutputFormat,
            CameraCaptureSettings settings,
            int numberOfRenderedFramesBeforeCapture = 0
        )
        {
            var legacySettings = new ImageComparisonSettings
            {
                TargetMSAASamples = settings.msaaSamples,
                UseHDR = settings.useHDR,
                TargetHeight = settings.targetHeight,
                TargetWidth = settings.targetWidth,
            };

            yield return CaptureFromCamera(
                new[] { captureCam },
                textureOutputFormat,
                legacySettings,
                numberOfRenderedFramesBeforeCapture
            );
        }

        /// <summary>
        /// Resizes the texture2D to the desired dimension, with the specified format, using a bilinear algorithm.
        /// </summary>
        /// <param name="targetFormat">Target format</param>
        /// <param name="targetSize">Target size in in a tuple format</param>
        /// <param name="textureToResize">The texture to resize</param>
        /// <returns>The resized texture</returns>
        public static Texture2D BilinearResize(
            TextureFormat targetFormat,
            (int ResolutionX, int ResolutionY) targetSize,
            Texture2D textureToResize
        )
        {
            var resizedTexture = new Texture2D(targetSize.ResolutionX, targetSize.ResolutionY, targetFormat, false);

            resizedTexture = ResizeInto(textureToResize, resizedTexture);
            resizedTexture.Apply();

            return resizedTexture;
        }

        /// <summary>
        /// Saves a Texture2D to the ActualImages folder in PNG or EXR format.
        /// </summary>
        /// <param name="texture">Texture to be saved</param>
        /// <param name="nameWithoutExtension">The name of the file. The extension .png is added by default.</param>
        /// <param name="alsoSaveToDevicePersistentStorage">Additionally to the ActualImages folder, also saves to the device persistent storage so that it's available after reboot.</param>
        /// <param name="useEXR">Save in EXR format for HDR format. If the image is not using an HDR texture format, a warning will be logged.</param>
        /// <exception cref="ArgumentNullException">Texture needs to be not null</exception>
        /// <exception cref="ArgumentException">An image needs to be provided</exception>
        public static void SaveAsActual(
            Texture2D texture,
            string nameWithoutExtension,
            bool alsoSaveToDevicePersistentStorage = false,
            bool useEXR = false
        )
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (string.IsNullOrEmpty(nameWithoutExtension))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(nameWithoutExtension));
            }

            // comparisonIsSuccess is false because SaveAsActual is an unconditional save,
            // not a comparison result. The save is triggered by SaveImagesOnFailure = true.
            var exporter = ComparisonImageExporterProvider.Instance;
            exporter.WriteImages(
                texture,
                null,
                null,
                false,
                new LegacyImageExportOptions
                {
                    ActualImageFileName = nameWithoutExtension,
                    FileExtension = useEXR ? "exr" : "png",
                    SaveImagesOnFailure = true,
                    SaveImagesToDiskOnFailure = alsoSaveToDevicePersistentStorage,
                }
            );
        }

        /// <summary>
        /// Capture a frame from a camera with the specified format and using the capture options of the specified ImageComparisonSettings. This is used
        /// within the LegacyImageComparison algorithm.
        /// </summary>
        /// <param name="cameras">A collection of cameras that will be used for the rendering. All cameras render to the same render texture, useful for stacking and overlays.</param>
        /// <param name="format">Format of the texture2D</param>
        /// <param name="settings">Legacy image comparison settings, specifying the resolution, whether HDR is used, number of MSAA samples</param>
        /// <param name="frameDelay">Number of frames to render before capturing. 1 by default to prevent issue with shader variants when testing within the editor.</param>
        /// <returns>A Texture2D containing the captured frame</returns>
        internal static Texture2D CaptureFromCamera(
            IEnumerable<Camera> cameras,
            TextureFormat format,
            ImageComparisonSettings settings,
            int frameDelay = 1
        )
        {
            var width = settings.TargetWidth;
            var height = settings.TargetHeight;
            var samples = settings.TargetMSAASamples;

            // Some HDRP test fail with HDRP batcher because shaders variant are compiled "on the fly" in editor mode.
            // Persistent PerMaterial CBUFFER is build during culling, but some nodes could use new variants and CBUFFER will be up to date next frame.
            // ( this is editor specific, standalone player has no frame delay issue because all variants are ready at init stage )
            // This adds a dummy rendered frame before doing the real rendering and compare images ( test already has frame delay, but there is no rendering )

            var defaultFormat =
                (settings.UseHDR)
                    ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR)
                    : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            var desc = new RenderTextureDescriptor(width, height, defaultFormat, 24) { msaaSamples = samples };
            var rt = RenderTexture.GetTemporary(desc);
            Texture2D actual = null;

            try
            {
                UnityEngine.Graphics.SetRenderTarget(rt);
                GL.Clear(true, true, Color.black);
                UnityEngine.Graphics.SetRenderTarget(null);

                for (var i = 0; i < frameDelay + 1; i++) // x frame delay + the last one is the one really tested ( ie 5 frames delay means 6 frames are rendered )
                {
                    foreach (var camera in cameras)
                    {
                        camera.targetTexture = rt;
                        camera.Render();
                        camera.targetTexture = null;
                    }

                    // only proceed the test on the last rendered frame
                    if (frameDelay == i)
                    {
                        actual = CaptureFrame(format, settings, width, height, desc, rt);
                    }
                }

                return actual;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        static Texture2D CaptureFrame(
            TextureFormat format,
            ImageComparisonSettings settings,
            int width,
            int height,
            RenderTextureDescriptor desc,
            RenderTexture rt
        )
        {
            var actual = new Texture2D(width, height, format, false, true);
            RenderTexture dummy = null;

            try
            {
                if (settings.UseHDR)
                {
                    desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                    dummy = RenderTexture.GetTemporary(desc);
                    UnityEngine.Graphics.Blit(rt, dummy);
                }
                else
                    RenderTexture.active = rt;

                actual.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                actual.Apply();
            }
            finally
            {
                RenderTexture.active = null;
                if (dummy != null)
                    RenderTexture.ReleaseTemporary(dummy);
            }
            return actual;
        }

        internal static Texture2D ResizeInto(Texture2D source, Texture2D dest)
        {
            var format = source != null ? source.format : TextureFormat.ARGB32;
            var rtFormat = GetCompatibleRenderTextureFormat(format);
            var rt = RenderTexture.GetTemporary(dest.width, dest.height, 0, rtFormat);
            var prev = RenderTexture.active;
            try
            {
                UnityEngine.Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var newImage = new Texture2D(dest.width, dest.height, format, false, true);
                newImage.ReadPixels(new Rect(0, 0, dest.width, dest.height), 0, 0);
                newImage.Apply();
                return newImage;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                Object.Destroy(dest);
            }
        }

        static RenderTextureFormat GetCompatibleRenderTextureFormat(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.RGBAFloat => RenderTextureFormat.ARGBFloat,
                TextureFormat.RGBAHalf => RenderTextureFormat.ARGBHalf,
                TextureFormat.RFloat => RenderTextureFormat.RFloat,
                TextureFormat.RHalf => RenderTextureFormat.RHalf,
                TextureFormat.RGFloat => RenderTextureFormat.RGFloat,
                TextureFormat.RGHalf => RenderTextureFormat.RGHalf,
                _ => RenderTextureFormat.ARGB32,
            };
        }
    }
}
