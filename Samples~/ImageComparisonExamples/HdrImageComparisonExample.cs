using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.ImageComparisonExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to test HDR (High Dynamic Range) rendering output
    using the Graphics Test Framework.

    HDR tests use:
      - TextureFormat.RGBAHalf or RGBAFloat for high-precision pixel data
      - ImageExtension.EXR for lossless HDR image storage
      - ImageAssert.AreEqualLinearHDR for linear-space HDR comparison

    Set these via the GraphicsTest attribute properties: TextureFormat and ImageExtension.
    */

    [Category("Samples")]
    [TestOf(nameof(HdrImageComparisonExample))]
    internal class HdrImageComparisonExample
    {
        Texture2D actualImage;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(actualImage);
        }

        /*
        -- Tutorial [2] --
        Basic HDR test using RGBAHalf format and EXR storage.
        The GraphicsTest attribute's TextureFormat and ImageExtension properties configure
        how reference images are stored and loaded. The test then uses AreEqualLinearHDR
        which performs comparison in linear HDR space rather than gamma LDR.
        */

        [Test]
        [GraphicsTest(TextureFormat = TextureFormat.RGBAHalf, ImageExtension = ImageExtension.EXR)]
        [Description("Verifies HDR rendering output stored as RGBAHalf EXR.")]
        public void HdrRendering_RGBAHalf_MatchesReference(GraphicsTestCase testCase)
        {
            actualImage = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            actualImage.SetPixel(0, 0, Color.red);
            actualImage.Apply();

            ImageAssert.AreEqualLinearHDR(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [3] --
        HDR test with RGBAFloat for maximum precision.
        RGBAFloat uses 32 bits per channel compared to RGBAHalf's 16 bits.
        Use this when you need full float precision, such as for light baking validation
        or scientific rendering.
        */

        [Test]
        [GraphicsTest(TextureFormat = TextureFormat.RGBAFloat, ImageExtension = ImageExtension.EXR)]
        [Description("Verifies HDR rendering with full-precision RGBAFloat format.")]
        public void HdrRendering_RGBAFloat_MatchesReference(GraphicsTestCase testCase)
        {
            actualImage = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            actualImage.SetPixel(0, 0, Color.red);
            actualImage.Apply();

            ImageAssert.AreEqualLinearHDR(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [4] --
        HDR test with custom comparison settings.
        Even with HDR tests, you can customize ImageComparisonSettings for thresholds.
        AreEqualLinearHDR uses RMSE and IncorrectPixelsCount metrics on raw linear values,
        so thresholds should be calibrated for the linear HDR range (not 0-255 gamma).
        */

        [Test]
        [GraphicsTest(TextureFormat = TextureFormat.RGBAHalf, ImageExtension = ImageExtension.EXR)]
        [Description("Verifies HDR rendering with custom RMSE threshold.")]
        public void HdrRendering_WithCustomThreshold_MatchesReference(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                PerPixelCorrectnessThreshold = 0.001f,
                IncorrectPixelsThreshold = 0.01f,
                RMSEThreshold = 0.01f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.RMSE
                    | ImageComparisonSettings.ImageTests.IncorrectPixelsCount,
            };

            actualImage = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            actualImage.SetPixel(0, 0, Color.red);
            actualImage.Apply();

            ImageAssert.AreEqualLinearHDR(
                testCase.ReferenceImage.Image,
                actualImage,
                settings,
                testCase.ReferenceImage.LoadMessage
            );
        }
    }
}
