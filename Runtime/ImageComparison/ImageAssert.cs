using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.LegacyColorDifference;
#if UNITY_EDITOR
using UnityEditor.Profiling;
using UnityEditorInternal;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provides test assertion helpers for working with images.
    /// </summary>
    public class ImageAssert
    {
        const int k_BatchSize = 1024;
        const int k_RenderTextureDepthBits = 24;
        const float k_BadPixelCountAdjustment = 0.1f;
        internal const int k_KBackBufferWidth = 1920;
        internal const int k_KBackBufferHeight = 1080;

        /// <summary>
        /// Render an image from the given camera and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image to compare against.</param>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        /// <param name="expectedImagePathLog"> The log message to display if the test fails. </param>
        /// <param name="saveFailedImageToDisk"> If true, the actual image will be saved to disk if the test fails. </param>
        public static void AreEqual(
            Texture2D expected,
            Camera camera,
            ImageComparisonSettings settings = null,
            string expectedImagePathLog = null,
            bool saveFailedImageToDisk = false
        )
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            AreEqual(expected, new[] { camera }, settings, expectedImagePathLog, saveFailedImageToDisk);
        }

        /// <summary>
        /// Render an image from the given cameras and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image to compare against.</param>
        /// <param name="cameras">The cameras to render from. All cameras will be rendered to the same rendered texture. This useful when testing camera stacking/overlay.</param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        /// <param name="expectedImagePathLog"> The log message to display if the test fails. </param>
        /// <param name="saveFailedImageToDisk"> If true, the actual image will be saved to disk if the test fails. </param>
        public static void AreEqual(
            Texture2D expected,
            IEnumerable<Camera> cameras,
            ImageComparisonSettings settings = null,
            string expectedImagePathLog = null,
            bool saveFailedImageToDisk = false
        )
        {
            var expectedFormatOrDefault = expected?.format ?? TextureFormat.ARGB32;

            if (cameras == null)
                throw new ArgumentNullException(nameof(cameras));

            var cameraList = new List<Camera>();
            foreach (var c in cameras)
            {
                if (c != null)
                    cameraList.Add(c);
            }
            cameras = cameraList;

            settings ??= new ImageComparisonSettings();

            Texture2D actual = null;
            try
            {
                if (RuntimeSettings.reuseTestsForXR)
                {
                    var w = Screen.width;
                    var h = Screen.height;
                    actual = new Texture2D(w, h, expectedFormatOrDefault, false, true);
                    actual.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                    actual.Apply();
                }
                else if (settings.UseBackBuffer)
                {
                    GraphicsTestLogger.Log(
                        $"Capturing image from backbuffer with screen dimension w:{Screen.width} h:{Screen.height}"
                    );
                    actual = ImageCapture.CaptureBackBuffer(
                        expectedFormatOrDefault,
                        new Rect(0, 0, Screen.width, Screen.height)
                    );
                }
                else
                {
                    Assert.True(HasAny(cameras), "Invalid test scene, no active cameras found for image capture.");
                    actual = ImageCapture.CaptureFromCamera(cameras, expectedFormatOrDefault, settings);
                }
                AreEqual(expected, actual, settings, expectedImagePathLog, true, saveFailedImageToDisk);
            }
            finally
            {
                if (actual != null)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(actual);
#else
                    Object.Destroy(actual);
#endif
                }
            }
        }

        /// <summary>
        /// Render an image from the given cameras and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image to compare against.</param>
        /// <param name="cameras">The cameras to render from. All cameras will be rendered to the same rendered texture. This is useful when testing camera stacking/overlay.</param>
        /// <param name="callback">Optional callback with boolean parameter to represent if AreEqual is successful </param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        /// <param name="expectedImagePathLog"> The log message to display if the test fails. </param>
        /// <param name="saveFailedImageToDisk"> If true, the actual image will be saved to disk if the test fails. </param>
        /// <returns>
        /// An enumerator that can be used to wait for the image comparison to complete.
        /// </returns>
        public static IEnumerator AreEqualAsync(
            Texture2D expected,
            IEnumerable<Camera> cameras,
            Action<bool> callback = null,
            ImageComparisonSettings settings = null,
            string expectedImagePathLog = null,
            bool saveFailedImageToDisk = false
        )
        {
            if (cameras == null)
            {
                if (callback != null)
                {
                    callback(false);
                }
                yield break;
            }

            settings ??= new ImageComparisonSettings();

            var width = settings.TargetWidth;
            var height = settings.TargetHeight;
            var samples = settings.TargetMSAASamples;
            var format = expected != null ? expected.format : TextureFormat.ARGB32;

            // Some HDRP test fail with HDRP batcher because shaders variant are compiled "on the fly" in editor mode.
            // Persistent PerMaterial CBUFFER is build during culling, but some nodes could use new variants and CBUFFER will be up to date next frame.
            // ( this is editor specific, standalone player has no frame delay issue because all variants are ready at init stage )
            // This PR adds a dummy rendered frame before doing the real rendering and compare images ( test already has frame delay, but there is no rendering )
            const int dummyRenderedFrameCount = 1;
            var ldrFormat =
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            GraphicsFormat.R8G8B8A8_SRGB;
#else
            SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
#endif
            var defaultFormat = (settings.UseHDR) ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR) : ldrFormat;
            var desc = new RenderTextureDescriptor(width, height, defaultFormat, k_RenderTextureDepthBits) { msaaSamples = samples };
            var rt = RenderTexture.GetTemporary(desc);
            UnityEngine.Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.black);
            UnityEngine.Graphics.SetRenderTarget(null);

            Texture2D actual = null;
            var succeeded = false;
            try
            {
                if (RuntimeSettings.reuseTestsForXR)
                {
                    var w = Screen.width;
                    var h = Screen.height;
                    actual = new Texture2D(w, h, format, false, true);
                    actual.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                    actual.Apply();
                }
                else if (settings.UseBackBuffer)
                {
                    yield return CaptureBackBufferAsync(ldrFormat, format, value => actual = value);
                }
                else
                {
                    for (var i = 0; i < dummyRenderedFrameCount + 1; i++) // x frame delay + the last one is the one really tested ( ie 5 frames delay means 6 frames are rendered )
                    {
                        foreach (var camera in cameras)
                        {
                            if (camera == null)
                            {
                                continue;
                            }
                            camera.targetTexture = rt;
                            camera.Render();
                            camera.targetTexture = null;
                        }

                        // only proceed the test on the last rendered frame
                        if (dummyRenderedFrameCount == i)
                        {
                            actual = new Texture2D(width, height, format, false, true);
                            RenderTexture dummy = null;
                            if (settings.UseHDR)
                            {
                                desc.graphicsFormat = ldrFormat;
                                dummy = RenderTexture.GetTemporary(desc);
                                UnityEngine.Graphics.Blit(rt, dummy);
                            }

                            var req = AsyncGPUReadback.Request(dummy ?? rt, 0, ldrFormat);
                            yield return new WaitUntil(() => req.done);

                            if (req.hasError)
                                throw new InvalidOperationException("AsyncGPUReadback request failed. The GPU readback could not be completed.");

                            var data = req.GetData<Color32>().ToArray();
                            actual.SetPixels32(data);

                            if (dummy != null)
                            {
                                RenderTexture.ReleaseTemporary(dummy);
                            }

                            actual.Apply();
                        }
                    }
                }
                AreEqual(expected, actual, settings, expectedImagePathLog, true, saveFailedImageToDisk);
                succeeded = true;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                if (actual != null)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(actual);
#else
                    UnityEngine.Object.Destroy(actual);
#endif
                }
                callback?.Invoke(succeeded);
            }
        }

        /// <summary>
        /// Compares an image to a 'reference' image to see if it looks correct. Assumes linear HDR images (RGBAFloat or RGBAHalf).
        /// </summary>
        /// <param name="expected">What the image is supposed to look like.</param>
        /// <param name="actual">What the image actually looks like.</param>
        /// <param name="settings">Optional settings that control how the comparison is performed. Can be null, in which case the images are required to be exactly identical.</param>
        /// <param name="expectedImagePathLog"> The log message to display if the test fails. </param>
        /// <param name="saveFailedImage"> If true, the actual image will be saved if the test fails. </param>
        /// <param name="saveFailedImageToDisk"> If true, the actual image will be saved to disk if the test fails. </param>
        public static void AreEqualLinearHDR(
            Texture2D expected,
            Texture2D actual,
            ImageComparisonSettings settings = null,
            string expectedImagePathLog = null,
            bool saveFailedImage = true,
            bool saveFailedImageToDisk = false
        )
        {
            if (!actual)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            var hdrFormat = actual.format is TextureFormat.RGBAHalf or TextureFormat.RGBAFloat;

            if (!hdrFormat)
            {
                throw new ArgumentException(
                    $"Actual image is using an invalid format: {actual.format}. Expected format should be RGBAHalf or RGBAFloat."
                );
            }

            var options = new LegacyImageExportOptions
            {
                ActualImageFileName = settings?.ActualImageFileName,
                SaveActualImageOnSuccess = GraphicsTestBuildSettings.LoadOrDefault().SaveActualImages,
                SaveImagesOnFailure = saveFailedImage,
                SaveImagesToDiskOnFailure = saveFailedImageToDisk,
                FileExtension = "exr",
            };

            try
            {
                var differenceMessage = CheckBasicImagePropertiesMatch(expected, actual, expectedImagePathLog, "exr");

                if (differenceMessage != null)
                {
                    throw new AssertionException(differenceMessage);
                }

                var expectedHasNonFinite = ContainsNaN(expected);
                if (expectedHasNonFinite)
                    Assert.That(
                        CheckForNonFiniteValues(expected, expected.name, out var messageExpected),
                        Is.True,
                        $"Expected image has non-finite pixels: {messageExpected}"
                    );

                var actualHasNonFinite = ContainsNaN(actual);
                if (actualHasNonFinite)
                    Assert.That(
                        CheckForNonFiniteValues(actual, actual.name, out var messageActual),
                        Is.True,
                        $"Actual image has non-finite pixels: {messageActual}"
                    );
            }
            catch (AssertionException)
            {
                ComparisonImageExporterProvider.Instance.WriteImages(actual, null, null, false, options);

                throw;
            }

            using var expectedPixels = new NativeArray<Color>(expected.GetPixels(0), Allocator.TempJob);
            using var actualPixels = new NativeArray<Color>(actual.GetPixels(0), Allocator.TempJob);
            using var diffPixels = new NativeArray<Color>(expectedPixels.Length, Allocator.TempJob);
            settings ??= new ImageComparisonSettings();

            var imageComparisonRanAndFailed = false;

            var testBadPixelsCount = settings.ActiveImageTests.HasFlag(
                ImageComparisonSettings.ImageTests.IncorrectPixelsCount
            );
            var testRmse = settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.RMSE);

            var batchCount = (expectedPixels.Length + k_BatchSize - 1) / k_BatchSize;
            using var batchSquaredErrorSums = new NativeArray<float>(batchCount, Allocator.TempJob);
            using var batchBadPixelCounts = new NativeArray<int>(batchCount, Allocator.TempJob);

            new ComputeLinearHDRImageDiffJob
            {
                expected = expectedPixels,
                actual = actualPixels,
                diff = diffPixels,
                pixelThreshold = settings.PerPixelCorrectnessThreshold,
                pixelCount = expectedPixels.Length,
                batchSize = k_BatchSize,
                batchSquaredErrorSums = batchSquaredErrorSums,
                batchBadPixelCounts = batchBadPixelCounts,
            }
                .Schedule(batchCount, 1)
                .Complete();

            var pixelCount = expected.width * expected.height;
            var mseSum = 0f;
            foreach (var t in batchSquaredErrorSums)
                mseSum += t;

            var mse = mseSum / (pixelCount * 4);
            var rmse = Mathf.Sqrt(mse);
            var badPixelsSum = 0;
            foreach (var t in batchBadPixelCounts)
                badPixelsSum += t;

            var badPixelsMean = Mathf.Max(0f, (badPixelsSum - k_BadPixelCountAdjustment) / pixelCount);
            Assert.That(float.IsNaN(mse), Is.False, "MSE value is NaN.");
            Assert.That(float.IsNaN(rmse), Is.False, "RMSE value is NaN.");
            Assert.That(float.IsNaN(badPixelsMean), Is.False, "BadPixelsMean value is NaN.");

            var testFailureDescription = EvaluateTestResults(
                settings,
                expectedImagePathLog,
                testRmse,
                rmse,
                testBadPixelsCount,
                badPixelsMean,
                ref imageComparisonRanAndFailed
            );

            if (imageComparisonRanAndFailed)
            {
                GraphicsTestLogger.Log(LogType.Log, testFailureDescription);

                HandleTestFailureImages(expected, actual, diffPixels, options);

                throw new AssertionException(testFailureDescription);
            }

            ComparisonImageExporterProvider.Instance.WriteImages(actual, null, null, true, options);
        }

        static string EvaluateTestResults(
            ImageComparisonSettings settings,
            string expectedImagePathLog,
            bool testRmse,
            float rmse,
            bool testBadPixelsCount,
            float badPixelsMean,
            ref bool imageComparisonRanAndFailed
        )
        {
            var testFailureDescription = string.Empty;
            if (testRmse && rmse > settings.RMSEThreshold)
            {
                imageComparisonRanAndFailed = true;
                testFailureDescription =
                    $"Failed RMSE threshold test ({rmse} is bigger than {settings.RMSEThreshold} threshold). {expectedImagePathLog}";
            }

            if (!imageComparisonRanAndFailed && testBadPixelsCount && badPixelsMean > settings.IncorrectPixelsThreshold)
            {
                imageComparisonRanAndFailed = true;
                testFailureDescription =
                    $"Failed per pixel threshold test ({badPixelsMean} is bigger than {settings.IncorrectPixelsThreshold} threshold). {expectedImagePathLog}";
            }

            return testFailureDescription;
        }

        static void HandleTestFailureImages(
            Texture2D expected,
            Texture2D actual,
            NativeArray<Color> diffPixels,
            LegacyImageExportOptions options
        )
        {
            var diffPixelsArray = new Color32[expected.width * expected.height];
            for (var i = 0; i < diffPixels.Length; i++)
            {
                diffPixelsArray[i] = diffPixels[i];
            }

            ComparisonImageExporterProvider.Instance.WriteImages(actual, expected, diffPixelsArray, false, options);
        }

        static bool CheckForNonFiniteValues(Texture2D texture, string textureName, out string message)
        {
            if (texture == null)
            {
                message = $"[NonFiniteCheck] {textureName} is null";
                return false;
            }

            var pixels = texture.GetPixels(0);
            ScanPixelsForNonFinite(pixels, out var nanCount, out var posInfCount, out var negInfCount, out var firstNonFiniteIndex, out var firstNonFiniteColor);

            if (nanCount > 0 || posInfCount > 0 || negInfCount > 0)
            {
                var x = firstNonFiniteIndex % texture.width;
                var y = firstNonFiniteIndex / texture.width;
                message =
                    $"[NonFiniteCheck] {textureName} (format={texture.format} size={texture.width}x{texture.height}) contains non-finite values! "
                    + $"NaN={nanCount}, +Inf={posInfCount}, -Inf={negInfCount}. "
                    + $"First at index {firstNonFiniteIndex} ({x},{y}): RGBA=({firstNonFiniteColor.r}, {firstNonFiniteColor.g}, {firstNonFiniteColor.b}, {firstNonFiniteColor.a})";
                return false;
            }

            message = $"[NonFiniteCheck] {textureName} has no non-finite values (checked {pixels.Length} pixels)";
            return true;
        }

        static void ScanPixelsForNonFinite(
            Color[] pixels,
            out int nanCount,
            out int posInfCount,
            out int negInfCount,
            out int firstNonFiniteIndex,
            out Color firstNonFiniteColor)
        {
            nanCount = 0;
            posInfCount = 0;
            negInfCount = 0;
            firstNonFiniteIndex = -1;
            firstNonFiniteColor = default;

            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];

                var isNaN = float.IsNaN(p.r) || float.IsNaN(p.g) || float.IsNaN(p.b) || float.IsNaN(p.a);
                var isPosInf = float.IsPositiveInfinity(p.r) || float.IsPositiveInfinity(p.g) || float.IsPositiveInfinity(p.b) || float.IsPositiveInfinity(p.a);
                var isNegInf = float.IsNegativeInfinity(p.r) || float.IsNegativeInfinity(p.g) || float.IsNegativeInfinity(p.b) || float.IsNegativeInfinity(p.a);

                if (isNaN) nanCount++;
                if (isPosInf) posInfCount++;
                if (isNegInf) negInfCount++;

                if ((isNaN || isPosInf || isNegInf) && firstNonFiniteIndex < 0)
                {
                    firstNonFiniteIndex = i;
                    firstNonFiniteColor = p;
                }
            }
        }

        /// <summary>
        /// Checks if the given image contains NaN or non-finite values.
        /// </summary>
        /// <param name="pixels">The image to check.</param>
        internal static bool ContainsNaN(Texture2D pixels)
        {
            return ContainsNonFiniteValues(pixels);
        }

        static bool ContainsNonFiniteValues(Texture2D texture)
        {
            var pixelArray = texture.GetPixels(0);
            ScanPixelsForNonFinite(pixelArray, out var nanCount, out var posInfCount, out var negInfCount, out _, out _);
            return nanCount > 0 || posInfCount > 0 || negInfCount > 0;
        }

        /// <summary>
        /// Represents the results from an image comparison operation.
        /// </summary>
        public struct ImageComparisonResults : IEquatable<ImageComparisonResults>
        {
            /// <summary>
            /// Indicates whether the image comparison was successful.
            /// </summary>
            public bool Success { get; init; }

            /// <summary>
            /// The average delta E value from the image comparison.
            /// </summary>
            public float AverageDeltaE { get; set; }

            /// <summary>
            /// The number of bad pixels found during the image comparison.
            /// </summary>
            public float BadPixelsCount { get; set; }

            /// <summary>
            /// Gets or sets whether the average deltaE is within threshold
            /// </summary>
            public bool AverageDeltaEWithinThreshold { get; set; }

            /// <summary>
            /// Gets or sets whether the bad pixel ratio is within threshold
            /// </summary>
            public bool BadPixelsCountWithinThreshold { get; set; }

            /// <summary>
            /// Compares this instance with another instance of <see cref="ImageComparisonResults"/>.
            /// </summary>
            /// <param name="other">
            /// The other instance to compare with.
            /// </param>
            /// <returns>
            /// True if the instances are equal; otherwise, false.
            /// </returns>
            public bool Equals(ImageComparisonResults other)
            {
                return Success.Equals(other.Success)
                    && AverageDeltaE.Equals(other.AverageDeltaE)
                    && BadPixelsCount.Equals(other.BadPixelsCount);
            }

            /// <summary>
            /// Compares this instance with another object.
            /// </summary>
            /// <param name="obj">
            /// The object to compare with.
            /// </param>
            /// <returns>
            /// True if the object is an instance of <see cref="ImageComparisonResults"/> and is equal to this instance; otherwise, false.
            /// </returns>
            public override bool Equals(object obj)
            {
                return obj is ImageComparisonResults other && Equals(other);
            }

            /// <summary>
            /// Generates a hash code for this instance.
            /// </summary>
            /// <returns>
            /// The hash code for this instance.
            /// </returns>
            public override int GetHashCode()
            {
                return HashCode.Combine(Success, AverageDeltaE, BadPixelsCount);
            }
        }

        /// <summary>
        /// Compares an image to a 'reference' image to see if it looks correct.
        /// </summary>
        /// <param name="expected">
        /// What the image is supposed to look like.
        /// </param>
        /// <param name="actual">
        /// What the image actually looks like.
        /// </param>
        /// <param name="settings">
        /// Optional settings that control how the comparison is performed. Can be null, in which case the images are required to be exactly identical.
        /// </param>
        /// <param name="expectedImagePathLog">
        /// The log message to display if the test fails.
        /// </param>
        /// <param name="saveFailedImage">
        /// If true, the actual image will be saved if the test fails.
        /// </param>
        /// <param name="saveFailedImageToDisk">
        /// If true, the actual image will be saved to disk if the test fails.
        /// </param>
        /// <param name="logMessages">
        /// If true, the log messages will be displayed.
        /// </param>
        public static void AreEqual(
            Texture2D expected,
            Texture2D actual,
            ImageComparisonSettings settings = null,
            string expectedImagePathLog = null,
            bool saveFailedImage = true,
            bool saveFailedImageToDisk = false,
            bool logMessages = true
        )
        {
            AreEqual(
                expected,
                actual,
                settings,
                expectedImagePathLog,
                saveFailedImage,
                saveFailedImageToDisk,
                logMessages,
                out var _
            );
        }

        static bool HasAny(IEnumerable<Camera> cameras)
        {
            foreach (var _ in cameras)
                return true;
            return false;
        }

        static string CheckBasicImagePropertiesMatch(Texture2D expected, Texture2D actual, string expectedImagePathLog, string fileExtension = "png")
        {
            switch (true)
            {
                case var _ when !expected:
                    return $"No reference image was provided.{Environment.NewLine}"
                        + "The actual (rendered) image will be saved as: "
                        + $"{ComparisonImageExporterProvider.Instance.FindImageDirectoryName()}/{ComparisonImageExporterProvider.Instance.FindImageName()}.{fileExtension}{Environment.NewLine}"
                        + $"{expectedImagePathLog}";
                case var _ when actual.width != expected.width:
                    return $"{expectedImagePathLog} The expected image had width {expected.width}px, "
                        + $"but the actual image had width {actual.width}px.";

                case var _ when actual.height != expected.height:
                    return $"{expectedImagePathLog} The expected image had height {expected.height}px, "
                        + $"but the actual image had height {actual.height}px.";

                case var _ when actual.format != expected.format:
                    return $"{expectedImagePathLog} The expected image had format {expected.format}, "
                        + $"but the actual image had format {actual.format}.";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Compares an image to a 'reference' image to see if it looks correct.
        /// </summary>
        /// <param name="expected">What the image is supposed to look like.</param>
        /// <param name="actual">What the image actually looks like.</param>
        /// <param name="settings">Optional settings that control how the comparison is performed. Can be null, in which case the images are required to be exactly identical.</param>
        /// <param name="expectedImagePathLog">
        /// The log message to display if the test fails.
        /// </param>
        /// <param name="saveFailedImage">
        /// If true, the actual image will be saved if the test fails.
        /// </param>
        /// <param name="saveFailedImageToDisk">
        /// If true, the actual image will be saved to disk if the test fails.
        /// </param>
        /// <param name="logMessages">
        /// If true, the log messages will be displayed.
        /// </param>
        /// <param name="result">
        /// The result of the image comparison.
        /// </param>
        public static void AreEqual(
            Texture2D expected,
            Texture2D actual,
            ImageComparisonSettings settings,
            string expectedImagePathLog,
            bool saveFailedImage,
            bool saveFailedImageToDisk,
            bool logMessages,
            out ImageComparisonResults result
        )
        {
            if (!actual)
                throw new ArgumentNullException(nameof(actual));

            var argumentError = CheckBasicImagePropertiesMatch(expected, actual, expectedImagePathLog);

            var loggingOptions = new LegacyImageExportOptions
            {
                LogMessages = logMessages,
                SaveActualImageOnSuccess = GraphicsTestBuildSettings.LoadOrDefault().SaveActualImages,
                SaveImagesToDiskOnFailure = saveFailedImageToDisk,
                SaveImagesOnFailure = saveFailedImage,
                ActualImageFileName = settings?.ActualImageFileName,
            };

            if (argumentError != null)
            {
                ComparisonImageExporterProvider.Instance.WriteImages(actual, null, null, false, loggingOptions);
                throw new ArgumentException(argumentError);
            }

            if (settings == null)
            {
                settings = new ImageComparisonSettings();
            }

            var algorithm = new LegacyColorDifferenceAlgorithm(settings);

            var compareResult = (LegacyColorDifferenceAggregate)algorithm.Compare(expected, actual);
            result = compareResult.ImageComparisonResults;

            ComparisonImageExporterProvider.Instance.WriteImages(
                actual,
                expected,
                compareResult.DifferencePixels,
                compareResult.ImageComparisonResults.Success,
                loggingOptions
            );

            if (!compareResult.ImageComparisonResults.Success)
            {
                throw new AssertionException(
                    $"Expected: {Environment.NewLine}{algorithm.Description}{Environment.NewLine}Actual:{Environment.NewLine}{compareResult}"
                );
            }
        }

        /// <summary>
        /// Render an image from the given camera and check if it allocated memory while doing so.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="settings">The image comparison settings for this comparison.</param>
        /// <param name="gcAllocThreshold">The threshold for the number of GC allocations.</param>
        public static void AllocatesMemory(
            Camera camera,
            ImageComparisonSettings settings = null,
            int gcAllocThreshold = 0
        )
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (settings == null)
                settings = new ImageComparisonSettings();

            var width = settings.TargetWidth;
            var height = settings.TargetHeight;

            var defaultFormat =
                (settings.UseHDR)
                    ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR)
                    : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            var desc = new RenderTextureDescriptor(width, height, defaultFormat, k_RenderTextureDepthBits);

            var gcAllocRecorder = Recorder.Get("GC.Alloc");
            gcAllocRecorder.FilterToCurrentThread();
            gcAllocRecorder.enabled = false;

            var rt = RenderTexture.GetTemporary(desc);
            try
            {
                if (!settings.UseBackBuffer && !RuntimeSettings.reuseTestsForXR)
                    camera.targetTexture = rt;

                // Render the first frame at this resolution (Alloc are allowed here)
                camera.Render();

                Profiler.BeginSample("GraphicTests_GC_Alloc_Check");
                {
                    gcAllocRecorder.enabled = true;
                    camera.Render();
                    gcAllocRecorder.enabled = false;
                }
                Profiler.EndSample();

                var allocationCountOfRenderPipeline = gcAllocRecorder.sampleBlockCount - gcAllocThreshold;

                Assert.That(
                    allocationCountOfRenderPipeline,
                    Is.LessThanOrEqualTo(0),
                    $@"Memory allocation test failed, {allocationCountOfRenderPipeline} allocations detected. Steps to find where your allocation is:
                    - Open the profiler window (ctrl-7) and enable deep profiling.
                    - Run your the test that fails and wait (it can take much longer because deep profiling is enabled).
                    - In the CPU section of the profiler, select on Hierarchy and search for the 'GraphicTests_GC_Alloc_Check' marker.
                    - This should give you one result, click on it and press f to go to the frame where it happened.
                    - Click on the GC Alloc column to sort by allocation and unfold the hierarchy under the 'GraphicTests_GC_Alloc_Check' marker."
                );

                camera.targetTexture = null;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// Render an image from the given camera and check if it allocated memory while doing so. Also outputs the callstack of the GC.Alloc found
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="settings">Settings to create the camera render target</param>
        /// <param name="overrideSrpMarkerName">Override the main marker used to check the GC.Alloc</param>
        /// <returns>
        /// An enumerator that can be used to wait for the operation to complete.
        /// </returns>
        public static IEnumerator CheckGCAllocWithCallstack(
            Camera camera,
            ImageComparisonSettings settings = null,
            string overrideSrpMarkerName = null
        )
        {
#if UNITY_EDITOR
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (settings == null)
                settings = new ImageComparisonSettings();

            var width = settings.TargetWidth;
            var height = settings.TargetHeight;

            var defaultFormat =
                (settings.UseHDR)
                    ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR)
                    : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            var desc = new RenderTextureDescriptor(width, height, defaultFormat, k_RenderTextureDepthBits);

            var rt = RenderTexture.GetTemporary(desc);
            try
            {
                if (!settings.UseBackBuffer && !RuntimeSettings.reuseTestsForXR)
                    camera.targetTexture = rt;

                ProfilerDriver.ClearAllFrames();
                ProfilerDriver.memoryRecordMode = ProfilerMemoryRecordMode.GCAlloc;
                ProfilerDriver.enabled = true;

                // Wait for memoryRecordMode to apply
                yield return new WaitForEndOfFrame();

                // Render the camera
                yield return new WaitForEndOfFrame();

                ProfilerDriver.enabled = false;
                // Wait for results to be available in the profiler
                yield return new WaitForEndOfFrame();

                var cameraRenderFrameIndex = ProfilerDriver.GetPreviousFrameIndex(Time.frameCount);
                long totalGcAllocSize = 0;

                const int mainThread = 0;
                var humanReadableCallstack = new StringBuilder();
                using (var frameData = ProfilerDriver.GetRawFrameDataView(cameraRenderFrameIndex, mainThread))
                {
                    if (!frameData.valid)
                        yield break;

                    var gcAllocMarkerId = frameData.GetMarkerId("GC.Alloc");

                    // Check if there is a GC Alloc marker in the frame
                    if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                        yield break;

                    // Check if there is the srp marker in the frame
                    var srpMarker = frameData.GetMarkerId(
                        overrideSrpMarkerName
                            ?? "UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke]"
                    );
                    if (srpMarker == FrameDataView.invalidMarkerId)
                        throw new Exception("SRP Marker not found in profiling while searching for GC.Alloc");
                    var sampleCount = frameData.sampleCount;
                    for (var i = 0; i < sampleCount; ++i)
                    {
                        if (srpMarker == frameData.GetSampleMarkerId(i))
                        {
                            var endMarkerIndex = frameData.GetSampleChildrenCountRecursive(i) + i;

                            if (i >= endMarkerIndex)
                                continue;

                            for (; i < endMarkerIndex; i++)
                            {
                                if (gcAllocMarkerId != frameData.GetSampleMarkerId(i))
                                    continue;

                                var callstack = new List<ulong>();
                                frameData.GetSampleCallstack(i, callstack);
                                foreach (var callAddress in callstack)
                                {
                                    var methodInfo = frameData.ResolveMethodInfo(callAddress);
                                    if (string.IsNullOrEmpty(methodInfo.methodName))
                                        continue;
                                    humanReadableCallstack.AppendLine(methodInfo.methodName);
                                }

                                humanReadableCallstack.AppendLine();

                                var gcAllocSize = frameData.GetSampleMetadataAsLong(i, 0);
                                totalGcAllocSize += gcAllocSize;
                            }
                        }
                    }
                }

                if (totalGcAllocSize > 0)
                    throw new Exception(
                        $@"Memory allocation test failed, {totalGcAllocSize}B of GC.Alloc detected. Callstacks:
{humanReadableCallstack}
If the callstack is not exploitable you can try to find the allocation by following these instructions:
- Open the profiler window (ctrl-7) and enable deep profiling.
- Run your the test that fails and wait (it can take much longer because deep profiling is enabled).
- In the CPU section of the profiler, select on Hierarchy and search for the 'GraphicTests_GC_Alloc_Check' marker.
- This should give you one result, click on it and press f to go to the frame where it hapended.
- Click on the GC Alloc column to sort by allocation and unfold the hierarchy under the 'GraphicTests_GC_Alloc_Check' marker."
                    );
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
            }
            yield break;
#else
            AllocatesMemory(camera, settings, 0);
            yield break;
#endif
        }

        static IEnumerator CaptureBackBufferAsync(GraphicsFormat ldrFormat, TextureFormat textureFormat, Action<Texture2D> result)
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            var rtDesc = new RenderTextureDescriptor(screenWidth, screenHeight, ldrFormat, k_RenderTextureDepthBits);
            var tempRT = RenderTexture.GetTemporary(rtDesc);

            UnityEngine.Graphics.Blit(null, tempRT);

            // D3D-style top-left origin APIs flip the image from blit; undo that here.

            RenderTexture screenRT = null;
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var (scale, offs) = (new Vector2(1, -1), new Vector2(0, 1));
                screenRT = RenderTexture.GetTemporary(rtDesc);
                UnityEngine.Graphics.Blit(tempRT, screenRT, scale, offs);
            }

            var req = AsyncGPUReadback.Request(
                SystemInfo.graphicsUVStartsAtTop ? screenRT : tempRT,
                0,
                ldrFormat
            );
            yield return new WaitUntil(() => req.done);
            var data = req.GetData<Color32>().ToArray();
            var texture = new Texture2D(screenWidth, screenHeight, textureFormat, false, true);
            texture.SetPixels32(data);

            RenderTexture.ReleaseTemporary(tempRT);
            if (screenRT)
                RenderTexture.ReleaseTemporary(screenRT);

            result(texture);
        }

        struct ComputeLinearHDRImageDiffJob : IJobParallelFor
        {
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<Color> expected;

            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<Color> actual;

            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<Color> diff;

            public float pixelThreshold;
            public int pixelCount;
            public int batchSize;

            [WriteOnly]
            public NativeArray<float> batchSquaredErrorSums;

            [WriteOnly]
            public NativeArray<int> batchBadPixelCounts;

            public void Execute(int batchIndex)
            {
                var start = batchIndex * batchSize;
                var end = Mathf.Min(start + batchSize, pixelCount);
                var squaredErrorSum = 0f;
                var badPixelCount = 0;

                for (var i = start; i < end; i++)
                {
                    var exp = expected[i];
                    var act = actual[i];

                    var deltaR = Mathf.Abs(exp.r - act.r);
                    var deltaG = Mathf.Abs(exp.g - act.g);
                    var deltaB = Mathf.Abs(exp.b - act.b);
                    var deltaA = Mathf.Abs(exp.a - act.a);
                    var maxDelta = Mathf.Max(Mathf.Max(Mathf.Max(deltaR, deltaG), deltaB), deltaA);

                    if (maxDelta > pixelThreshold)
                        badPixelCount++;
                    squaredErrorSum += deltaR * deltaR + deltaG * deltaG + deltaB * deltaB + deltaA * deltaA;
                    diff[i] = new Color(maxDelta, maxDelta, maxDelta, 1.0f);
                }

                batchSquaredErrorSums[batchIndex] = squaredErrorSum;
                batchBadPixelCounts[batchIndex] = badPixelCount;
            }
        }
    }
}
