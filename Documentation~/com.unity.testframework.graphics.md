# About the Graphics Test Framework

Use the Graphics Test Framework package to create automated tests for rendering outputs - tests that render an image and compare it to a 'known good' reference image. The package uses features from the [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html) and [NUnit](https://docs.nunit.org/).

# Installing the Graphics Test Framework

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html).

<a name="UsingPackageName"></a>
# Using the Graphics Test Framework

Before you can use the Graphics Test Framework in your project, you need to add the Engine and Editor TestTools.Graphics assembly definitions to the test assembly (asmdef) in your project.
Alternatively, you can add com.unity.testframework.graphics to the testables section of your manifest, but that will add the Graphics Test Framework tests to your test runner window.

There are two main components to the framework:

## ImageAssert.AreEqual()

This is a new assertion method that you can use in your tests to check two images for equality. There is also an overload that takes a camera instead of an image, and automatically captures the camera output for you and compares that.

An optional third parameter allows you to configure the sensitivity of the check. Even when everything else is the same, it's common for there to be small differences between images due to changes in hardware, driver version, and so on. You can use this third parameter to set a threshold for how different pixels need to be before they are counted, and you can set a threshold for how different the overall image needs to be before the assertion will fail.

## Automatic test case management

The framework can automatically generate test cases based on the scenes in the project and manage reference images for them.

Using this feature requires a little bit of setup. On your test method, you should add `[UseGraphicsTestCases]`. The class should implement `IPrebuildSetup` and call `UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup()` in the `Setup()` method. This will ensure that the test cases are generated and set up correctly.

```csharp
class MyTestClass : IPrebuildSetup
{
    public void Setup()
    {
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup();
    }

    [UnityTest]
    [UseGraphicsTestCases]
    public IEnumerator DoTest(GraphicsTestCase testCase)
    {
        // Your test code here
    }
}
```

You can also use the `[PrebuildSetup("MySetupClass")]` attribute to automatically call a setup method for you if you're using a different class to manage your test setup.

Your test method should also take a single `GraphicsTestCase` parameter. You will also usually want to use `[UnityTest]` and return `IEnumerator`, rather than using `[Test]` and returning `void`, because usually you will want to load a new scene in your test, and this requires yielding one frame for the load to complete.

With this in place, any scene added to the build systems will result in a test case for the scene being generated in the test runner.

**Important Note for Android and WebGL Platforms**

For Android and WebGL Platforms, you'll need to use a slightly different test setup pattern that uses asynchronous reference image loading. Please refer to the [Using the Graphics Test Framework with Android and WebGL](#using-the-graphics-test-framework-with-android-and-webgl) section below for instructions.

### Test Filters
Since test cases are automatically generated based on the scenes in the project build settings the usual way of filtering test cases may not work.  To filter scenes out of specific configurations create a test filter scriptable object by right clicking in the asset list and choosing Create -> Testing -> Test Filter ScriptableObject.  This will create a filter asset which you may want to place in the Editor directory as it is not used at runtime.  Select this newly created object in the inspector then add a scene in the scene asset field to exclude from the configuration specified.  If multiple scenes need to be excluded in the same configuration you can add them to the filter by incrementing the number next to the scene field and adding additional scenes to the fields that appear.  Remove scenes by decrementing the number, this will remove entries at the bottom of the list.

Note that if options are left blank or otherwise unspecified it will indicate all values.  For example, if color space is set to Unititialized the scene will be removed from the test case list when running in either linear or gamma colorspace.  To indicate a test filter should only be applied to any vr configuration, leave the XR SDK field blank but check all stereo rendering modes.  Also note the using None in the XR SDK will indicate to exclude the case only from non-vr modes (if the stereo modes are not checked).  See the tooltips in the column headers for more details on this.

To add more filters just click the Add Filter button at the bottom.  You can have as many filters per scene as you need but will need to create one for each configuration you want.  However, you should only have one test filter scriptable object in the project as the filters are applied from only the first scriptable object found in the assets.

### Base Reference images (new approach)

The Graphics Test Framework has a new way of using the reference images. The `ReferenceImagesBase` folder stores the project's reference images that are similar accross platforms and graphics APIs. When a reference image for a test is required, first it tries to load it from its specific location in the `ReferenceImages/ColorSpace/Platform/GraphicsAPI` folder. If it doesn’t exist, it is loaded from the `ReferenceImagesBase` folder. Thus, specific reference images are preferred over the base reference images.

The preferred way of using it is the following one. If you want to use the same reference image across multiple graphics APIs and platforms, once you have created your tests and added them to the Build Settings, add the images for the tests directly to the `ReferenceImagesBase` folder. Then you can either run the tests in-Editor to generate the images for the tests that differ, using the Editor renderer, or you can exit Unity and run the tests on-device from the commandline.

When the run completes, you should be able to obtain a TestResults.xml file for the run. As well as reporting what tests failed (uses a different image), the TestResults.xml file will also contain encoded versions of the rendered images.

In Unity, go to `Tests -> Extract images from TestResults.xml...` and select the TestResults.xml file created by the run. The framework will process the images in the test results and put them into a folder called "ActualImages" in the root of your Assets folder, where you can inspect them to make sure they look correct.

Once the generated images look correct, merge the images into the ReferenceImages folder to the specific `ColorSpace/Platform/GraphicsAPI`. Run the tests again and you should see that they now use the reference images successfully!

#### Optimize your existing project
    1. Open the target project in the Unity Editor
    2. In the “Window” tab, “General” tag, open “Graphics Test Setup” window
    3. In the new window press “Start Optimization” and follow the progress bar for updates on the optimization.
    4. At the end of the optimization, the right top corner displays the total of images optimized.
    5. The optimized reference images can be found in the ReferenceImagesBase folder.

### Reference images

The simplest way to set up your initial reference images is to allow the tests to generate them. Once you have created your tests and added them to the Build Settings, you can either run the tests in-Editor to generate images using the Editor renderer, or you can exit Unity and run the tests on-device from the commandline.

When the run completes, you should be able to obtain a TestResults.xml file for the run. As well as reporting that the tests failed (because they have no reference images), the TestResults.xml file will also contain encoded versions of the rendered images.

In Unity, go to `Tests -> Extract images from TestResults.xml...` and select the TestResults.xml file created by the run. The framework will process the images in the test results and put them into a folder called "ActualImages" in the root of your Assets folder, where you can inspect them to make sure they look correct.

Once you're happy that the images look correct, you should rename the `ActualImages` folder to `ReferenceImages` (or merge the images into the existing ReferenceImages folder, if there is one). Run the tests again and you should see that they now use the reference images successfully!

By default, reference images will be set up in a three-level hierarchy of folders: `ColorSpace/Platform/GraphicsAPI`. If you want to use the same reference image across multiple graphics APIs, you can put it directly into the `ColorSpace/Platform` folder.

## Capturing Editor Windows

To capture from the scene view instead of the game view, use:

```csharp
Texture2D capturedTexture = await EditorWindowCapture.CaptureAsync(customWindow, EditorWindowCaptureSettings.Default);
```

in an `async` test. This will capture the scene view from the specified window.

The `EditorWindowCaptureSettings` class allows you to specify the resolution of the capture, what timeout to use for certain waiting operations (like async shader compilation). The default settings are 512x512 resolution and a 10-second timeout for async tasks when targeting a `SceneView`.

When testing the SceneView window specifically, you can also set the Image Comparison Viewpoint to set which view to capture from. If you wish to execute additional actions before the capture, you can add a list of `Action<EditorWindow>` to the `EditorWindowCaptureSettings.AdditionalSetupActions`.

By extending the `EditorWindowCaptureSettings` class, you can create custom settings for your tests and pass them to the `CaptureAsync` method.

## Code-Based Graphics Tests
To use the Graphics Test Framework using code-based tests instead of scene-based tests, use the `[CodeBasedGraphicsTest]` attribute. You must specify the reference image directory and optionally specify the actual image directory. Here's an example:
```CSharp
[UnityTest, Category("Graphics")]
[CodeBasedGraphicsTest("Assets/ReferenceImages", "Assets/ActualImages")]
public IEnumerator MyTest()
{
    ...
}
```

## Using the Graphics Test Framework with Android and WebGL
*For all platforms other than Android and WebGL*, the reference images used in the Graphics Test Framework are loaded synchronously from Unity asset bundles by NUnit (via a custom test case creation override) in a custom test case constructor.

However, for Android and WebGL, these reference image asset bundles need to be loaded using an asynchronous web request to avoid locking the main Unity runtime thread. The Graphics Test Framework provides async methods for the purpose, but you'll need to call them correctly in your test setup and methods.

The additional setup and test code needed to use the Graphics Test Framework on the Android and WebGL platform can be described as

1. Loading the reference image asset bundles asynchronously from a UnitySetUp method in your test class, then
2. Associating each test case with its corresponding reference image loaded in the UnitySetUp method.

Below are actual examples used in the `com.unity.test.urp` test package for this purpose. Please use them as a guide for your own tests.

### **1. Example: How to load the reference image asset bundles asynchronously from a UnitySetUp method in your test class**
```CSharp
#if UNITY_WEBGL || UNITY_ANDROID
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return RuntimeGraphicsTestCaseProvider.EnsureGetReferenceImageBundlesAsync();
    }
#endif
```

### **2. Example: How to associate each test case with its corresponding reference image loaded in the UnitySetUp method**
```CSharp
    [UnityTest, Category("UniversalRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(universalPackagePath)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
#if UNITY_WEBGL || UNITY_ANDROID
        // Do this near the beginning of the test case method before you test or assert
        RuntimeGraphicsTestCaseProvider.AssociateReferenceImageWithTest(testCase);
#endif
...
    }
```

# Technical details
## Requirements

This version of the Graphics Test Framework is compatible with the following versions of the Unity Editor:

* 2018.1 and later (recommended)

## Known limitations

Graphics Test Framework version 0.1.0 includes the following known limitations:

* Actual/Diff images cannot be retrieved from test results when running in-player tests from the Unity Test Runner interactively. The commandline must be used instead.

## Document revision history
|Date|Reason|
|---|---|
|May 10, 2018|Document created. Matches package version 0.1|
|Feb 16, 2022|Added scene view tests section. Matches package version 7.8.19-exp.1|
