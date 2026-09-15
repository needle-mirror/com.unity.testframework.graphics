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

Each dropdown in the **Platform Schemata** is one schema, and each schema creates its own set of subfolders. The default schemata are:

- **Default**: The subfolder structure Unity uses for platform-specific images.
- **Base**: The subfolder structure Unity uses for shared images, for example if you [`Optimize reference images`](../performance-optimization/reference-image-optimization.md).

To find a test's reference image, Unity searches one schema at a time. Within a schema it starts at the deepest subfolder the run matches and walks up to the root path. The first image it finds is the one the test compares against; if a schema holds no image for the test, Unity moves on to the next schema.

Unity searches the schemata in this order:

1. Schemata with a node that identifies the machine, such as a device model, when the run reports a value for it.
2. Schemata whose nodes only describe how the run was configured.
3. Schemata with an identifying node that the run reported no value for.
4. Schemata with no nodes, which match every run.

An image in a device subfolder was captured on that device, so its schema is searched before a schema that constrains more dimensions but names no device. A schema whose identifying node went unreported has lost that claim, so it drops below the configuration schemata and Unity reads its folders as an ordinary fallback.

Schemata in the same group are searched in order of how many dimensions they constrain, most first, because each one is one more thing the run had to match. Schemata constraining the same number are ordered by name, then by root path, then by node sequence.

A global context doesn't count. A test fixture appends its context to every schema that doesn't declare one, so counting it would let the fixture decide the order — and a schema left with nothing but contexts still matches every run, so it stays the universal fallback. Declaring a context only fixes the depth of its folder.

The order doesn't depend on where a schema came from or on the order you list them, so a schema contributed by a package is ordered by the same rule as your own. To change which of two equally specific schemata Unity searches first, rename one: the name is the only key you can change without moving reference images. Any combination of schemata is valid: a schema that constrains a node another schema doesn't is never an error.

To prevent a specific node value, such as a `None` or `Unknown` sentinel, from creating a subfolder, mark its enum member with the [`[ElideFromPlatformPath]`](xref:UnityEngine.TestTools.Graphics.Platforms.ElideFromPlatformPathAttribute) attribute. For more information, refer to [Test specific platforms](../test-authoring/get-platform-information.md).

| **Property** | **Description** |
|:---|:--|
| **Name** | Sets the name of the schema. The name must correspond to a C# type name that implements [`IPlatformNode`](xref:UnityEngine.TestTools.Graphics.IPlatformNode). |
| **Root Path** | Sets the top level of the folder hierarchy in the **Project** window. For example, **Assets/ReferenceImages**. | 
| **Nodes** | Sets the nested folder structure under the root path. Each node is the child of the node above it. The built-in options for a node type are: <ul><li>**Color Space**: The color space. For example **Linear**.</li><li>**RuntimePlatform**: The platform. For example **WindowsEditor** or **WindowsPlayer**.</li><li>**Architecture**: The architecture. For example **x64** or **ARM64**.</li><li>**GraphicsDeviceType**: The graphics API, for example **Direct3D12**.</li><li>**XrDevice**: The XR device type, for example **OpenXR** or **None**.</li><li>**GraphicsVendor**: The GPU manufacturer, for example **NVIDIA**. **AMD** and **ATI** are the same value, and Unity always names the folder **ATI**.</li><li>A global context activated by a test fixture, for example **GpuResidentDrawerContext**. If a schema doesn't declare the context, Unity appends it after the schema's last node. For more information, refer to [Test specific platforms](../test-authoring/get-platform-information.md).</li></ul> |
| **Add** (**+**) | Adds a node to the schema. |
| **Remove** (**-**) | Removes a node from the schema. Select a node first, then select this button. |

You can't drag entries in **Platform Schemata** into a different order, because the order you list them in doesn't change which one Unity searches first.

## Contributed by Image Build Settings

This section appears when the project contains [Image Build Settings](#image-build-settings) assets, and lists the schemata each one adds to the build. It's read-only: to change an entry, select the asset it came from.

A schema marked **already covered by this asset, not built** reads the same root path and nodes as one in **Platform Schemata** above, so Unity builds only that copy.

## Image Build Settings

An **Image Build Settings** asset holds **Platform Schemata** outside the Graphics Test Build Settings asset. Unity adds the schemata of every such asset in the project to the build, so you can ship reference images and their folder structure inside a package.

To create one, select **Assets** > **Create** > **Graphics Test Framework** > **Image Build Settings**. Its **Platform Schemata** work as described in [Platform schemata](#platform-schemata). To reference images in a package, set each schema's **Root Path** to the package path, for example `Packages/<your-package-name>/<folder>`.

To move schemata between assets, select **Extract to Image Build Settings** in the Graphics Test Build Settings asset, or **Import from GraphicsTestBuildSettings** in the Image Build Settings asset.

**Note**: If two schemata share a name but resolve to different reference images, Unity keeps their bundles apart by adding a hash to the bundle name, for example `base-standalone-3f9a1c-0`, and logs the mapping. Rename one of the schemata to avoid this.

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
