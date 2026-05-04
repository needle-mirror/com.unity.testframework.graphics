using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.ImageComparisonExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates advanced image comparison patterns:
      1. Multiple camera comparison: ImageAssert.AreEqual accepts IEnumerable<Camera>
         to render all cameras onto the same render texture (camera stacking/overlay)
      2. Async image comparison: AreEqualAsync uses AsyncGPUReadback to avoid GPU stalls

    Note: These APIs require active Camera objects in a loaded scene. The tutorials
    below describe the patterns; the test methods use a synthetic Texture2D for
    demonstration purposes. In production tests, you would load a scene and use cameras.

    Multiple camera example (in a real scene-based test):

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
            .OrderBy(c => c.depth);
        var settings = new ImageComparisonSettings { TargetWidth = 512, TargetHeight = 512 };
        ImageAssert.AreEqual(testCase.ReferenceImage.Image, cameras, settings);

    Async comparison example (in a real scene-based test):

        yield return ImageAssert.AreEqualAsync(
            testCase.ReferenceImage.Image,
            cameras,
            succeeded => Debug.Log($"Comparison passed: {succeeded}"),
            settings
        );
    */

    [Category("Samples")]
    [TestOf(nameof(MultipleCameraExample))]
    internal class MultipleCameraExample
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
        ImageComparisonSettings used for multi-camera and async comparisons.
        AverageCorrectnessThreshold controls how much average color deviation is tolerated
        across the entire image when using AverageDeltaE.
        */

        [Test, GraphicsTest]
        [Description("Demonstrates comparison settings used with multi-camera rendering.")]
        public void MultipleCameras_SettingsExample(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                AverageCorrectnessThreshold = 0.005f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [3] --
        The Texture2D-based AreEqual overload works identically for single and multi-camera
        scenarios. The difference is only in how the actual image is obtained.
        With multiple cameras, the framework renders each camera in depth order onto
        the same render texture before performing the comparison.
        */

        [Test, GraphicsTest]
        [Description("Demonstrates Texture2D comparison as used after multi-camera capture.")]
        public void AsyncComparison_SettingsExample(GraphicsTestCase testCase)
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = 512,
                TargetHeight = 512,
                AverageCorrectnessThreshold = 0.005f,
                ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }
    }
}
