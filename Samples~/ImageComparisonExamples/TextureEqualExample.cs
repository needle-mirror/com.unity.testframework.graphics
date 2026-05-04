using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.LegacyColorDifference;

namespace GraphicsTestFrameworkProject.Samples.ImageComparisonExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates the Assert.That / IsTexture.EqualTo constraint API
    for custom texture comparison.

    Instead of ImageAssert.AreEqual (which uses the legacy DeltaE algorithm),
    you can use NUnit's constraint syntax with pluggable comparison algorithms:

      Assert.That(actual, IsTexture.EqualTo(expected).Using(algorithm));

    Available algorithms:
      - StructuralSimilarity (SSIM): perceptual quality metric, 0..1 where 1 = identical
      - PeakSignalToNoiseRatio (PSNR): signal fidelity in dB, higher is better (~30 dB human threshold)
      - LegacyColorDifferenceAlgorithm: the original per-pixel DeltaE algorithm from ImageAssert

    The constraint API integrates with NUnit's assertion messages, showing
    algorithm-specific descriptions and results on failure.
    */

    [Category("Samples")]
    [TestOf(nameof(TextureEqualExample))]
    internal class TextureEqualExample
    {
        Texture2D expected;
        Texture2D actual;

        [SetUp]
        public void SetUp()
        {
            expected = CreateSolidColorTexture(64, 64, Color.blue);
            actual = CreateSolidColorTexture(64, 64, Color.blue);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(expected);
            Object.DestroyImmediate(actual);
        }

        static Texture2D CreateSolidColorTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /*
        -- Tutorial [2] --
        Structural Similarity Index Measure (SSIM).
        SSIM compares luminance, contrast, and structure across sliding windows.
        MinimumIndexMeasure sets the pass/fail threshold (0..1, where 1 = identical).
        A value of 0.95 is a good default for most graphics tests.
        */

        [Test]
        [Description("Compares textures using SSIM with a 0.95 similarity threshold.")]
        public void Ssim_IdenticalTextures_PassesThreshold()
        {
            var ssimSettings = new StructuralSimilaritySettings(minimumIndexMeasure: 0.95f);
            var ssim = new StructuralSimilarity(ssimSettings);

            Assert.That(actual, IsTexture.EqualTo(expected).Using(ssim));
        }

        /*
        -- Tutorial [3] --
        SSIM with custom window and Gaussian parameters.
        Smaller windows are more sensitive to local differences;
        larger windows smooth over small artifacts.
        */

        [Test]
        [Description("SSIM with custom window size and Gaussian weight.")]
        public void Ssim_CustomWindowSettings()
        {
            var ssimSettings = new StructuralSimilaritySettings(minimumIndexMeasure: 0.90f)
            {
                WindowSize = 7,
                GaussianWeight = 1.0f,
            };

            var ssim = new StructuralSimilarity(ssimSettings);
            Assert.That(actual, IsTexture.EqualTo(expected).Using(ssim));
        }

        /*
        -- Tutorial [4] --
        Peak Signal-to-Noise Ratio (PSNR).
        PSNR measures fidelity in decibels. Common thresholds:
          - < 30 dB: differences likely visible
          - 30-40 dB: good quality
          - > 40 dB: excellent / near-identical
        */

        [Test]
        [Description("Compares textures using PSNR with a 30 dB threshold.")]
        public void Psnr_IdenticalTextures_ExceedsThreshold()
        {
            var psnrSettings = new PeakSignalToNoiseRatioSettings(value: 30f);
            var psnr = new PeakSignalToNoiseRatio(psnrSettings);

            Assert.That(actual, IsTexture.EqualTo(expected).Using(psnr));
        }

        /*
        -- Tutorial [5] --
        Combining multiple algorithms for comprehensive comparison.
        Run SSIM and PSNR on the same texture pair to get complementary perspectives:
        SSIM captures perceptual/structural quality while PSNR captures raw signal fidelity.
        */

        [Test]
        [Description("Runs both SSIM and PSNR assertions on the same texture pair.")]
        public void CombinedMetrics_BothPass()
        {
            var ssim = new StructuralSimilarity(
                new StructuralSimilaritySettings(minimumIndexMeasure: 0.95f)
            );
            var psnr = new PeakSignalToNoiseRatio(
                new PeakSignalToNoiseRatioSettings(value: 30f)
            );

            Assert.That(actual, IsTexture.EqualTo(expected).Using(ssim));
            Assert.That(actual, IsTexture.EqualTo(expected).Using(psnr));
        }

        /*
        -- Tutorial [6] --
        Using the legacy algorithm through the constraint API.
        This gives the same result as ImageAssert.AreEqual but with NUnit constraint syntax,
        which provides richer assertion failure messages and can be composed with other constraints.
        */

        [Test]
        [Description("Uses the legacy DeltaE algorithm via the constraint API.")]
        public void LegacyAlgorithm_ViaConstraintApi()
        {
            var settings = new ImageComparisonSettings
            {
                AverageCorrectnessThreshold = 0.005f,
                PerPixelCorrectnessThreshold = 0.01f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE,
                ActivePixelTests = ImageComparisonSettings.PixelTests.DeltaE,
            };

            var algorithm = new LegacyColorDifferenceAlgorithm(settings);
            Assert.That(actual, IsTexture.EqualTo(expected).Using(algorithm));
        }
    }
}
