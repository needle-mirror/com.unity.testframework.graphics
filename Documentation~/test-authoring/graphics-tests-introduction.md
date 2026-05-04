# Graphics test workflow

Follow this workflow to write a test that compares a texture or a capture of your scene to a reference image, and fails if the images are different.

Follow these steps:

1. Create a test assembly file.
2. Write a test that captures an image of the scene or a texture you render into, then compares it against a reference image.
3. Run the test for the first time to create a reference image for future tests.
4. Run the test and check the results.

**Note:** If you haven't yet created a graphics test, you can create and try two simple example tests from the **Graphics Tests** window. For more information, refer to [Get started](../get-started.md). 

## Create a test assembly

Create a test assembly `.asmdef` file in your project, and include the following assemblies in the `references` section:

- `UnityEngine.TestTools.Graphics`
- `UnityEditor.TestTools.Graphics`

For more information, refer to [Create a test assembly](https://docs.unity3d.com/6000.5/Documentation/Manual/test-framework/workflow-create-test-assembly.html).

To create an example assembly file in your project, refer to [Get started](../get-started.md).

## Write a test

Write a test to do any of the following:

- [Write a scene test](writing-graphics-tests.md). Load a scene and capture a camera view, then test it against a reference image.
- [Write a rendering code test](writing-graphics-tests-texture.md). Render into a texture, then test it against a reference image.
- [Write a Unity Editor window test](compare-editor-window-captures.md). Capture a Unity Editor window, then test it against a reference image.
- [Write a shader test](writing-shader-tests.md). Test a ShaderLab shader or an HLSL method.

## Run the test for the first time

To run the test for the first time, follow these steps:

1. Use the **Test Runner** window to run the test.

    The first time you run a test, it fails because there's no reference image to check the capture against yet.

2. Use the **Graphics Tests** window to check the reference image represents the correct output, then make it the reference image for future tests.

For more information, refer to [Run a test for the first time](running-graphics-tests-first-time.md).

## Run the test and check the results

Run the test again using the **Test Runner** window, then check the test results in the **Graphics Tests** window. If the test image and the reference image are the same, the test passes.

For more information, refer to [Run a test and check the results](running-graphics-tests.md).

## Additional resources

- [Write a scene test](writing-graphics-tests.md)
- [Write a shader test](writing-shader-tests.md)
- [Unity Test Framework learning materials](https://docs.unity3d.com/Manual/test-framework/course/overview.html)
- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)
