# Test specific platforms

Write tests for specific platforms, or ignore tests on platforms that don't meet specific requirements.

## Get platform information

To get information about the GPU and graphics capabilities of the test environment, use the `GraphicsDeviceInfo` class. For example, to check the shader model level:

```csharp
if (GraphicsDeviceInfo.ShaderLevel < 30)
{
    Debug.Log("Shader model 3.0 is required for this test.");
}
```        

For more information, refer to the [GraphicsDeviceInfo](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsDeviceInfo) API.

## Ignore a test on a specific platform

To ignore tests on a specific platform, add a [`[IgnoreGraphicsTest]`](xref:UnityEngine.TestTools.Graphics.IgnoreGraphicsTestAttribute) attribute. Add the following parameters:

- A regex pattern to match test case names. For example, `".*"` to match all tests.
- A message describing the reason for ignoring the test.
- One or more platform features, for example `GraphicsDeviceType.Vulkan` or `GraphicsVendor.AMD`. The test is ignored if a platform matches all the platform features in the attribute.

For example:

```csharp
[Test, GraphicsTest]
[IgnoreGraphicsTest(".*", "Test ignored on AMD GPUs with Vulkan or OpenGLES3", GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3, GraphicsVendor.AMD)]
```

To ignore more than one platform type, add multiple `[IgnoreGraphicsTest]` attributes. For example, the following code ignores the test if the platform uses Vulkan or uses an AMD GPU:

```csharp
[Test, GraphicsTest]
[IgnoreGraphicsTest(".*", "Test ignored on Vulkan", GraphicsDeviceType.Vulkan)]
[IgnoreGraphicsTest(".*", "Test ignored on AMD GPUs", GraphicsVendor.AMD)]

```

For more information about platform features, refer to the [GraphicsDeviceInfo](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsDeviceInfo) API.

## Create a custom platform reference

To create a custom platform reference, create a [`GraphicsTestPlatform`](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsTestPlatform) instance with details about the specific platform, color space, graphics API, GPU manufacturer, and XR device.

You can use a `GraphicsTestPlatform` instance to do the following:

- Represent a folder hierarchy in the **Project** window.
- Evaluate `[IgnoreGraphicsTest]` conditions against a specific configuration.

For example, the following creates platform reference data that uses linear color space and has a Direct3D11 graphics API, NVIDIA GPU, and no XR device:

```csharp
GraphicsTestPlatform customPlatform = new GraphicsTestPlatform(
    ColorSpace.Linear,
    RuntimePlatform.WindowsEditor,
    GraphicsDeviceType.Direct3D11,
    GraphicsVendor.NVIDIA,
    XrDevice.None
);
```

To get information about the platform of the running Unity Editor or built application, use [`GraphicsTestPlatform.Current`](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsTestPlatform.Current).

To add a custom platform feature, for example a new type of graphics API or GPU manufacturer, implement the [`IPlatformNode`](xref:UnityEngine.TestTools.Graphics.IPlatformNode) interface. For an example, refer to [Sample projects](../sample-projects.md).

## Additional resources

- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)
- [Graphics Test Settings component reference](../test-investigation/graphics-test-settings-component.md)
- [Customize a test](../build-customization/customize-a-test.md)
