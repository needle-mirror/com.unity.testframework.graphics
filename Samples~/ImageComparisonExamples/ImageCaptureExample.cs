using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.ImageComparisonExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates the ImageCapture utility class, which provides
    static helpers for working with captured Texture2D objects.

    ImageCapture offers these capabilities:
      - CaptureFromCamera: renders one or more cameras to a render texture
      - CaptureBackbuffer / CaptureBackBuffer: grabs the final screen output
      - BilinearResize: resizes a captured texture to a target resolution
      - SaveAsActual: saves a texture to the ActualImages output folder

    Note: CaptureFromCamera requires an active Camera in a loaded scene.
    These samples use a synthetic Texture2D for demonstration purposes.
    In production tests, you would use CaptureFromCamera with a real scene Camera.
    */

    [Category("Samples")]
    [TestOf(nameof(ImageCaptureExample))]
    internal class ImageCaptureExample
    {
        Texture2D actualImage;

        [SetUp]
        public void SetUp()
        {
            actualImage = new Texture2D(1, 1, TextureFormat.RGB24, false);
            actualImage.SetPixel(0, 0, Color.red);
            actualImage.Apply();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(actualImage);
        }

        /*
        -- Tutorial [2] --
        BilinearResize: resizes a texture to a target resolution using bilinear filtering.
        This is useful when the captured image resolution differs from the reference image
        resolution and you need to compare them at the same dimensions.
        */

        [Test, GraphicsTest]
        [Description("Resizes a texture using bilinear filtering.")]
        public void BilinearResize_ProducesTargetDimensions(GraphicsTestCase testCase)
        {
            var source = new Texture2D(64, 64, TextureFormat.ARGB32, false);
            try
            {
                var resized = ImageCapture.BilinearResize(
                    TextureFormat.ARGB32,
                    (32, 32),
                    source
                );

                Assert.That(resized.width, Is.EqualTo(32));
                Assert.That(resized.height, Is.EqualTo(32));
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        /*
        -- Tutorial [3] --
        SaveAsActual: saves a texture to the ActualImages output folder without
        performing any comparison. This is useful for diagnostic output, generating
        initial reference images, or saving intermediate results for debugging.
        */

        [Test, GraphicsTest]
        [Description("Saves a texture as an actual image for diagnostic output.")]
        public void SaveAsActual_ForDiagnostics(GraphicsTestCase testCase)
        {
            ImageCapture.SaveAsActual(actualImage, "DiagnosticCapture");
            Assert.Pass("Image saved to ActualImages folder.");
        }

        /*
        -- Tutorial [4] --
        CameraCaptureSettings: configures camera-based capture resolution, MSAA, and HDR mode.
        In a real scene-based test, you would pass these settings to CaptureFromCamera:

            var captureSettings = new CameraCaptureSettings
            {
                targetWidth = 1024,
                targetHeight = 768,
                useHDR = false,
                msaaSamples = 1,
            };

            Texture2D captured = null;
            foreach (var frame in ImageCapture.CaptureFromCamera(camera, TextureFormat.ARGB32, captureSettings))
                captured = frame;

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, captured);
        */

        [Test, GraphicsTest]
        [Description("Demonstrates CameraCaptureSettings configuration.")]
        public void CameraCaptureSettings_ConfigurationExample(GraphicsTestCase testCase)
        {
            var captureSettings = new CameraCaptureSettings
            {
                targetWidth = 1024,
                targetHeight = 768,
                useHDR = false,
                msaaSamples = 1,
            };

            Assert.That(captureSettings.targetWidth, Is.EqualTo(1024));
            Assert.That(captureSettings.targetHeight, Is.EqualTo(768));
            Assert.That(captureSettings.useHDR, Is.False);

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }
}
