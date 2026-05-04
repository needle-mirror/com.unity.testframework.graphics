# Run a graphics test for the first time

Before you can use a test in your test suite, run it once to generate a reference image that future tests compare against.

Follow these steps after you [write a test](./writing-graphics-tests.md):

1. From the main menu, select **Window** > **General** > **Test Runner** to open the **Test Runner** window.

2. Select your test, then select **Run Selected**.

    The test fails, but saves the image it captures to the `Assets/ActualImages` folder in the **Project** window.

    **Note:** By default, Unity organizes the `ActualImages` folder by color space, platform, architecture, graphics API, and XR device. To customize the folder structure, refer to [Graphics Test Build Settings Inspector window reference](../test-investigation/graphics-test-build-settings-inspector.md).

3. From the main menu, select **Window** > **General** > **Graphics Tests** to open the Graphics Tests window.

4. Select your test.

5. Select the correct tab. For example, if Unity saved the captured image in `Assets/ActualImages/Linear/WindowsEditor/Direct3D12/None`, select the **linear-windowseditor-direct3d12-none** tab.

6. Move the slider to the left to display the captured image.

7. Check the captured image is correct, then select **Accept Result**.

Unity copies the capture image to the `Assets/ReferenceImages` folder in the **Project** window, and future test runs compare their capture against it to determine if the test passes or fails.

You can also move the image manually from the `ActualImages` folder to the root of the `ReferenceImages` folder instead. You don't need to create any subfolders.

## Additional resources

- [Run a test and check the results](running-graphics-tests.md)
- [Write a scene test](writing-graphics-tests.md)
- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)



