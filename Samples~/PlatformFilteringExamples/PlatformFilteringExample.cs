using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.PlatformFilteringExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to use platform filtering attributes to control
    which tests run on which platforms and graphics APIs.

    Three attributes are available:
      - [IgnoreGraphicsTest]: skips matching tests (can be overridden via command line)
      - [TestNotSupportedOn]: skips matching tests (cannot be overridden; the feature is absent)
      - [TestOnlySupportedOn]: runs only on listed platforms (cannot be overridden)

    Platform values can be any enum type registered as an IPlatformNode.DataType,
    such as GraphicsDeviceType and RuntimePlatform.

    These examples use [GraphicsTest] to keep the focus on the filtering attributes.
    The same attributes work identically with [SceneGraphicsTest].
    */

    [Category("Samples")]
    [TestOf(nameof(PlatformFilteringExample))]
    internal class PlatformFilteringExample
    {
        /*
        -- Tutorial [2] --
        IgnoreGraphicsTest: the most common filtering attribute.
        By default it uses regex matching (IgnoreGraphicsTestMode.MatchRegex)
        against the test case name.

        An empty pattern ("") matches all test cases in the method.
        The platforms parameter accepts any enum that is a registered IPlatformNode.DataType.
        */

        [Test, GraphicsTest]
        [IgnoreGraphicsTest("", "Known flicker on Metal due to driver bug", GraphicsDeviceType.Metal)]
        [Description("Demonstrates basic IgnoreGraphicsTest with a graphics API filter.")]
        public void BasicIgnore_ByGraphicsApi(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [3] --
        IgnoreGraphicsTest with a specific test name pattern.
        The pattern is matched against the test case name.
        When using [GraphicsTest], the test case name is derived from the method name.
        When using [SceneGraphicsTest], it comes from the scene filename.
        */

        [Test, GraphicsTest]
        [IgnoreGraphicsTest("ShadowTest", "Shadow maps not stable on GLES3", GraphicsDeviceType.OpenGLES3)]
        [Description("Ignores a specific test name pattern on a specific graphics API.")]
        public void IgnoreSpecificPattern_ByGraphicsApi(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [4] --
        IgnoreGraphicsTest with explicit match mode control.
        IgnoreGraphicsTestMode options:
          - MatchExact: pattern must match the full test case name exactly
          - MatchStart: pattern must match the beginning of the name
          - MatchEnd: pattern must match the end of the name
          - MatchRegex: pattern is a regular expression (default)
          - MatchRegexIgnoreCase: case-insensitive regex
        */

        [Test, GraphicsTest]
        [IgnoreGraphicsTest(
            "PostProcess",
            "Post-processing bloom has precision issues on mobile GPUs",
            false,
            true,
            IgnoreGraphicsTestMode.MatchExact,
            GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan
        )]
        [Description("Demonstrates exact name matching with multiple GPU types.")]
        public void ExactMatchIgnore_MultipleApis(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [5] --
        IgnoreGraphicsTest with regex pattern matching.
        The regex is matched against the test case name. This example ignores
        all test cases whose names start with a number (e.g., "001_Bloom", "002_DOF").
        */

        [Test, GraphicsTest]
        [IgnoreGraphicsTest(
            @"^\d+_",
            "Numbered legacy tests are not validated on WebGPU yet",
            false,
            true,
            IgnoreGraphicsTestMode.MatchRegex,
            GraphicsDeviceType.WebGPU
        )]
        [Description("Demonstrates regex pattern matching for ignore filters.")]
        public void RegexIgnore_LegacyTests(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [6] --
        TestNotSupportedOn: marks a test as fundamentally unsupported on certain platforms.
        Unlike IgnoreGraphicsTest, this cannot be overridden via command line because
        the test would never succeed on these platforms.

        Use this for hardware or API limitations (e.g., compute shaders on GLES3,
        ray tracing on non-DXR hardware).
        */

        [Test, GraphicsTest]
        [TestNotSupportedOn(
            "",
            "Compute shader features not available on OpenGL ES 3",
            GraphicsDeviceType.OpenGLES3
        )]
        [Description("Demonstrates TestNotSupportedOn for missing API features.")]
        public void NotSupportedOn_MissingApiFeature(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [7] --
        TestOnlySupportedOn: the inverse of TestNotSupportedOn.
        The test only runs on the listed platforms and is skipped everywhere else.

        This is useful for platform-exclusive features like DXR ray tracing
        (Direct3D12 on Windows only) or Metal-specific optimizations.
        */

        [Test, GraphicsTest]
        [TestOnlySupportedOn(
            "",
            "Hardware ray tracing requires Direct3D 12",
            GraphicsDeviceType.Direct3D12
        )]
        [Description("Demonstrates TestOnlySupportedOn for platform-exclusive features.")]
        public void OnlySupportedOn_ExclusiveFeature(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [8] --
        Combining platform filters: multiple ignore attributes on the same method.
        Each attribute is evaluated independently. The test is skipped if any of them match.

        You can also filter by RuntimePlatform in addition to GraphicsDeviceType.
        */

        [Test, GraphicsTest]
        [TestNotSupportedOn("", "Not supported on OpenGL backends",
            GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [IgnoreGraphicsTest("HDR", "HDR tonemapping differs on macOS Metal",
            GraphicsDeviceType.Metal)]
        [IgnoreGraphicsTest("", "Flaky on Linux standalone", RuntimePlatform.LinuxPlayer)]
        [Description("Demonstrates combining multiple platform filter attributes.")]
        public void CombinedFilters_MultipleAttributesAndPlatforms(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [9] --
        Filtering by RuntimePlatform.
        Platform values are not limited to GraphicsDeviceType; any enum registered
        as an IPlatformNode.DataType can be used. RuntimePlatform is registered
        by the built-in RuntimePlatformNode.

        The isInclusive parameter (third argument in the full overload) controls logic:
          - false (default): test is ignored when the current platform is in the list
          - true: test is ignored when the current platform is NOT in the list
        */

        [Test, GraphicsTest]
        [TestNotSupportedOn("", "WebGL has no async GPU readback", RuntimePlatform.WebGLPlayer)]
        [Description("Demonstrates filtering by RuntimePlatform.")]
        public void FilterByRuntimePlatform(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }
}
