using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.Graphics;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.EditorTestExamples
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to use EditorWindowCapture to capture Editor windows
    as Texture2D images for visual comparison in Edit Mode tests.

    EditorWindowCapture.CaptureAsync works with any EditorWindow subclass and provides:
      - Custom width/height for the capture resolution
      - A delay before capture (for async shader compilation, layout settling, etc.)
      - Additional setup actions to configure the window before capture
      - Specialized SceneViewCaptureSettings for SceneView captures with viewpoint control

    These tests must be Editor-only (Edit Mode tests).
    */

    [Category("Samples")]
    [TestOf(nameof(EditorWindowCaptureExample))]
    internal class EditorWindowCaptureExample
    {
        /*
        -- Tutorial [2] --
        Basic EditorWindow capture with default settings.
        EditorWindowCaptureSettings.Default uses 512x512 resolution with no delay.
        The returned Texture2D can then be used with ImageAssert for comparison.
        */

        [Test]
        [Description("Captures a custom EditorWindow with default settings.")]
        public async Task BasicCapture_DefaultSettings()
        {
            var window = EditorWindow.CreateWindow<TestEditorWindow>();
            try
            {
                var captured = await EditorWindowCapture.CaptureAsync(
                    window,
                    EditorWindowCaptureSettings.Default
                );

                Assert.That(captured, Is.Not.Null);
                Assert.That(captured.width, Is.EqualTo(512));
                Assert.That(captured.height, Is.EqualTo(512));
            }
            finally
            {
                window.Close();
            }
        }

        /*
        -- Tutorial [3] --
        Custom dimensions and delay.
        Use a delay when the window needs time to initialize, load assets,
        or compile shaders before the capture is meaningful.
        */

        [Test]
        [Description("Captures with custom dimensions and a delay for initialization.")]
        public async Task CustomDimensions_WithDelay()
        {
            var window = EditorWindow.CreateWindow<TestEditorWindow>();
            try
            {
                var settings = new EditorWindowCaptureSettings(
                    width: 1024,
                    height: 768,
                    delayBeforeCapture: TimeSpan.FromMilliseconds(500)
                );

                var captured = await EditorWindowCapture.CaptureAsync(window, settings);

                Assert.That(captured, Is.Not.Null);
                Assert.That(captured.width, Is.EqualTo(1024));
                Assert.That(captured.height, Is.EqualTo(768));
            }
            finally
            {
                window.Close();
            }
        }

        /*
        -- Tutorial [4] --
        Additional setup actions.
        Setup actions are delegates that run after window setup but before the delay
        and capture. Use them to configure the window state for the test:
        focusing a specific panel, setting a tool, toggling overlays, etc.
        */

        [Test]
        [Description("Captures with additional setup actions to configure the window.")]
        public async Task CaptureWithSetupActions()
        {
            var window = EditorWindow.CreateWindow<TestEditorWindow>();
            try
            {
                Action<EditorWindow> setBackgroundColor = w =>
                {
                    w.rootVisualElement.style.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
                };

                var settings = new EditorWindowCaptureSettings(
                    width: 512,
                    height: 512,
                    delayBeforeCapture: TimeSpan.FromMilliseconds(100),
                    setBackgroundColor
                );

                var captured = await EditorWindowCapture.CaptureAsync(window, settings);

                Assert.That(captured, Is.Not.Null);
            }
            finally
            {
                window.Close();
            }
        }

        /*
        -- Tutorial [5] --
        SceneView capture with SceneViewCaptureSettings.
        SceneViewCaptureSettings extends EditorWindowCaptureSettings with:
          - ImageComparisonViewpoint: the world-space Transform to look from
          - Timeout: maximum wait for the camera to align and shaders to compile

        The capture will align the scene view camera to the specified viewpoint,
        wait for async shader compilation to finish, then capture.
        */

        [Test]
        [Description("Captures a SceneView from a specific viewpoint with timeout.")]
        public async Task SceneViewCapture_WithViewpoint()
        {
            var sceneView = EditorWindow.CreateWindow<SceneView>();
            try
            {
                var viewpoint = sceneView.camera.transform;

                var settings = new SceneViewCaptureSettings(
                    width: 512,
                    height: 512,
                    delayBeforeCapture: TimeSpan.FromMilliseconds(200),
                    timeout: TimeSpan.FromSeconds(10),
                    imageComparisonViewpoint: viewpoint
                );

                var captured = await EditorWindowCapture.CaptureAsync(sceneView, settings);

                Assert.That(captured, Is.Not.Null);
                Assert.That(captured.width, Is.EqualTo(512));
            }
            finally
            {
                sceneView.Close();
            }
        }

        /*
        -- Tutorial [6] --
        Combining EditorWindowCapture with ImageAssert for visual regression testing.
        This is the end-to-end pattern: capture a window and compare to a reference image.
        */

        [Test, GraphicsTest]
        [Description("Full workflow: capture an EditorWindow and compare to a reference image.")]
        public async Task CaptureAndCompare_ToReferenceImage(GraphicsTestCase testCase)
        {
            var window = EditorWindow.CreateWindow<TestEditorWindow>();
            try
            {
                var captured = await EditorWindowCapture.CaptureAsync(
                    window,
                    new EditorWindowCaptureSettings(512, 512, TimeSpan.FromMilliseconds(100))
                );

                var settings = new ImageComparisonSettings
                {
                    TargetWidth = 512,
                    TargetHeight = 512,
                    AverageCorrectnessThreshold = 0.01f,
                    ActiveImageTests = ImageComparisonSettings.ImageTests.AverageDeltaE,
                };

                ImageAssert.AreEqual(testCase.ReferenceImage.Image, captured, settings);
            }
            finally
            {
                window.Close();
            }
        }
    }

    /// <summary>
    /// A minimal EditorWindow used by the capture examples.
    /// Replace this with your own window type when using this sample.
    /// </summary>
    internal class TestEditorWindow : EditorWindow
    {
        void OnGUI()
        {
            GUILayout.Label("Graphics Test Framework - Editor Window Capture Sample");
        }
    }
}
