using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Tests
{
    /*
    -- Tutorial [1] --
    This class demonstrates how to use the Graphics Test Framework's SceneGraphicsTestAttribute with multiple test methods.
    */

    [Category("FeatureSet")]
    // The TestOf attribute is used to specify the production class that this test fixture is covering. This is useful for tracking and organizing tests.
    [TestOf(nameof(GraphicsTestWithMarkupExample))]
    internal class GraphicsTestWithMarkupExample
    {
        /*
        -- Tutorial [2] --
        These string are used to define the location of the test scenes.
        Test scenes may be located in the Assets or Packages folders.
        
        These strings will later be used in the `SceneGraphicsTest` attribute to specify the test scene paths. You can also use direct paths to scenes or regular expressions to match multiple scenes.

        Here, the two strings are the same, but in a real-world scenario, they would be different.
        For example, you may have different categories of tests, such as "FeatureA", "FeatureB", and "FeatureC".
        You can also use different directories for each category, such as:
          - Assets/TestScenes/FeatureA
          - Assets/TestScenes/FeatureB
          - Assets/TestScenes/FeatureC
        */

        const string k_TestSceneDirectoryCategoryA = "Assets/TestScenes";
        const string k_TestSceneDirectoryCategoryB = "Assets/TestScenes";

        /*
        -- Tutorial [3] --
        We can add markup to test methods and test classes to provide additional information about the tests. The markup is used to specify the properties of the test, such as its category, description, author and other properties.
        
        The test class is marked with the `Category` attribute to group the tests of the same feature set (FeatureSet) together. This could be a superset of features in the same system, such as "Post Processing" or "Lighting".

        Categories can also denote meta-information about the tests, such as its test type (e.g. "Performance", "Regression", "Smoke") or its test status (e.g. "Stable", "Experimental", "Deprecated") or how quickly it runs (e.g. "Fast", "Medium", "Slow").
        */

        // Specifying an author for the test allows for better tracking of test ownership.
        [Author("Your Name", "your.email@domain.com")]
        // Adding a description to the test helps in understanding its purpose. This string will be displayed in the test results.
        [Description("This test verifies the functionality of Feature A with some data.")]
        // Categorizing the test allows for better organization and filtering of tests. We can use this category to run only tests related to Feature A, for example by using the UTR argument `--category FeatureA`.
        [Category("FeatureA")]
        // It is also possible to use custom properties to add additional information to the test. For example, we can specify the team responsible for the test.
        [NUnit.Framework.Property("Team", "Graphics Team")]
        // The `SceneGraphicsTest` attribute is used to specify the test scene for this test. The scene path is defined in the constant string above.
        [UnityTest, SceneGraphicsTest(k_TestSceneDirectoryCategoryA)]
        public IEnumerator FeatureA_WithSomeData_ExpectedOutcome(
            SceneGraphicsTestCase graphicsTestCase
        )
        {
            yield return RunTest(graphicsTestCase);
        }

        /*
        -- Tutorial [4] --
        The test method below is a simple test that verifies the functionality of Feature A with some different data.
        You can have multiple test methods or classes of the same category.
        This is useful when you want to test different aspects of the same feature or scene.
        Tests groups under the same category may be run together.
        They can be anywhere in a project and will still run, as long as they are run in the same test session.
        */

        [Category("FeatureA")]
        [Description("This test verifies the functionality of Feature A with some other data.")]
        [UnityTest, SceneGraphicsTest(k_TestSceneDirectoryCategoryA)]
        public IEnumerator FeatureA_WithSomeDifferentData_ExpectedOutcome(
            SceneGraphicsTestCase graphicsTestCase
        )
        {
            // Do something different
            yield return RunTest(graphicsTestCase);
        }

        /*
        -- Tutorial [5] --
        The test method below is a simple test that verifies the functionality of Feature B with some data.
        This set of tests is in a different category than the previous ones.
        */

        [Description("This test verifies the functionality of Feature B with some data.")]
        [Category("FeatureB")]
        [IgnoreGraphicsTest("Scene3", "Reason")] // Test ignores are also supported and are applied to each test method separately.
        [UnityTest, SceneGraphicsTest(k_TestSceneDirectoryCategoryB)]
        public IEnumerator FeatureB_WithSomeData_ExpectedOutcome(
            SceneGraphicsTestCase graphicsTestCase
        )
        {
            yield return RunTest(graphicsTestCase);
        }

        /*
        -- Tutorial [6] --
        We can also do integration tests with multiple test categories.

        We have also marked this test as 'explicit'.
        Marking tests as explicit will prevent them from being run by default. However, if they are explicitly requested (individually or as part of a set), such as their category, they will be run.
        In this case, the test will be run if the categories "FeatureA", "FeatureB", "FeatureSet" or "Integration" are requested.
        It will not be run if there is no category or filter specified.
        */

        [Description("This test verifies the functionality of Feature B with some data.")]
        [Category("FeatureA")]
        [Category("FeatureB")]
        [Category("Integration")]
        [Explicit("Integration tests are not run by default.")]
        [UnityTest, SceneGraphicsTest(k_TestSceneDirectoryCategoryB)]
        public IEnumerator FeatureAFeatureB_WithSomeIntegration_ExpectedOutcome(
            SceneGraphicsTestCase graphicsTestCase
        )
        {
            yield return RunTest(graphicsTestCase);
        }

        static IEnumerator RunTest(GraphicsTestCase testCase)
        {
            yield return null;
        }
    }
}
