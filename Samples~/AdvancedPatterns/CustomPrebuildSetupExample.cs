using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.AdvancedPatterns
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to create a custom pre-build setup step by
    extending GraphicsPrebuildSetupAttribute. Pre-build setup steps run before
    the test player is built, allowing you to:
      - Import or generate assets
      - Configure project settings
      - Validate prerequisites
      - Run custom build preparation logic

    The 'order' parameter controls execution order: lower numbers run first.
    [BakeLighting] is itself a built-in subclass of GraphicsPrebuildSetupAttribute.
    */

    /*
    -- Tutorial [2] --
    Define a custom pre-build setup by subclassing GraphicsPrebuildSetupAttribute
    and overriding the Setup() method. The base constructor accepts an order value.
    */

    internal class ValidateTestScenesAttribute : GraphicsPrebuildSetupAttribute
    {
        readonly string[] m_RequiredScenes;

        public ValidateTestScenesAttribute(params string[] requiredScenes)
            : base(order: 0)
        {
            m_RequiredScenes = requiredScenes;
        }

        protected override void Setup()
        {
#if UNITY_EDITOR
            foreach (var scenePath in m_RequiredScenes)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[ValidateTestScenes] Required scene not found: {scenePath}");
                }
            }

            Debug.Log($"[ValidateTestScenes] Validated {m_RequiredScenes.Length} required scenes.");
#endif
        }
    }

    /*
    -- Tutorial [3] --
    Another example: a setup that configures quality settings before build.
    Higher order values run later, so this runs after scene validation (order 0).
    */

    internal class ConfigureQualitySettingsAttribute : GraphicsPrebuildSetupAttribute
    {
        readonly int m_QualityLevel;

        public ConfigureQualitySettingsAttribute(int qualityLevel)
            : base(order: 5)
        {
            m_QualityLevel = qualityLevel;
        }

        protected override void Setup()
        {
            QualitySettings.SetQualityLevel(m_QualityLevel, applyExpensiveChanges: true);
            Debug.Log($"[ConfigureQualitySettings] Set quality level to {m_QualityLevel}.");
        }
    }

    /*
    -- Tutorial [4] --
    Apply custom pre-build setup attributes to test classes or methods.
    Multiple attributes are supported; they execute in order of their 'order' value.
    The test itself uses [GraphicsTest] since the focus here is on the setup attributes,
    not on scene-based testing.
    */

    [ValidateTestScenes("Assets/Scenes/TestScene.unity")]
    [ConfigureQualitySettings(2)]
    [Category("Samples")]
    [TestOf(nameof(CustomPrebuildSetupExample))]
    internal class CustomPrebuildSetupExample
    {
        [Test, GraphicsTest]
        [Description("Tests run after custom pre-build validation and configuration.")]
        public void AfterCustomSetup_RendersCorrectly(GraphicsTestCase testCase)
        {
            var actualImage = new Texture2D(1, 1);
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
        }
    }
}
