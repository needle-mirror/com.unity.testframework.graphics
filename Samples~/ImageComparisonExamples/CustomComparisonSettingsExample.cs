using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.ImageComparisonExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to configure ImageComparisonSettings to control
    the strictness and behavior of image comparisons.

    ImageComparisonSettings has two layers of configuration:
      1. Image-level tests (ActiveImageTests): control which aggregate metrics are checked
      2. Pixel-level tests (ActivePixelTests): control how individual pixels are evaluated
         for the IncorrectPixelsCount image test

    These examples use the [GraphicsTest] attribute with Texture2D-to-Texture2D comparison,
    which is the simplest approach for demonstrating settings configuration.
    */

    [Category("Samples")]
    [TestOf(nameof(CustomComparisonSettingsExample))]
    internal class CustomComparisonSettingsExample
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
        Strict comparison: all pixels must match exactly.
        Setting all thresholds to zero and enabling all tests enforces pixel-perfect matching.
        This is appropriate for deterministic rendering tests where no variance is acceptable.
        */

        [Test, GraphicsTest]
        [Description("Strict pixel-perfect comparison with zero tolerance.")]
        public void StrictComparison(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                PerPixelCorrectnessThreshold = 0f,
                PerPixelGammaThreshold = 0f,
                PerPixelAlphaThreshold = 0f,
                AverageCorrectnessThreshold = 0f,
                IncorrectPixelsThreshold = 0f,
                RMSEThreshold = 0f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE
                    | ImageComparisonSettings.ImageTests.IncorrectPixelsCount
                    | ImageComparisonSettings.ImageTests.RMSE,
                ActivePixelTests = ImageComparisonSettings.PixelTests.DeltaE
                    | ImageComparisonSettings.PixelTests.DeltaAlpha
                    | ImageComparisonSettings.PixelTests.DeltaGamma,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [3] --
        Relaxed comparison: allow minor per-pixel differences.
        This is typical for cross-platform tests where small numerical differences are expected
        due to different GPU hardware, driver versions, or floating-point precision.

        IncorrectPixelsThreshold is a ratio (0.0 to 1.0) of how many pixels can exceed
        the per-pixel thresholds. A value of 0.005 allows 0.5% of pixels to differ.
        */

        [Test, GraphicsTest]
        [Description("Relaxed comparison allowing minor per-pixel differences.")]
        public void RelaxedComparison(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                PerPixelCorrectnessThreshold = 0.02f,
                PerPixelGammaThreshold = 5f / 255,
                PerPixelAlphaThreshold = 5f / 255,
                AverageCorrectnessThreshold = 0.01f,
                IncorrectPixelsThreshold = 0.005f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE
                    | ImageComparisonSettings.ImageTests.IncorrectPixelsCount,
                ActivePixelTests = ImageComparisonSettings.PixelTests.DeltaE
                    | ImageComparisonSettings.PixelTests.DeltaGamma,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [4] --
        RMSE-only comparison: a single aggregate metric for overall image similarity.
        This approach ignores individual pixel differences and focuses on the root mean
        squared error across the entire image. Useful for effects with inherent noise
        (e.g., stochastic sampling, temporal anti-aliasing).
        */

        [Test, GraphicsTest]
        [Description("RMSE-only comparison for noisy rendering effects.")]
        public void RmseOnlyComparison(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                RMSEThreshold = 0.05f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.RMSE,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [5] --
        Custom resolution and MSAA settings.
        TargetWidth/TargetHeight control the render texture resolution used when
        comparing against a Camera (via the camera-based AreEqual overload).
        TargetMSAASamples sets the multi-sample anti-aliasing level.
        UseHDR enables HDR rendering for the capture render texture.

        When comparing two Texture2D objects directly, the resolution is determined
        by the textures themselves, but these fields still serve as metadata for
        the framework's reference image management.
        */

        [Test, GraphicsTest]
        [Description("High-resolution 4x MSAA comparison settings.")]
        public void HighResolutionMsaaComparison(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 1920,
                TargetHeight = 1080,
                TargetMSAASamples = 4,
                AverageCorrectnessThreshold = 0.005f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [6] --
        Back buffer comparison: captures the final composited frame buffer
        instead of rendering to a separate render texture.
        UseBackBuffer = true tells the camera-based AreEqual overload to capture
        the screen output rather than rendering to an offscreen render texture.
        This is useful when testing post-processing, UI overlays, or any effect
        that depends on the final screen output.

        Note: UseBackBuffer only applies when using the camera-based AreEqual overload.
        When comparing two Texture2D objects directly (as shown here for illustration),
        this flag has no effect on the comparison itself.
        */

        [Test, GraphicsTest]
        [Description("Back buffer comparison settings for final screen output.")]
        public void BackBufferComparison(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                UseBackBuffer = true,
                AverageCorrectnessThreshold = 0.01f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [7] --
        Retrieving comparison results programmatically.
        The overload with an out ImageComparisonResults parameter lets you inspect
        the comparison metrics (AverageDeltaE, BadPixelsCount) after the assertion
        for logging, reporting, or conditional logic.
        */

        [Test, GraphicsTest]
        [Description("Retrieves comparison results for programmatic inspection.")]
        public void ComparisonWithResults(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                AverageCorrectnessThreshold = 0.01f,
                IncorrectPixelsThreshold = 0.005f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE
                    | ImageComparisonSettings.ImageTests.IncorrectPixelsCount,
            };

            ImageAssert.AreEqual(
                testCase.ReferenceImage.Image,
                actualImage,
                settings,
                testCase.ReferenceImage.LoadMessage,
                saveFailedImage: true,
                saveFailedImageToDisk: false,
                logMessages: true,
                out var results
            );

            Debug.Log($"Average DeltaE: {results.AverageDeltaE}, Bad Pixels: {results.BadPixelsCount}");
        }
    }
}
