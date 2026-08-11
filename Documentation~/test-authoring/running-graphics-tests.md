# Run a test and check the results

After you [run a test for the first time](running-graphics-tests-first-time.md) to create a reference image, run the test again when you want to check for changes.

**Note:** To run a test with a C# script instead, refer to the [GraphicsTestBuildManager](xref:UnityEditor.TestTools.Graphics.Builder.GraphicsTestBuildManager) and [GraphicsTestBuilder](xref:UnityEditor.TestTools.Graphics.Builder.GraphicsTestBuilder) API documentation.

## Run the test

To run the test, follow these steps:

1. From the main menu, select **Window** > **General** > **Test Runner** to open the **Test Runner** window.
2. Select your test.
3. Select **Run Selected**.

Unity generates the following in the **Project** window:

- The captured image from the test run. The file name ends in `Test`.
- The reference image the test used as a comparison, if the test fails. The file name ends in `.expected`
- An image with the difference between the two images, if the test fails. The file name ends in `.diff`.

Unity also generates an XML test report called `TestResults.xml` in the user folder of your computer.

For more information, refer to [Run tests in the Test Runner window](https://docs.unity3d.com/Manual/test-framework/workflow-run-test.html).

**Note:** If you run a test in **Edit Mode** or **Play Mode**, Unity uses assets directly from the **Project** window. If you run a **Player** test, Unity adds assets to an [AssetBundle](https://docs.unity3d.com/Manual/AssetBundlesIntro.html) for runtime code to load.

## Compare the images

To compare the images, follow these steps:

1. From the main menu, select **Window** > **General** > **Graphics Tests** to open the Graphics Tests window.
2. Select your test.
3. Select the correct tab. For example, if Unity saved the captured image in `Assets/ActualImages/Linear/WindowsEditor/Direct3D12/None`, select the **linear-windowseditor-direct3d12-none** tab.
4. Move the slider to compare the captured image with the reference image.

For more information, refer to [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md).

## Check why the test failed

Check the reasons why a test failed.

To make tests less sensitive to differences in the images, refer to [Change the comparison settings](../test-investigation/graphics-test-settings-component.md). 

### Check in your C# script

To get the result of the test, add an `ImageComparisonResults` out parameter when you call the [`ImageAssert`](xref:UnityEngine.TestTools.Graphics.ImageAssert) API. For example:

```csharp
ImageAssert.AreEqual(
    testCase.ReferenceImage.Image,
    actualTexture,
    settings,
    expectedImagePathLog: testCase.ReferenceImage.LoadMessage,
    saveFailedImage: true,
    saveFailedImageToDisk: false,
    logMessages: true,
    result: out ImageComparisonResults result
);
```

### Check the logs

The Graphics Test Framework adds logs in the following places:

- The **Logs** tab in the **Graphics Tests** window.
- The Console window.
- A `GraphicsTestLogs.log` file in the `Logs` folder.

To add your own logs, use the [`GraphicsTestLogger`](xref:UnityEngine.TestTools.Graphics.GraphicsTestLogger) API. For example:

```csharp
GraphicsTestLogger.Log("Camera clear flags: " + camera.clearFlags);
GraphicsTestLogger.Log("Render pipeline: " + GraphicsSettings.currentRenderPipeline?.name);
```

## Additional resources

- [Running tests](https://docs.unity3d.com/Manual/test-framework/running-tests.html)
- [Write a scene test](../test-authoring/writing-graphics-tests.md)
- [Test specific platforms](get-platform-information.md)
- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)
- [Deduplicate reference images](../performance-optimization/reference-image-optimization.md)
