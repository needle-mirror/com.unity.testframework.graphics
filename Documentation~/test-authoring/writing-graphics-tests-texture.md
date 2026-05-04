# Write a rendering code test

Write a graphics test where you render into a texture, then compare the texture to a reference image.

Follow these steps:

1. Create a script that includes the `NUnit.Framework` and `UnityEngine.TestTools.Graphics` namespaces.

2. Create a class with a method that has the `[Test]` and [`[GraphicsTest]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestAttribute) attributes. The method must accept a [`GraphicsTestCase`](xref:UnityEngine.TestTools.Graphics.GraphicsTestCase) parameter, which the test framework automatically inputs into the test method.

    For example:

    ```csharp
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine.TestTools.Graphics;
    using UnityEngine;

    class MyGraphicsTest
    {
        [Test, GraphicsTest]
        public void MyTestMethod(GraphicsTestCase testCase)
        {
        }
    }
    ```

    By default, Unity names reference images using the full parameterized test name. To customize this behavior, refer to [Customize reference image naming](../build-customization/customize-a-test.md#customize-reference-image-naming).

3. Inside the method, render what you want to test. For example, the following code creates a 1 &times; 1 pixel red texture.

    ```csharp
    public void MyTestMethod(GraphicsTestCase testCase)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
        texture.SetPixel(0, 0, Color.red);
        texture.Apply();
    }
    ```

4. Compare the rendered texture to the reference image in the `testCase` using the [`ImageAssert`](xref:UnityEngine.TestTools.Graphics.ImageAssert) API. For example:

    ```csharp
    ImageAssert.AreEqual(testCase.ReferenceImage.Image, texture);
    ```

The test always fails the first time you run it because there's no reference image yet. For more information, refer to [Run a graphics test for the first time](running-graphics-tests-first-time.md).

## Get a copy of a camera texture

To get a copy of the camera view or the back buffer as a texture, use the [`ImageCapture`](xref:UnityEngine.TestTools.Graphics.ImageCapture) API.

To get a texture copy of the back buffer, use the [`ImageCapture.CaptureBackbuffer`](xref:UnityEngine.TestTools.Graphics.ImageCapture.CaptureBackbuffer(UnityEngine.TextureFormat)) method. For example:

```csharp
foreach (Texture2D frame in ImageCapture.CaptureBackbuffer(TextureFormat.RGB24))
{
    ImageAssert.AreEqual(testCase.ReferenceImage.Image, frame);
}
```

To get a texture copy of a camera view, use the [`ImageCapture.CaptureFromCamera`](xref:UnityEngine.TestTools.Graphics.ImageCapture.CaptureFromCamera(UnityEngine.Camera,UnityEngine.TextureFormat,UnityEngine.TestTools.Graphics.CameraCaptureSettings,System.Int32)) method. For example:

```csharp
CameraCaptureSettings settings = new CameraCaptureSettings
{
    targetWidth = 1920,
    targetHeight = 1080,
    useHDR = false,
    msaaSamples = 1
};

Texture2D captured = ImageCapture.CaptureFromCamera(camera, TextureFormat.RGB24, settings).First();
```

To save the image, use the [`ImageCapture.SaveAsActual`](xref:UnityEngine.TestTools.Graphics.ImageCapture.SaveAsActual(UnityEngine.Texture2D,System.String,System.Boolean,System.Boolean)) method. For example:

```csharp
ImageCapture.SaveAsActual(captured, "MyTest_Actual");
```

## Example

```csharp
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEngine;

class MyGraphicsTest
{
    [Test, GraphicsTest]
    public void RenderRedPixel(GraphicsTestCase testCase)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
        texture.SetPixel(0, 0, Color.red);
        texture.Apply();

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, texture);
    }
}
```

## Additional resources

- [Write a scene test](writing-graphics-tests.md)
- [Graphics test workflow](graphics-tests-introduction.md)
- [Run a graphics test for the first time](running-graphics-tests-first-time.md)
