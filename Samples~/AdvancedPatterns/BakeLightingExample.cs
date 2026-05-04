using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.AdvancedPatterns
{
    /*
    -- Tutorial [1] --
    This sample demonstrates the [BakeLighting] attribute, which bakes lightmaps
    for specified scenes as a pre-build step before graphics tests run.

    When Unity builds the test Player or enters Play mode for tests:
      1. Each scene listed in BakeLighting is opened in the Editor
      2. Lightmapping.Bake() is called and the scene is saved with baked data
      3. Tests then run against scenes with pre-baked lighting

    This ensures deterministic lighting results without requiring lightmap data
    to be checked into source control.
    */

    /*
    -- Tutorial [2] --
    BakeLighting is applied at the class level so all test methods benefit from
    the pre-baked data. The attribute accepts one or more scene paths.
    Scenes are baked in the order specified.
    */

    [BakeLighting(
        "Assets/Scenes/Lighting_Indoor.unity",
        "Assets/Scenes/Lighting_Outdoor.unity"
    )]
    [Category("Samples")]
    [TestOf(nameof(BakeLightingExample))]
    internal class BakeLightingExample
    {
        const string k_LightingSceneDirectory = "Assets/TestScenes";

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
        -- Tutorial [3] --
        After baking, tests run as normal. The scenes already contain baked lightmaps,
        light probes, and reflection probes from the pre-build step.

        Note: This sample creates a synthetic actual image for demonstration purposes.
        In production tests, you would typically capture from a Camera in the loaded scene.
        */

        [UnityTest, SceneGraphicsTest(k_LightingSceneDirectory)]
        [Description("Verifies baked lighting matches reference images.")]
        public IEnumerator BakedLighting_MatchesReference(SceneGraphicsTestCase testCase)
        {
            SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
            yield return null;

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }

    /*
    -- Tutorial [4] --
    BakeLighting also supports an 'order' parameter to control execution order
    when multiple pre-build steps are present. Lower numbers run first.
    This is useful when baking depends on other setup steps (e.g., asset import).
    */

    [BakeLighting(10, "Assets/Scenes/Lighting_Complex.unity")]
    [Category("Samples")]
    internal class OrderedBakeLightingExample
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

        [UnityTest, SceneGraphicsTest("Assets/Scenes")]
        [Description("Bakes lighting with explicit ordering (runs after order < 10).")]
        public IEnumerator OrderedBake_MatchesReference(SceneGraphicsTestCase testCase)
        {
            SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
            yield return null;

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }
}
