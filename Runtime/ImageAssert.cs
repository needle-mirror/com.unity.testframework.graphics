using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using Unity.Jobs;
using Unity.TestProtocol;
using Unity.TestProtocol.Messages;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
#endif
using System.Text;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provides test assertion helpers for working with images.
    /// </summary>
    public class ImageAssert
    {
        static Dictionary<ImageComparisonSettings.Resolution, (int, int)> backBufferResolutions = new Dictionary<ImageComparisonSettings.Resolution, (int, int)>
        {
            {ImageComparisonSettings.Resolution.w1920h1080, (1920, 1080)},
            {ImageComparisonSettings.Resolution.w1600h900, (1600, 900)},
            {ImageComparisonSettings.Resolution.w1280h720, (1280, 720)},
            {ImageComparisonSettings.Resolution.w960h540, (960, 540)},
            {ImageComparisonSettings.Resolution.w640h360, (640, 360)}
        };

        const int k_BatchSize = 1024;

        // The back buffer resolution is set to the highest available, and then comparisons are scaled down if necessary
        public const int kBackBufferHeight = 1080;
        public const int kBackBufferWidth = 1920;

        static Recorder gcAllocRecorder;

        public static Action<RenderTexture> onAllCamerasRendered;

        /// <summary>
        /// Render an image from the given camera and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image that should be rendered by the camera.</param>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        public static void AreEqual(Texture2D expected, Camera camera, ImageComparisonSettings settings = null)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            AreEqual(expected, new List<Camera> { camera }, settings);
        }

        /// <summary>
        /// Render an image from the given cameras and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image that should be rendered by the camera.</param>
        /// <param name="cameras">The cameras to render from.</param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        public static void AreEqual(Texture2D expected, IEnumerable<Camera> cameras, ImageComparisonSettings settings = null)
        {
            if (cameras == null)
                throw new ArgumentNullException(nameof(cameras));

            if (settings == null)
                settings = new ImageComparisonSettings();

            int width = settings.TargetWidth;
            int height = settings.TargetHeight;
            int samples = settings.TargetMSAASamples;
            var format = expected != null ? expected.format : TextureFormat.ARGB32;

            // Some HDRP test fail with HDRP batcher because shaders variant are compiled "on the fly" in editor mode.
            // Persistent PerMaterial CBUFFER is build during culling, but some nodes could use new variants and CBUFFER will be up to date next frame.
            // ( this is editor specific, standalone player has no frame delay issue because all variants are ready at init stage )
            // This PR adds a dummy rendered frame before doing the real rendering and compare images ( test already has frame delay, but there is no rendering )
            int dummyRenderedFrameCount = 1;

            var defaultFormat = (settings.UseHDR) ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR) : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, defaultFormat, 24);
            desc.msaaSamples = samples;
            var rt = RenderTexture.GetTemporary(desc);
            UnityEngine.Graphics.SetRenderTarget(rt);
            UnityEngine.GL.Clear(true, true, UnityEngine.Color.black);
            UnityEngine.Graphics.SetRenderTarget(null);


            Texture2D actual = null;
            try
            {
                if (RuntimeSettings.reuseTestsForXR)
                {
                    GetImageResolution(settings, out int w, out int h);
                    actual = new Texture2D(w, h, format, false);
                    actual.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                    actual.Apply();
                }
                else if (settings.UseBackBuffer)
                {
                    actual = BackBufferCapture(expected, cameras, settings);
                    actual.Apply();
                }
                else
                {
                    for (int i = 0; i < dummyRenderedFrameCount + 1; i++)        // x frame delay + the last one is the one really tested ( ie 5 frames delay means 6 frames are rendered )
                    {
                        foreach (var camera in cameras)
                        {
                            if (camera == null)
                                continue;
                            camera.targetTexture = rt;
                            camera.Render();
                            camera.targetTexture = null;
                        }

                        if (onAllCamerasRendered != null)
                            onAllCamerasRendered(rt);

                        // only proceed the test on the last rendered frame
                        if (dummyRenderedFrameCount == i)
                        {

                            actual = new Texture2D(width, height, format, false);
                            RenderTexture dummy = null;

                            if (settings.UseHDR)
                            {
                                desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                                dummy = RenderTexture.GetTemporary(desc);
                                UnityEngine.Graphics.Blit(rt, dummy);
                            }
                            else
                                RenderTexture.active = rt;

                            actual.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                            RenderTexture.active = null;

                            if (dummy != null)
                                RenderTexture.ReleaseTemporary(dummy);

                            actual.Apply();
                        }
					}
                }
                AreEqual(expected, actual, settings);

            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                if (actual != null)
                {
#if UNITY_EDITOR
                    UnityEngine.Object.DestroyImmediate(actual);
#else
                    UnityEngine.Object.Destroy(actual);
#endif
                }
            }
        }
        
        private static string StripParametricTestCharacters(string name)
        {
            {
                string illegal = "\"";
                int found = name.IndexOf(illegal);
                while (found >= 0)
                {
                    name = name.Remove(found, 1);
                    found = name.IndexOf(illegal);
                }
            }
            {
                string illegal = ",";
                name = name.Replace(illegal, "-");
            }
            {
                string illegal = "(";
                name = name.Replace(illegal, "_");
            }
            {
                string illegal = ")";
                name = name.Replace(illegal, "_");
            }
            return name;
        }

        /// <summary>
        /// Compares an image to a 'reference' image to see if it looks correct. Assumes linear HDR images (RGBAFloat or RGBAHalf).
        /// </summary>
        /// <param name="expected">What the image is supposed to look like.</param>
        /// <param name="actual">What the image actually looks like.</param>
        /// <param name="settings">Optional settings that control how the comparison is performed. Can be null, in which case the images are required to be exactly identical.</param>
        public static void AreEqualLinearHDR(Texture2D expected, Texture2D actual, ImageComparisonSettings settings = null, bool saveFailedImage = true)
        {
            if (actual == null)
                throw new ArgumentNullException(nameof(actual));

            bool hdrFormat = (actual.format == TextureFormat.RGBAHalf || actual.format == TextureFormat.RGBAFloat);
                Assert.IsTrue(hdrFormat, $"Input image {nameof(actual)} is using an invalid format. Expected format should be RGBAHalf or RGBAFloat.");

            var actualImagePath = "";
            if (saveFailedImage == true)
            {
                actualImagePath = CodeBasedGraphicsTestAttribute.TryFindAttributeOn(TestContext.CurrentTestExecutionContext.CurrentTest, out var attrib)
                ? attrib.ActualImagesRoot : "Assets/ActualImages";
            }
            else
            {
                actualImagePath = "Assets/ActualImages";
            };

            var dirName = Path.Combine(actualImagePath, TestUtils.GetCurrentTestResultsFolderPath());

            var imageName = TestContext.CurrentContext.Test.MethodName != null ? TestContext.CurrentContext.Test.Name : "NoName";
            var imageMessage = new ImageMessage
            {
                PathName = dirName,
                ImageName = StripParametricTestCharacters(imageName),
            };
            imageMessage.ActualImage = actual.EncodeToEXR();

            try
            {
                Assert.That(expected, Is.Not.Null, "No reference image was provided. Path: " + dirName);

                Assert.That(actual.width, Is.EqualTo(expected.width),
                    "The expected image had width {0}px, but the actual image had width {1}px.", expected.width,
                    actual.width);
                Assert.That(actual.height, Is.EqualTo(expected.height),
                    "The expected image had height {0}px, but the actual image had height {1}px.", expected.height,
                    actual.height);

                Assert.That(actual.format, Is.EqualTo(expected.format),
                    "The expected image had format {0} but the actual image had format {1}.", expected.format,
                    actual.format);

                using (var expectedPixels = new NativeArray<Color>(expected.GetPixels(0), Allocator.TempJob))
                using (var actualPixels = new NativeArray<Color>(actual.GetPixels(0), Allocator.TempJob))
                using (var diffPixels = new NativeArray<Color>(expectedPixels.Length, Allocator.TempJob))
                using (var batchMSEArray = new NativeArray<float>(Mathf.CeilToInt(expectedPixels.Length / (float)k_BatchSize), Allocator.TempJob))
                using (var batchMaxDeltaArray = new NativeArray<float>(batchMSEArray.Length, Allocator.TempJob))
                using (var batchBadPixelsArray = new NativeArray<int>(batchMSEArray.Length, Allocator.TempJob))
                {
                    if (settings == null)
                        settings = new ImageComparisonSettings();

                    // Extract flags
                    bool testBadPixelsCount = settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.IncorrectPixelsCount);
                    bool testRMSE = settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.RMSE);

                    new ComputeLinearHDRImageDiffJob
                    {
                        expected = expectedPixels,
                        actual = actualPixels,
                        diff = diffPixels,
                        pixelThreshold = settings.PerPixelCorrectnessThreshold,
                        batchMSE = batchMSEArray,
                        batchMaxDelta = batchMaxDeltaArray,
                        batchBadPixels = batchBadPixelsArray
                    }.Schedule(expectedPixels.Length, k_BatchSize).Complete();

                    int pixelCount = expected.width * expected.height;
                    float mse = batchMSEArray.Sum() / (pixelCount * 4);
                    float rmse = Mathf.Sqrt(mse);
                    float badPixelsMean = (batchBadPixelsArray.Sum() - 0.1f) / pixelCount;
                    float maxPixelDelta = batchMaxDeltaArray.Max();

                    try
                    {
                        if (testRMSE)
                            Assert.That(rmse, Is.LessThanOrEqualTo(settings.RMSEThreshold), "Failed RMSE threshold test.");
                        if (testBadPixelsCount)
                            Assert.That(badPixelsMean, Is.LessThanOrEqualTo(settings.IncorrectPixelsThreshold), "Failed per pixel threshold test.");
                    }
                    catch (AssertionException)
                    {
                        var diffImage = new Texture2D(expected.width, expected.height, TextureFormat.RGBAHalf, false);
                        var diffPixelsArray = new Color[expected.width * expected.height];
                        diffPixels.CopyTo(diffPixelsArray);
                        diffImage.SetPixels(diffPixelsArray, 0);
                        diffImage.Apply(false);

                        imageMessage.DiffImage = diffImage.EncodeToEXR();
                        imageMessage.ExpectedImage = expected.EncodeToEXR();
                        throw;
                    }
                }
                if (RuntimeSettings.saveActualImages)
                {
#if UNITY_EDITOR
                    ImageHandler.TextureImporterSettings importSettings = new ImageHandler.TextureImporterSettings(); 
                    ImageHandler.instance.SaveImage(imageMessage, true, importSettings);
#else
                    PlayerConnection.instance.Send(ImageMessage.MessageId, imageMessage.Serialize());
#endif
                }
            }
            catch (AssertionException)
            {
#if UNITY_EDITOR
                if (saveFailedImage)
                {
                    ImageHandler.TextureImporterSettings importSettings = new ImageHandler.TextureImporterSettings(); 
                    ImageHandler.instance.SaveImage(imageMessage, true, importSettings);
                }
#else
                PlayerConnection.instance.Send(ImageMessage.MessageId, imageMessage.Serialize());
#endif
                throw;
            }
        }

        /// <summary>
        /// Compares an image to a 'reference' image to see if it looks correct.
        /// </summary>
        /// <param name="expected">What the image is supposed to look like.</param>
        /// <param name="actual">What the image actually looks like.</param>
        /// <param name="settings">Optional settings that control how the comparison is performed. Can be null, in which case the images are required to be exactly identical.</param>
        public static void AreEqual(Texture2D expected, Texture2D actual, ImageComparisonSettings settings = null, bool saveFailedImage = true)
        {
            if (actual == null)
                throw new ArgumentNullException(nameof(actual));
            
            var actualImagePath = "";

            if (saveFailedImage == true)
            {
                actualImagePath = CodeBasedGraphicsTestAttribute.TryFindAttributeOn(TestContext.CurrentTestExecutionContext.CurrentTest, out var attrib)
                ? attrib.ActualImagesRoot : "Assets/ActualImages";
            } else {
                actualImagePath = "Assets/ActualImages";
            };
            
            var dirName = Path.Combine(actualImagePath, TestUtils.GetCurrentTestResultsFolderPath());

            var imageName = TestContext.CurrentContext.Test.MethodName != null ? TestContext.CurrentContext.Test.Name : "NoName";
            var imageMessage = new ImageMessage
            {
                PathName = dirName,
                ImageName = StripParametricTestCharacters(imageName),
            };
            imageMessage.ActualImage = actual.EncodeToPNG();

            try
            {
                Assert.That(expected, Is.Not.Null, "No reference image was provided. Path: " + dirName);

                Assert.That(actual.width, Is.EqualTo(expected.width),
                    "The expected image had width {0}px, but the actual image had width {1}px.", expected.width,
                    actual.width);
                Assert.That(actual.height, Is.EqualTo(expected.height),
                    "The expected image had height {0}px, but the actual image had height {1}px.", expected.height,
                    actual.height);

                Assert.That(actual.format, Is.EqualTo(expected.format),
                    "The expected image had format {0} but the actual image had format {1}.", expected.format,
                    actual.format);

                using (var expectedPixels = new NativeArray<Color32>(expected.GetPixels32(0), Allocator.TempJob))
                using (var actualPixels = new NativeArray<Color32>(actual.GetPixels32(0), Allocator.TempJob))
                using (var diffPixels = new NativeArray<Color32>(expectedPixels.Length, Allocator.TempJob))
                using (var sumOverThreshold = new NativeArray<float>(Mathf.CeilToInt(expectedPixels.Length / (float)k_BatchSize), Allocator.TempJob))
                using (var badPixels = new NativeArray<int>(sumOverThreshold.Length, Allocator.TempJob))
                {
                    if (settings == null)
                        settings = new ImageComparisonSettings();

                    // Extract flags
                    bool testAverageDeltaE = settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.AverageDeltaE);
                    bool testBadPixelsCount = settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.IncorrectPixelsCount);
                    bool countBadDeltaE = testBadPixelsCount && settings.ActivePixelTests.HasFlag(ImageComparisonSettings.PixelTests.DeltaE);
                    bool countBadGamma = testBadPixelsCount && settings.ActivePixelTests.HasFlag(ImageComparisonSettings.PixelTests.DeltaGamma);
                    bool countBadAlpha = testBadPixelsCount && settings.ActivePixelTests.HasFlag(ImageComparisonSettings.PixelTests.DeltaAlpha);

                    new ComputeDiffJob
                    {
                        expected = expectedPixels,
                        actual = actualPixels,
                        diff = diffPixels,
                        sumOverThreshold = sumOverThreshold,
                        badPixels = badPixels,
                        deltaEThreshold = settings.PerPixelCorrectnessThreshold,
                        gammaThreshold = settings.PerPixelGammaThreshold,
                        alphaThreshold = settings.PerPixelAlphaThreshold,
                        addDeltaE = testAverageDeltaE,
                        countBadDeltaE = countBadDeltaE,
                        countBadGamma = countBadGamma,
                        countBadAlpha = countBadAlpha
                    }.Schedule(expectedPixels.Length, k_BatchSize).Complete();

                    int pixelCount = expected.width * expected.height;
                    float averageDeltaE = sumOverThreshold.Sum() / pixelCount;
                    float badPixelsCount = (badPixels.Sum() - 0.1f) / pixelCount;

                    try
                    {
                        if (testAverageDeltaE)
                            Assert.That(averageDeltaE, Is.LessThanOrEqualTo(settings.AverageCorrectnessThreshold));
                        if (testBadPixelsCount)
                            Assert.That(badPixelsCount, Is.LessThanOrEqualTo(settings.IncorrectPixelsThreshold));
                    }
                    catch (AssertionException)
                    {
                        var diffImage = new Texture2D(expected.width, expected.height, TextureFormat.RGB24, false);
                        var diffPixelsArray = new Color32[expected.width * expected.height];
                        diffPixels.CopyTo(diffPixelsArray);
                        diffImage.SetPixels32(diffPixelsArray, 0);
                        diffImage.Apply(false);

                        imageMessage.DiffImage = diffImage.EncodeToPNG();
                        imageMessage.ExpectedImage = expected.EncodeToPNG();
                        throw;
                    }
                }
                if (RuntimeSettings.saveActualImages)
                {
#if UNITY_EDITOR
                    ImageHandler.instance.SaveImage(imageMessage);
#else
                    PlayerConnection.instance.Send(ImageMessage.MessageId, imageMessage.Serialize());
#endif
                }
            }
            catch (AssertionException)
            {
#if UNITY_EDITOR
                if (saveFailedImage)
                    ImageHandler.instance.SaveImage(imageMessage);
#else
                PlayerConnection.instance.Send(ImageMessage.MessageId, imageMessage.Serialize());
#endif
                throw;
            }
        }

        /// <summary>
        /// Render an image from the given camera and check if it allocated memory while doing so.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="width"> width of the image to be rendered</param>
        /// <param name="height"> height of the image to be rendered</param>
        public static void AllocatesMemory(Camera camera, ImageComparisonSettings settings = null, int gcAllocThreshold = 0)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (settings == null)
                settings = new ImageComparisonSettings();

            int width = settings.TargetWidth;
            int height = settings.TargetHeight;

            var defaultFormat = (settings.UseHDR) ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR) : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, defaultFormat, 24);

            if (gcAllocRecorder == null)
            {
                gcAllocRecorder = Recorder.Get("GC.Alloc");
                gcAllocRecorder.FilterToCurrentThread();
                gcAllocRecorder.enabled = false;
            }

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

                int allocationCountOfRenderPipeline = gcAllocRecorder.sampleBlockCount - gcAllocThreshold;

                if (allocationCountOfRenderPipeline > 0)
                    throw new Exception(
                        $@"Memory allocation test failed, {allocationCountOfRenderPipeline} allocations detected. Steps to find where your allocation is:
                        - Open the profiler window (ctrl-7) and enable deep profiling.
                        - Run your the test that fails and wait (it can take much longer because deep profiling is enabled).
                        - In the CPU section of the profiler, select on Hierarchy and search for the 'GraphicTests_GC_Alloc_Check' marker.
                        - This should give you one result, click on it and press f to go to the frame where it hapended.
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
        /// <param name="overrideSRPMarkerName">Override the main marker used to check the GC.Alloc</param>
        public static IEnumerator CheckGCAllocWithCallstack(Camera camera, ImageComparisonSettings settings = null, string overrideSRPMarkerName = null)
        {
#if UNITY_2020_2_OR_NEWER && UNITY_EDITOR
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (settings == null)
                settings = new ImageComparisonSettings();

            int width = settings.TargetWidth;
            int height = settings.TargetHeight;

            var defaultFormat = (settings.UseHDR) ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR) : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, defaultFormat, 24);

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

                int cameraRenderFrameIndex = ProfilerDriver.GetPreviousFrameIndex(Time.frameCount);
                long totalGcAllocSize = 0;

                const int mainThread = 0;
                var humanReadableCallstack = new StringBuilder();
                using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(cameraRenderFrameIndex, mainThread))
                {
                    if (!frameData.valid)
                        yield break;

                    int gcAllocMarkerId = frameData.GetMarkerId("GC.Alloc");
                    
                    // Check if there is a GC Alloc marker in the frame
                    if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                        yield break;
                    
                    // Check if there is the srp marker in the frame
                    var srpMarker = frameData.GetMarkerId(overrideSRPMarkerName ?? "UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke]");
                    if (srpMarker == FrameDataView.invalidMarkerId)
                        throw new Exception("SRP Marker not found in profiling while searching for GC.Alloc");
                    int sampleCount = frameData.sampleCount;
                    for (int i = 0; i < sampleCount; ++i)
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
                                
                                long gcAllocSize = frameData.GetSampleMetadataAsLong(i, 0);
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

                camera.targetTexture = null;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
            yield break;
#else
            AllocatesMemory(camera, settings, 0);
            yield break;
#endif
        }

        static Texture2D BackBufferCapture(Texture2D expected, IEnumerable<Camera> cameras, ImageComparisonSettings settings = null)
        {
            (int,int) resolution = (0,0);
            backBufferResolutions.TryGetValue(settings.ImageResolution, out resolution);

            var format = expected != null ? expected.format : TextureFormat.ARGB32;

            Vector2 screenRes = new Vector2(Screen.width, Screen.height); // Grab the resolution of the screen
            Texture2D actual = new Texture2D((int)screenRes.x, (int)screenRes.y, format, false); // new texture to fill sized to the screen
            actual.ReadPixels(new Rect(0, 0, (int)screenRes.x, (int)screenRes.y), 0, 0, false); // grab screen pixels
            Texture2D resizeActual = new Texture2D(resolution.Item1, resolution.Item2, actual.format, false);
            resizeActual = ResizeInto(actual, resizeActual);
            UnityEngine.Object.Destroy(actual);
            return resizeActual;
        }

        public static void GetImageResolution(ImageComparisonSettings settings, out int width, out int height)
        {
            if (settings.UseBackBuffer && backBufferResolutions.TryGetValue(settings.ImageResolution, out var resolution))
            {
                width = resolution.Item1;
                height = resolution.Item2;
            }
            else
            {
                width = settings.TargetWidth;
                height = settings.TargetHeight;
            }
        }

        struct ComputeDiffJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> expected;
            [ReadOnly] public NativeArray<Color32> actual;
            public NativeArray<Color32> diff;

            public float deltaEThreshold;
            public float gammaThreshold;
            public float alphaThreshold;

            public bool addDeltaE;

            public bool countBadDeltaE;
            public bool countBadGamma;
            public bool countBadAlpha;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> sumOverThreshold;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> badPixels;

            public void Execute(int index)
            {
                Color32 exp = expected[index];
                Color32 act = actual[index];
                int batch = index / k_BatchSize;

                float deltaE = 0;
                float deltaGamma = 0;
                float deltaAlpha = 0;
                bool pixelIsCorrect = true;

                if (addDeltaE || countBadDeltaE)
                {
                    deltaE = JABDeltaE(RGBtoJAB(exp), RGBtoJAB(act));
                    float deltaEOverThreshold = Mathf.Max(0f, deltaE - deltaEThreshold);
                    sumOverThreshold[batch] = sumOverThreshold[batch] + deltaEOverThreshold;
                    if (countBadDeltaE)
                        pixelIsCorrect &= deltaEOverThreshold <= 0;
                }

                if (countBadGamma)
                {
                    float deltaR = Mathf.Abs(exp.r - act.r);
                    float deltaG = Mathf.Abs(exp.g - act.g);
                    float deltaB = Mathf.Abs(exp.b - act.b);

                    deltaGamma = Mathf.Max(Mathf.Max(deltaR, deltaG), deltaB) / 255f;
                    pixelIsCorrect &= deltaGamma <= gammaThreshold;
                }

                if (countBadAlpha)
                {
                    deltaAlpha = Mathf.Abs(exp.a - act.a) / 255f;
                    pixelIsCorrect &= deltaAlpha <= alphaThreshold;
                }

                badPixels[batch] += pixelIsCorrect ? 0 : 1;

                // deltaE is linear, convert it to sRGB for easier debugging
                deltaE = Mathf.LinearToGammaSpace(deltaE);
                float result = Mathf.Max(Mathf.Max(deltaE, deltaAlpha), deltaGamma);
                var colorResult = new Color(result, result, result, 1f);
                diff[index] = colorResult;
            }
        }
        struct ComputeLinearHDRImageDiffJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color> expected;
            [ReadOnly] public NativeArray<Color> actual;
            public NativeArray<Color> diff;
            public float pixelThreshold;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> batchMSE;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> batchMaxDelta;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> batchBadPixels;

            public void Execute(int index)
            {
                Color exp = expected[index];
                Color act = actual[index];
                int batch = index / k_BatchSize;

                // compute pixel difference
                float deltaR = Mathf.Abs(exp.r - act.r);
                float deltaG = Mathf.Abs(exp.g - act.g);
                float deltaB = Mathf.Abs(exp.b - act.b);
                float deltaA = Mathf.Abs(exp.a - act.a);
                float maxDelta = Mathf.Max(Mathf.Max(Mathf.Max(deltaR, deltaG), deltaB), deltaA);
                if (maxDelta > pixelThreshold)
                    batchBadPixels[batch]++;
                batchMaxDelta[batch] = Mathf.Max(batchMaxDelta[batch], maxDelta);
                batchMSE[batch] += deltaR * deltaR;
                batchMSE[batch] += deltaG * deltaG;
                batchMSE[batch] += deltaB * deltaB;
                batchMSE[batch] += deltaA * deltaA;
                diff[index] = new Color(maxDelta, maxDelta, maxDelta, 1.0f);
            }
        } 

        // Linear RGB to XYZ using D65 ref. white
        static Vector3 RGBtoXYZ(Color color)
        {
            float x = color.r * 0.4124564f + color.g * 0.3575761f + color.b * 0.1804375f;
            float y = color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.0721750f;
            float z = color.r * 0.0193339f + color.g * 0.1191920f + color.b * 0.9503041f;
            return new Vector3(x * 100f, y * 100f, z * 100f);
        }

        // sRGB to JzAzBz
        // https://www.osapublishing.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272
        static Vector3 RGBtoJAB(Color color)
        {
            var xyz = RGBtoXYZ(color.linear);

            const float kB  = 1.15f;
            const float kG  = 0.66f;
            const float kC1 = 0.8359375f;        // 3424 / 2^12
            const float kC2 = 18.8515625f;       // 2413 / 2^7
            const float kC3 = 18.6875f;          // 2392 / 2^7
            const float kN  = 0.15930175781f;    // 2610 / 2^14
            const float kP  = 134.034375f;       // 1.7 * 2523 / 2^5
            const float kD  = -0.56f;
            const float kD0 = 1.6295499532821566E-11f;

            float x2 = kB * xyz.x - (kB - 1f) * xyz.z;
            float y2 = kG * xyz.y - (kG - 1f) * xyz.x;

            float l = 0.41478372f * x2 + 0.579999f * y2 + 0.0146480f * xyz.z;
            float m = -0.2015100f * x2 + 1.120649f * y2 + 0.0531008f * xyz.z;
            float s = -0.0166008f * x2 + 0.264800f * y2 + 0.6684799f * xyz.z;
            l = Mathf.Pow(l / 10000f, kN);
            m = Mathf.Pow(m / 10000f, kN);
            s = Mathf.Pow(s / 10000f, kN);

            // Can we switch to unity.mathematics yet?
            var lms = new Vector3(l, m, s);
            var a = new Vector3(kC1, kC1, kC1) + kC2 * lms;
            var b = Vector3.one + kC3 * lms;
            var tmp = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);

            lms.x = Mathf.Pow(tmp.x, kP);
            lms.y = Mathf.Pow(tmp.y, kP);
            lms.z = Mathf.Pow(tmp.z, kP);

            var jab = new Vector3(
                0.5f * lms.x + 0.5f * lms.y,
                3.524000f * lms.x + -4.066708f * lms.y + 0.542708f * lms.z,
                0.199076f * lms.x + 1.096799f * lms.y + -1.295875f * lms.z
            );

            jab.x = ((1f + kD) * jab.x) / (1f + kD * jab.x) - kD0;

            return jab;
        }

        static float JABDeltaE(Vector3 v1, Vector3 v2)
        {
            float c1 = Mathf.Sqrt(v1.y * v1.y + v1.z * v1.z);
            float c2 = Mathf.Sqrt(v2.y * v2.y + v2.z * v2.z);

            float h1 = Mathf.Atan(v1.z / v1.y);
            float h2 = Mathf.Atan(v2.z / v2.y);

            float deltaH = 2f * Mathf.Sqrt(c1 * c2) * Mathf.Sin((h1 - h2) / 2f);
            float deltaE = Mathf.Sqrt(Mathf.Pow(v1.x - v2.x, 2f) + Mathf.Pow(c1 - c2, 2f) + deltaH * deltaH);
            return deltaE;
        }

        /// <summary>
        /// Resize a source texture to match the dimensions of the destination texture
        /// </summary>
        public static Texture2D ResizeInto(Texture2D source, Texture2D dest)
        {
            Color[] destPix = new Color[dest.width * dest.height];
            int y = 0;
            while (y < dest.height)
            {
                int x = 0;
                while (x < dest.width)
                {
                    float xFrac = x * 1.0F / (dest.width);
                    float yFrac = y * 1.0F / (dest.height);
                    destPix[y * dest.width + x] = source.GetPixelBilinear(xFrac, yFrac);
                    x++;
                }
                y++;
            }
            var format = source != null ? source.format : TextureFormat.ARGB32;

            Texture2D newImage = new Texture2D(dest.width, dest.height, format, false);
            newImage.SetPixels(destPix);
            newImage.Apply();
            UnityEngine.Object.Destroy(dest);
            return newImage;
        }
    }
}

#if UNITY_EDITOR
public class ImageHandler : ScriptableSingleton<ImageHandler>
{
    public string ImageResultsPath;
    public void HandleImageEvent(MessageEventArgs messageEventArgs)
    {
        var imageMessage = ImageMessage.Deserialize(messageEventArgs.data);
        SaveImage(imageMessage);
    }

#if UNITY_EDITOR
    public class TextureImporterSettings
    {
        public bool IsReadable { get; set; } = true;
        public bool UseMipMaps { get; set; } = false;
        public TextureImporterNPOTScale NPOTScale { get; set; } = TextureImporterNPOTScale.None;
        public TextureImporterCompression TextureCompressionType { get; set; } = TextureImporterCompression.Uncompressed;
        public FilterMode TextureFilterMode { get; set; } = FilterMode.Point;
    }

    public static void ReImportTextureWithSettings(string path, TextureImporterSettings settings)
    {
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        if (importer == null)
            return;
        importer.isReadable = settings.IsReadable;
        importer.npotScale = settings.NPOTScale;
        importer.mipmapEnabled = settings.UseMipMaps;
        importer.textureCompression = settings.TextureCompressionType;
        importer.filterMode = settings.TextureFilterMode;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
#endif

    public void SaveImage(ImageMessage imageMessage, bool hdr = false, TextureImporterSettings textureImporterSettings = null)
    {
        var saveDir = string.IsNullOrEmpty(ImageResultsPath) ? imageMessage.PathName : ImageResultsPath;

        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }
        string extension = hdr ? "exr" : "png";
        var actualImagePath = Path.Combine(saveDir, $"{imageMessage.ImageName}.{extension}");
        File.WriteAllBytes(actualImagePath, imageMessage.ActualImage);
        ReportArtifact(actualImagePath);
        if (textureImporterSettings != null)
            ReImportTextureWithSettings(actualImagePath, textureImporterSettings);

        if (imageMessage.DiffImage != null)
        {
            var diffImagePath = Path.Combine(saveDir, $"{imageMessage.ImageName}.diff.{extension}");
            File.WriteAllBytes(diffImagePath, imageMessage.DiffImage);
            ReportArtifact(diffImagePath);
            if (textureImporterSettings != null)
                ReImportTextureWithSettings(diffImagePath, textureImporterSettings);

            var expectedImagesPath =
                Path.Combine(saveDir, $"{imageMessage.ImageName}.expected.{extension}");
            File.WriteAllBytes(expectedImagesPath, imageMessage.ExpectedImage);
            ReportArtifact(expectedImagesPath);
            if (textureImporterSettings != null)
                ReImportTextureWithSettings(expectedImagesPath, textureImporterSettings);
        }
    }

    public void PromoteImageToBaseImage(string imagePath)
    {

    }

    private void ReportArtifact(string artifactPath)
    {
        var fullpath = Path.GetFullPath(artifactPath);
        var message = ArtifactPublishMessage.Create(fullpath);
        Debug.Log(UnityTestProtocolMessageBuilder.Serialize(message));
    }
}
#endif
