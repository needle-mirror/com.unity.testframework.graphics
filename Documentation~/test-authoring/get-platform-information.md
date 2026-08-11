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

## Build for multiple platform characteristics from the command line

By default, Unity builds reference-image content for the platform of the machine that runs the build. To make a single build carry content for several values of a platform characteristic — for example, so one player build serves test machines with different GPU vendors — pass a `-combine<Node>s` command-line argument to the Unity Editor that performs the build:

```
-combineGraphicsVendors=AMD,Nvidia
```

One argument exists for each platform characteristic, derived from the name of its [`IPlatformNode`](xref:UnityEngine.TestTools.Graphics.IPlatformNode) implementation: `-combineGraphicsVendors`, `-combineColorSpaces`, `-combineXrDevices`, and so on, including custom platform nodes. Argument names and values are case-insensitive, and both `-arg=value` and `-arg value` forms work.

The build creates a separate platform variant for every value, and the resulting player selects the content that matches the platform it actually runs on, falling back to less specific content when no exact match exists.

The following rules apply:

- **Precedence**: A characteristic pinned by a test fixture argument always wins. A command-line value overrides a value detected from the build environment. For example, if a fixture is declared with `GraphicsVendor.AMD`, that fixture's tests keep AMD regardless of `-combineGraphicsVendors`.
- **Cartesian product**: Passing several combine arguments builds every combination of the listed values. For example, two vendors and two XR devices produce four platform variants. Bundle count and build time grow accordingly.
- **Validation**: An unknown value, an empty value, or a misspelled `-combine...` argument fails the build with a message listing the valid options.

## Elide a platform value from reference image paths

Each platform value adds a subfolder to the reference image path. Some platform dimensions include a sentinel value that represents the absence of a feature, such as `None` or `Unknown`. Because most tests use this sentinel, it creates a redundant subfolder that holds the majority of the reference images.

To stop a value from adding a subfolder, mark the corresponding enum member with the [`[ElideFromPlatformPath]`](xref:UnityEngine.TestTools.Graphics.Platforms.ElideFromPlatformPathAttribute) attribute. For example, the following enum stores images for the `Unknown` tier in the parent folder instead of an `Unknown` subfolder:

```csharp
public enum GpuMemoryTier
{
    [ElideFromPlatformPath]
    Unknown,
    Low,
    Medium,
    High,
}
```

An elided value still participates in platform equality, `GetValue<T>`, and platform filtering. Unity omits only the folder segment.

To get the full path including elided segments, use [`GraphicsTestPlatform.ResultsPathWithElided`](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsTestPlatform.ResultsPathWithElided) or [`GraphicsTestPlatform.AllResultsPathsWithElided`](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsTestPlatform.AllResultsPathsWithElided).

For an example, refer to the **Advanced Patterns** folder in [Sample projects](../sample-projects.md).

## Additional resources

- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)
- [Graphics Test Settings component reference](../test-investigation/graphics-test-settings-component.md)
- [Customize a test](../build-customization/customize-a-test.md)
