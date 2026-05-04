using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.AdvancedPatterns
{
    /*
    -- Tutorial [1] --
    This sample demonstrates GlobalContext<TEnum>: a mutable platform context node.

    GlobalContext differs from a read-only IPlatformNode in that it can change state
    at runtime via the Activate method. Use GlobalContext when:
      - The context represents a runtime-configurable feature toggle
        (e.g., GPU Resident Drawer on/off, stereo rendering on/off)
      - Tests need to activate different modes and verify rendering for each

    The framework auto-discovers all GlobalContext<TEnum> subclasses via reflection
    and registers them in PlatformNodeRegistry. The enum type becomes part of
    GraphicsTestPlatform, influencing reference image paths and ignore filters.
    */

    /*
    -- Tutorial [2] --
    Step 1: Define an enum for your context values.
    Each value represents a distinct state that affects rendering output.
    */

    public enum RenderingQualityContext
    {
        Default = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4,
    }

    /*
    -- Tutorial [3] --
    Step 2: Implement GlobalContext<TEnum>.
    Override Current to report the active state, and Activate to change it.

    Simple contexts with no side effects need no overrides at all:
      public class MyContext : GlobalContext<MyEnum> { }
    The base class stores and returns the value passed to Activate.

    Contexts with side effects override Activate to apply changes:
    */

    public class RenderingQualityGlobalContext : GlobalContext<RenderingQualityContext>
    {
        int m_PreviousQualityLevel;

        public override Enum Current
        {
            get
            {
                var level = QualitySettings.GetQualityLevel();
                return level switch
                {
                    0 => RenderingQualityContext.Low,
                    1 => RenderingQualityContext.Medium,
                    2 => RenderingQualityContext.High,
                    3 => RenderingQualityContext.Ultra,
                    _ => RenderingQualityContext.Default,
                };
            }
        }

        public override void Activate(Enum value)
        {
            base.Activate(value);

            var context = (RenderingQualityContext)value;
            m_PreviousQualityLevel = QualitySettings.GetQualityLevel();

            var targetLevel = context switch
            {
                RenderingQualityContext.Low => 0,
                RenderingQualityContext.Medium => 1,
                RenderingQualityContext.High => 2,
                RenderingQualityContext.Ultra => 3,
                _ => m_PreviousQualityLevel,
            };

            QualitySettings.SetQualityLevel(targetLevel, applyExpensiveChanges: true);
        }
    }

    /*
    -- Tutorial [4] --
    Step 3: Use the context in tests.
    The context is automatically part of GraphicsTestPlatform. Reference images
    are stored in paths that include the context value (e.g., .../High/...).

    You can also use GlobalContextManager to assert or query context state.
    The test itself uses [GraphicsTest] since the focus here is on the context
    definition, not on scene-based testing.
    */

    [Category("Samples")]
    [TestOf(nameof(GlobalContextExample))]
    internal class GlobalContextExample
    {
        [Test, GraphicsTest]
        [Description("Tests rendering output under the active quality context.")]
        public void QualityContext_RendersCorrectly(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }
}
