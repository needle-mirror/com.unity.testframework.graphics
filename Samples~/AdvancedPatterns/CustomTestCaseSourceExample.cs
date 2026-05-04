using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.TestCases;

namespace GraphicsTestFrameworkProject.Samples.AdvancedPatterns
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to create a custom GraphicsTestCaseSource to control
    how test cases are generated. By subclassing SceneGraphicsTestCaseSource (or
    GraphicsTestCaseSource directly), you can:
      - Filter, modify, or augment the test cases produced by the default scene discovery
      - Add metadata or custom naming conventions
      - Multiply test cases by additional dimensions (e.g., SRP asset variants)

    The custom source is passed to SceneGraphicsTest via its first type argument:
      [SceneGraphicsTest(typeof(MySource), "Assets/Scenes")]
    */

    [Category("Samples")]
    [TestOf(nameof(CustomTestCaseSourceExample))]
    internal class CustomTestCaseSourceExample
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
        Using the custom source.
        PrefixedSceneTestCaseSource adds a "Sample_" prefix to all test case names,
        making them easy to identify and filter in test runners.

        Note: This sample creates a synthetic actual image for demonstration purposes.
        In production tests, you would typically capture from a Camera in the loaded scene.
        */

        [UnityTest, SceneGraphicsTest(typeof(PrefixedSceneTestCaseSource), "Assets/Scenes")]
        [Description("Uses a custom test case source that prefixes test names.")]
        public IEnumerator PrefixedTests_RendersCorrectly(SceneGraphicsTestCase testCase)
        {
            SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
            yield return null;

            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }

    /*
    -- Tutorial [3] --
    A custom SceneGraphicsTestCaseSource that modifies the name of each test case.
    Override GetTestCases to intercept the default behavior and transform the results.

    The `with` expression on records creates a shallow copy with modified properties,
    which is the idiomatic way to customize GraphicsTestCase instances.
    */

    internal class PrefixedSceneTestCaseSource : SceneGraphicsTestCaseSource
    {
        const string k_Prefix = "Sample";

        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite)
        {
            foreach (var testCase in base.GetTestCases(methodInfo, suite))
            {
                if (testCase is not SceneGraphicsTestCase sceneTestCase)
                    continue;

                yield return sceneTestCase with
                {
                    Name = $"{k_Prefix}_{testCase.Name}",
                    FullName = testCase.FullName.Replace(testCase.Name, $"{k_Prefix}_{testCase.Name}"),
                };
            }
        }
    }

    /*
    -- Tutorial [4] --
    A more advanced example: FilteredSceneTestCaseSource excludes scenes
    matching a pattern. This can be useful when a subset of scenes in a directory
    are not ready for automated testing.
    */

    internal class FilteredSceneTestCaseSource : SceneGraphicsTestCaseSource
    {
        static readonly string[] k_ExcludedPatterns = { "WIP_", "Experimental_" };

        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite)
        {
            return base.GetTestCases(methodInfo, suite)
                .Where(tc => !k_ExcludedPatterns.Any(p => tc.Name.StartsWith(p)));
        }
    }
}
