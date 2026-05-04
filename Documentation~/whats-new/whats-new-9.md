# What's new in Graphics Test Framework version 9.0

Discover new features and performance improvements in the latest update to Graphics Test Framework.

For more information, refer to the [changelog](../../CHANGELOG.md).

## New Graphics Tests window

The new **Graphics Tests** window lets you check test results, compare images in a split window, apply heatmap overlays, manage reference images, and check logs. For more information, refer to [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md).

## New platform API

The new [`GraphicsTestPlatform`](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsTestPlatform) API represents a combination of platform characteristics, such as runtime platform, color space, graphics API, GPU manufacturer, and XR device. You can use it to compare platform objects and configure the hierarchy of image folders in the **Project** view. For more information, refer to [Test specific platforms](../test-authoring/get-platform-information.md) and [Graphics Test Build Settings Inspector window reference](../test-investigation/graphics-test-build-settings-inspector.md).

## New asset for configuring settings 

The new Graphics Test Build Settings asset configures the build process. For more information, refer to [Customizing and optimizing tests](../build-customization/build-customization-landing.md).

## New attribute for ignoring tests

The [`[IgnoreGraphicsTest]`](xref:UnityEngine.TestTools.Graphics.IgnoreGraphicsTestAttribute) attribute replaces Test Filter assets. The new attribute supports regex matching, platform enumeration filtering, inclusive and exclusive modes, and command-line override with `-override-graphics-test-ignores`. For more information, refer to [Test specific platforms](../test-authoring/get-platform-information.md).

`[IgnoreGraphicsTest]` supports filtering by `RenderingThreadingMode`.

## New image comparison algorithms

You can now select different built-in algorithms, include the DeltaE algorithm, peak signal-to-noise ratio (PSNR), and structural similarity index measure (SSIM). Use `Assert.That(actual, IsTexture.EqualTo(expected).Using(algorithm))` to select an algorithm. For more information, refer to [Customize tests](../build-customization/customize-a-test.md).

## Test shaders

You can now create unit tests for ShaderLab assets and HLSL shader methods. Load a shader, execute a function, and assert on the C# return value. The framework automatically generates wrapper shaders for HLSL methods. For more information, refer to [Write a shader test](../test-authoring/writing-shader-tests.md).

## Get a copy of the camera view or the back buffer

You can now use the [`ImageCapture`](xref:UnityEngine.TestTools.Graphics.ImageCapture) API to capture images with bilinear resizing and save them to disk. For more information, refer to [Write a scene test](../test-authoring/writing-graphics-tests.md).

## Improvements to testing APIs

You can now:

- Use the [`ImageAssert.AreEqual`](xref:UnityEngine.TestTools.Graphics.ImageAssert.AreEqual(UnityEngine.Texture2D,UnityEngine.Camera,UnityEngine.TestTools.Graphics.ImageComparisonSettings,System.String,System.Boolean)) API to return results.
- Store multiple variants of reference images with the `AdditionalReferenceImages` property of [`GraphicsTestCase`](xref:UnityEngine.TestTools.Graphics.GraphicsTestCase).
- Parameterize tests with inline values or external data using the [`[GraphicsTestParam]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestParamAttribute) and [`[GraphicsTestParamSource]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestParamSourceAttribute) attributes. For more information, refer to [Customize a test](../build-customization/customize-a-test.md).
- Test for compute shader functionality in the [`GraphicsDeviceInfo`](xref:UnityEngine.TestTools.Graphics.Platforms.GraphicsDeviceInfo) API. For more information, refer to [Test specific platforms](../test-authoring/get-platform-information.md).


## Performance improvements

This version includes the following performance improvements:

- The framework caches test cache data and reduces redundant calculations when it discovers tests in your project, which significantly improves performance.
- The build workflow now supports grouped scene lists, and improves platform and test case extraction, and asset and image handling. The framework adds transition scenes to each build to avoid state leaking between tests.
- Deduplicating reference images is now more efficient. For more information, refer to [Deduplicate reference images](../performance-optimization/reference-image-optimization.md).
- The framework is faster at stripping shader variants. For more information, refer to [Strip shader variants from test builds](../performance-optimization/graphics-test-shader-stripping.md).
- The framework now loads reference images from platform-specific paths in order of specificity, starting with the most specific. The load message also includes the AssetBundle path for debugging.
- `ImageAssert` failure messages now include the root mean square error (RMSE) value.

## Additional resources

- [Upgrade to Graphics Test Framework 9.0](../upgrade-guides/8-to-9.md)
- [Get started](../get-started.md)
- [Sample projects](../sample-projects.md)
