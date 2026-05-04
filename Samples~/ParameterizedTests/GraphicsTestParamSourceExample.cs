using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.ParameterizedTests
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to use GraphicsTestParamSource to supply parameter sets
    from an external source class. This is useful when:
      - The number of parameter combinations is large or dynamic
      - Parameter generation involves logic that doesn't belong in an attribute
      - You want to share parameter sets across multiple test methods
    The source class must implement IEnumerable<object[]>.
    */

    [Category("Samples")]
    [TestOf(nameof(GraphicsTestParamSourceExample))]
    internal class GraphicsTestParamSourceExample
    {
        /*
        -- Tutorial [2] --
        Basic usage with a custom param source.
        CustomGraphicsTestParamSource yields three sets of arguments.
        Each yielded object[] maps to the extra parameters after GraphicsTestCase.
        */

        [Test, GraphicsTest]
        [GraphicsTestParamSource(typeof(CustomGraphicsTestParamSource))]
        [Description("Verifies rendering with parameter sets from an external source.")]
        public void ExternalSource_RendersCorrectly(GraphicsTestCase testCase, int qualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityLevel);
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }

        /*
        -- Tutorial [3] --
        A param source with multiple arguments per set.
        Each object[] in the enumeration supplies values for all extra parameters.
        */

        [Test, GraphicsTest]
        [GraphicsTestParamSource(typeof(ResolutionParamSource))]
        [Description("Verifies rendering at various resolution and HDR configurations.")]
        public void ResolutionConfig_RendersCorrectly(
            GraphicsTestCase testCase,
            int width,
            int height,
            bool useHdr
        )
        {
            var settings = new ImageComparisonSettings
            {
                TargetWidth = width,
                TargetHeight = height,
                UseHDR = useHdr,
            };

            var actualImage = new Texture2D(width, height);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage, settings);
        }
    }

    /*
    -- Tutorial [4] --
    A simple param source that yields single-value argument sets.
    The class is instantiated via Activator.CreateInstance, so it must have
    a parameterless constructor (the default one suffices here).
    */

    internal class CustomGraphicsTestParamSource : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { 2 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /*
    -- Tutorial [5] --
    A more complex param source that yields multi-value argument sets.
    This pattern is useful for generating resolution/HDR matrix combinations
    without polluting the test method with attribute noise.
    */

    internal class ResolutionParamSource : IEnumerable<object[]>
    {
        static readonly (int Width, int Height, bool UseHdr)[] k_Configs =
        {
            (256, 256, false),
            (512, 512, false),
            (512, 512, true),
            (1920, 1080, false),
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var (width, height, useHdr) in k_Configs)
                yield return new object[] { width, height, useHdr };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
