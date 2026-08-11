# Graphics Test Framework sample projects

Learn the structure of a project that uses the Graphics Test Framework.

## Prerequisites

To use the sample projects, [install the Graphics Test Framework package](install-and-upgrade.md) in your project.

## Initial setup

1. From the main menu, select **Window** &gt; **Package Manager** to open the Package Manager window.
2. Select **All Packages** then **Graphics Test Framework**.
3. Select the **Samples** tab.
4. Select **Import** next to a sample.

Unity installs samples in the `Assets/Samples/Graphics Test Framework/<version>/` folder in the **Project** window.

## Write a basic test

Open the scripts in the **Basic Graphics Tests** folder for simple examples of [`[GraphicsTest]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestAttribute) and [`[SceneGraphicsTest]`](xref:UnityEngine.TestTools.Graphics.SceneGraphicsTestAttribute) tests. For more information, refer to [Write a scene test](test-authoring/writing-graphics-tests.md) and [Write a rendering code test](test-authoring/writing-graphics-tests-texture.md).

## Write a parameterized test

Open the scripts in the **Parameterized Tests** folder for examples of parameterized tests with [`[GraphicsTestParam]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestParamAttribute) and [`[GraphicsTestParamSource]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestParamSourceAttribute) attributes. For more information, refer to [Write a scene test](test-authoring/writing-graphics-tests.md).

## Customize image comparison

Open the scripts in the **Image Comparison Examples** folder for examples of using [`ImageComparisonSettings`](xref:UnityEngine.TestTools.Graphics.ImageComparisonSettings) to configure different comparison algorithms and thresholds. For more information, refer to [Customize tests](build-customization/customize-a-test.md).

## Filter tests by platform

Open the scripts in the **Platform Filtering Examples** folder for examples of ignoring tests using [`[IgnoreGraphicsTest]`](xref:UnityEngine.TestTools.Graphics.IgnoreGraphicsTestAttribute), [`[TestNotSupportedOn]`](xref:UnityEngine.TestTools.Graphics.TestNotSupportedOnAttribute), and [`[TestOnlySupportedOn]`](xref:UnityEngine.TestTools.Graphics.TestOnlySupportedOnAttribute) attributes. For more information, refer to [Test specific platforms](test-authoring/get-platform-information.md).

## Capture a Unity Editor window

Open the scripts in the **Editor Window Capture** folder for examples of using [`EditorWindowCapture`](xref:UnityEditor.TestTools.Graphics.EditorWindowCapture) to capture Editor windows and the Scene view. For more information, refer to [Write a Unity Editor window test](test-authoring/compare-editor-window-captures.md).

## Advanced use cases

Open the scripts in the **Advanced Patterns** folder for examples of advanced use cases, such as custom test case sources, custom pre-build setup attributes, `GlobalContext`, custom [`IPlatformNode`](xref:UnityEngine.TestTools.Graphics.IPlatformNode) implementations, eliding a sentinel platform value from reference image paths with [`[ElideFromPlatformPath]`](xref:UnityEngine.TestTools.Graphics.Platforms.ElideFromPlatformPathAttribute), and the [`GraphicsTestLogger`](xref:UnityEngine.TestTools.Graphics.GraphicsTestLogger). For more information, refer to [Run code before a test](build-customization/custom-pre-build-steps.md), [Customize tests](build-customization/customize-a-test.md), and [Test specific platforms](test-authoring/get-platform-information.md).

## Additional resources

- [Write a scene test](test-authoring/writing-graphics-tests.md)
- [Write a shader test](test-authoring/writing-shader-tests.md)
- [Graphics Tests window reference](test-investigation/the-graphics-tests-window.md)
