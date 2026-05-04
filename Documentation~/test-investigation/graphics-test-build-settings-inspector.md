# Graphics Test Build Settings Inspector window reference

Explore the properties and settings in the **Inspector** window of a Graphics Test Build Settings asset. 

These settings only affect the build if you assign the asset in the **Settings** tab of the Graphics Test window. For more information, refer to [Graphics Test window Settings tab reference](graphics-test-settings-tab.md).

For more information, refer to [Customize tests](../build-customization/customize-a-test.md).

| **Property** | **Description** |
|:---|:---|
| **Auto Build Test Cases** | Automatically builds a test case and passes it to the test method when the method has a [`[GraphicsTest]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestAttribute) attribute. |
| **Image Results Path** | Sets the folder in the **Project** window where Unity stores test result images. The default is `Assets/ActualImages`. |
| **Platform Schemata** | Defines the folder hierarchy in the **Project** window that Unity uses to store images. |

## Platform schemata

Each dropdown section in the **Platform Schemata** creates a new set of subfolders. The default dropdown sections are:

- **Default**: The subfolder structure Unity uses for platform-specific images.
- **Base**: The subfolder structure Unity uses for shared images, for example if you [`Optimize reference images`](../performance-optimization/reference-image-optimization.md).

Unity looks for images in the order of the sections, then from the deepest subfolder up.

| **Property** | **Description** |
|:---|:--|
| **Name** | Sets the name of the section. The name must correspond to a C# type name that implements [`IPlatformNode`](xref:UnityEngine.TestTools.Graphics.IPlatformNode). |
| **Root Path** | Sets the top level of the folder hierarchy in the **Project** window. For example, **Assets/ReferenceImages**. | 
| **Nodes** | Sets the nested folder structure under the root path. Each node is the child of the node above it. The built-in options for a node type are: <ul><li>**Color Space**: The color space. For example **Linear**.</li><li>**RuntimePlatform**: The platform. For example **WindowsEditor** or **WindowsPlayer**.</li><li>**Architecture**: The architecture. For example **x64** or **ARM64**.</li><li>**GraphicsDeviceType**: The graphics API, for example **Direct3D12**.</li><li>**XrDevice**: The XR device type, for example **OpenXR** or **None**.</li></ul> |
| **Add** (**+**) | Adds a node to the section. |
| **Remove** (**-**) | Removes a node from the section. Select a node first, then select this button. |


## Reference Image Optimization

| **Property** | **Description** |
|:---|:---|
| **Auto Optimize** | Automatically [deduplicates reference images](../performance-optimization/reference-image-optimization.md) during the build. |
| **Concurrency limit** | Sets the maximum number of images Unity deduplicates at the same time. |

## Visualization

| **Property** | **Description** |
|:---|:---|
| **Heatmap Color Scheme** | Sets the color scheme Unity uses for the heatmap view in the **Graphics Test** window. The options are: <ul><li>**Grayscale**: Displays a grayscale color comparison, where black represents low difference areas and white represents high difference areas.</li><li>**Green Red**: Displays a red-green color comparison, where green represents low difference areas and red represents high difference areas.</li><li>**Blue Yellow**: Displays a blue-yellow color comparison, where blue represents low difference areas and yellow represents high difference areas.</li></ul> |

## Advanced

| **Property** | **Description** |
|:---|:---|
| **Always Save Results** | Saves test result images even if the tests pass. |
| **Enable Scene Watcher** | Triggers a domain reload when a scene file changes. For more information, refer to [Compilation and code reload](https://docs.unity3d.com/Manual/compilation-and-code-reload.html). |
| **Enable Shader Stripping** | Removes shader variants you don't need when you build a test, to reduce build times and the size of the built Unity Player. For more information, refer to [Strip shader variants from test builds](../performance-optimization/graphics-test-shader-stripping.md). |
| **Override Ignore Attributes** | Ignores attributes such as `[IgnoreGraphicsTest]`. For more information, refer to [Graphics Tests window reference](the-graphics-tests-window.md). |
| **Shader Warnings As Errors** | Fails a test if there are shader compilation warnings during the test. |

## Latest Build Settings (Read Only)

This section displays information about the most recent test build.

| **Property** | **Description** |
|:---|:---|
| **Scene Paths** | Displays the paths of the scenes Unity tested. |
| **Test Content Bundle Paths** | Displays the paths of the AssetBundles Unity tested. |
| **Build Platforms** | Displays the platforms Unity tested. |
| **Scene Lists** | Displays the scene lists Unity tested. |
| **Build Platform Schemata** | Displays the folder structures Unity used to store images for each platform. |

## Additional resources

- [Customizing and optimizing tests](../build-customization/build-customization-landing.md)
- [Run code before a test](../build-customization/custom-pre-build-steps.md)
- [Deduplicate reference images](../performance-optimization/reference-image-optimization.md)
