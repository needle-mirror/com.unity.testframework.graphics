# Get started

Add two example graphics tests to your project using the **Graphics Tests** window.

## Create example tests

1. [Install the Graphics Test Framework package](install-and-upgrade.md).

2. From the main menu, select **Window** > **General** > **Graphics Tests** to open the **Graphics Tests** window.

3. Select **Create Graphics Tests**.

    **Note**: The button is only available if there are no existing graphics tests in the project. To create a test manually, refer to [Writing graphics tests](test-authoring/test-authoring-landing.md).

Unity creates the following in an `Assets/Tests/GraphicsTests` folder in the **Project** window:

- A test assembly `.asmdef` file that contains the required references to the Unity Test Framework and the Graphics Test Framework.
- A script with two example tests.
- An empty `Scenes` folder where you can add scenes to test. 

## Try the example tests

For more information, refer to [Graphics test workflow](./test-authoring/graphics-tests-introduction.md).

### Try the example rendering code test

To try the rendering code test, which is the test in the script that has a `[GraphicsTest]` attribute, follow these steps:

1. From the main menu, select **Window** > **General** > **Test Runner** to open the **Test Runner** window.
2. Select **Play Mode**.
3. Select **Run All** to run the test. 

The script renders a red pixel texture and saves it in the `Assets\ActualImages` folder, but the test fails because there's no reference image to compare it to.

To create a reference image then rerun the test, follow these steps:

1. Open the **Graphics Tests** window.
2. Select **SampleCodeBasedTest**.
3. Select the tab that has your platform in its name, for example **linear-windowseditor-direct3d12-none**.
4. In the comparison window, move the slider to the left to display the image the test saved.
5. Select **Accept Result** to copy the image as a reference image. Unity saves the reference image in the `Assets/ReferenceImages` folder.
6. Rerun the test in the **Test Runner** window. 

The test now passes because the red pixel the test renders matches the reference image.

## Try the example scene test

To try the scene test, which is the test in the script that has a `[SceneGraphicsTest]` attribute, follow these steps:

1. In the **Project** window, drag a scene asset into the `Tests/GraphicsTests/Scenes` folder.
2. From the main menu, select **Window** > **General** > **Test Runner** to open the **Test Runner** window.
3. Select **Play Mode**.
4. Select **Run All** to run the test. 

The script captures the camera view and saves it as a texture in the `Assets/ActualImages` folder, but the test fails because there's no reference image to compare it to.

To create a reference image then rerun the test, follow these steps:

1. Open the **Graphics Tests** window.
2. Select **SampleSceneBasedTest**.
3. Select the tab that has your platform in its name, for example **linear-windowseditor-direct3d12-none**.
4. In the comparison window, move the slider to the left to display the image the test saved.
5. Select **Accept Result** to copy the image as a reference image. Unity saves the reference image in the `Assets/ReferenceImages` folder.
6. Rerun the test in the **Test Runner** window. 

The test now passes because the camera view matches the reference image.

## Additional resources

- [Install the Graphics Test Framework package](install-and-upgrade.md)
- [Graphics test workflow](test-authoring/graphics-tests-introduction.md)
- [Sample projects](sample-projects.md)
- [Unity Test Framework learning materials](https://docs.unity3d.com/Manual/test-framework/course/overview.html)

