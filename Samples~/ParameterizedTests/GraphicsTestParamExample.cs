using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.ParameterizedTests
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to use GraphicsTestParam to create parameterized graphics test variants.
    Each [GraphicsTestParam] attribute on a method creates one additional variant per graphics test case.
    The first parameter is always the GraphicsTestCase; subsequent parameters receive the supplied values.
    */

    [Category("Samples")]
    [TestOf(nameof(GraphicsTestParamExample))]
    internal class GraphicsTestParamExample
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
        Basic parameterized test with integer values.
        This generates three test variants, one for each quality level.
        The test name will automatically include the parameter value (e.g. "QualityLevel(1)", "QualityLevel(2)").
        */

        [Test, GraphicsTest]
        [GraphicsTestParam(0)]
        [GraphicsTestParam(1)]
        [GraphicsTestParam(2)]
        [Description("Verifies rendering at different quality levels.")]
        public void QualityLevel_RendersCorrectly(GraphicsTestCase testCase, int qualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityLevel);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [3] --
        Parameterized test with multiple arguments per variant.
        Each [GraphicsTestParam] supplies all extra parameters at once as an object array.
        Here we pass both a resolution multiplier and an MSAA sample count.
        */

        [Test, GraphicsTest]
        [GraphicsTestParam(1.0f, 1)]
        [GraphicsTestParam(0.5f, 2)]
        [GraphicsTestParam(2.0f, 4)]
        [Description("Verifies rendering at different resolution scales and MSAA levels.")]
        public void ResolutionAndMsaa_RendersCorrectly(
            GraphicsTestCase testCase,
            float resolutionScale,
            int msaaSamples
        )
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = (int)(512 * resolutionScale),
                TargetHeight = (int)(512 * resolutionScale),
                TargetMSAASamples = msaaSamples,
            };

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }

        /*
        -- Tutorial [4] --
        Named test parameters provide custom display names in test results.
        Set the TestName property to override the auto-generated name.
        The Ignore property can conditionally skip specific parameter combinations.
        */

        [Test, GraphicsTest]
        [GraphicsTestParam(false, TestName = "ShadowsOff")]
        [GraphicsTestParam(true, TestName = "ShadowsOn")]
        [Description("Verifies rendering with shadows toggled on and off.")]
        public void Shadows_Toggle_RendersCorrectly(GraphicsTestCase testCase, bool enableShadows)
        {
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [5] --
        Parameterized tests also work with SceneGraphicsTest for scene-based testing.
        Each scene discovered under the path is combined with each parameter set,
        producing (scenes x params) total test cases.

        Note: This sample creates a synthetic actual image for demonstration purposes.
        In production tests, you would typically capture from a Camera in the loaded scene.
        */

        const string k_SceneDirectory = "Assets/Scenes";

        [UnityTest, SceneGraphicsTest(k_SceneDirectory)]
        [GraphicsTestParam(1)]
        [GraphicsTestParam(4)]
        [Description("Verifies scene rendering at different MSAA sample counts.")]
        public IEnumerator Scene_WithMsaa_RendersCorrectly(
            SceneGraphicsTestCase testCase,
            int msaaSamples
        )
        {
            SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
            yield return null;

            var settings = new ImageComparisonSettings { TargetMSAASamples = msaaSamples };
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }
    }
}
