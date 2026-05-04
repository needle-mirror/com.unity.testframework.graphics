# Customize tests

Customize how a test captures and compares images.

## Customize capture and comparison settings

To customize how a test captures and compares images, use either of the following:

- The **Settings** tab of the **Graphics Tests** window to configure all tests. Unity saves the settings in a [Graphics Test Build Settings asset](../test-investigation/graphics-test-build-settings-inspector.md) in your project.
- A Graphics Test Settings component to configure a specific scene.

To add a Graphics Test Settings component, follow these steps:

1. Select a GameObject in the **Hierarchy** window.
2. In the **Inspector** window, select **Add Component**.
3. Select **Scripts** > **UnityEngine.TestTools.GraphicsTestFramework** > **Graphics Test Settings**.

For more information about the settings, refer to the [Graphics Test Settings component reference](../test-investigation/graphics-test-settings-component.md).

### Change the capture size

To make captures a consistent size, use either of the following:

- [`GameViewSize.SetGameViewSize`](xref:UnityEditor.TestTools.Graphics.GameViewSize.SetGameViewSize(System.Int32,System.Int32)) to set the Game view to a specific resolution.
- [`GlobalResolutionSetter.SetResolution`](xref:UnityEngine.TestTools.Graphics.GlobalResolutionSetter.SetResolution(System.Int32,System.Int32)) to set the device resolution for platform tests.

## Change the comparison algorithm

To change the comparison algorithm Unity uses, create an algorithm instance and pass it into your `Assert` statement.

Unity has the following built-in algorithm types:

- [`LegacyColorDifferenceAlgorithm`](xref:UnityEngine.TestTools.Graphics.LegacyColorDifference.LegacyColorDifferenceAlgorithm): Reflects how humans see color. This algorithm provides a balance between accuracy and performance, and you can use it for most tests. This is the default.
- [`PeakSignalToNoiseRatio`](xref:UnityEngine.TestTools.Graphics.PeakSignalToNoiseRatio) (PSNR): Measures signal power against noise power in decibels.  
- [`StructuralSimilarity`](xref:UnityEngine.TestTools.Graphics.StructuralSimilarity): Uses a similarity index measure (SSIM) from &minus;1 to 1. Compares luminance, contrast, and structure. Use this algorithm to detect invisible differences in patterns and compression.
- [`EuclideanDistance`](xref:UnityEngine.TestTools.Graphics.EuclideanDistance): Calculates the difference as a root mean square error. This algorithm is fast but sensitive, and might not align well with how humans see color.

To change the algorithm, follow these steps:

1. Create an instance of one of the built-in algorithm types. For example, to use PSNR:

    ```csharp
    PeakSignalToNoiseRatio algorithm = new PeakSignalToNoiseRatio(new PeakSignalToNoiseRatioSettings(30f));
    ```

2. Pass the algorithm instance into your `Assert` statement with an [`IsTexture.EqualTo`](xref:UnityEngine.TestTools.Graphics.IsTexture.EqualTo(UnityEngine.Texture2D)) method and a `Using` modifier. For example:

    ```csharp
    Assert.That(actualTexture, IsTexture.EqualTo(expectedTexture).Using(algorithm));
    ```

    **Note**: If you use [`ImageAssert.AreEqual`](xref:UnityEngine.TestTools.Graphics.ImageAssert.AreEqual(UnityEngine.Texture2D,UnityEngine.Camera,UnityEngine.TestTools.Graphics.ImageComparisonSettings,System.String,System.Boolean)), you can only configure the default algorithm in an [`ImageComparisonSettings`](xref:UnityEngine.TestTools.Graphics.ImageComparisonSettings) object. You can't use a different algorithm.

To implement a custom algorithm instead, refer to [`TextureComparisonAlgorithm`](xref:UnityEngine.TestTools.Graphics.TextureComparisonAlgorithm). 

## Parameterize tests

To parameterize tests using the [`GraphicsTestParam`](xref:UnityEngine.TestTools.Graphics.GraphicsTestParamAttribute) attribute, follow these steps:

1. Add the `[GraphicsTestParam]` attribute to your test method, and specify the parameter value and an optional name for the test case. For example:

    ```csharp
    [GraphicsTestParam(1.0f, TestName = "HighQuality")]
    ```

2. Pass the parameter as an argument to your test method:

    ```csharp
    public void QualityTest(GraphicsTestCase testCase, float quality)
    ```

To pass multiple parameters, create a class that implements `IEnumerable<object[]>`, then use the [`[GraphicsTestParamSource]`](xref:UnityEngine.TestTools.Graphics.GraphicsTestParamSourceAttribute) attribute instead of `[GraphicsTestParam]`. For example:

```csharp
[Test, GraphicsTest]
[GraphicsTestParamSource(typeof(MyTestData))]
public void DataDrivenTest(GraphicsTestCase testCase, float threshold)
{
    // Parameters provided by MyTestData
}
```

You can also use the `[Values]` or `[ValueSource]` parameters from the Unity Test Framework API. For more information, refer to [Parameterized tests](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-tests-parameterized.html) in the Unity Test Framework package documentation.

## Customize reference image naming

To control how Unity names reference images, use the `ReferenceImageRootSource` and `ReferenceImageNamingStrategyType` parameters on the `[GraphicsTest]` or `[SceneGraphicsTest]` attributes.

### Use the scene name for reference images

By default, Unity uses the full parameterized test name for reference images. For scene tests with multiple scenes, this creates one reference image per scene with the parameter appended to the name.

To use only the scene name instead, set `ReferenceImageRootSource` to `SceneAssetFileStem`. For example:

```csharp
[UnityTest, SceneGraphicsTest("Assets/Scenes", ReferenceImageRootSource = ReferenceImageRootSource.SceneAssetFileStem)]
public IEnumerator RenderScene(SceneGraphicsTestCase testCase, [Values(1, 2)] int quality)
{
    SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
    yield return null;

    Camera camera = Object.FindFirstObjectByType<Camera>();
    ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera);
}
```

With `SceneAssetFileStem`, Unity names the reference image after the scene file. For example, if the scene file is `MyScene.unity`, the reference image is `MyScene.png` regardless of the `quality` parameter value.

With the default `ParameterizedTestName`, Unity includes the parameter in the name. For example, `MyScene_1.png` and `MyScene_2.png`.

**Note**: If you use `SceneAssetFileStem` with a `[GraphicsTest]` attribute instead of `[SceneGraphicsTest]`, Unity falls back to `ParameterizedTestName`.

### Implement a custom naming strategy

To implement a custom naming strategy, create a class that implements [`IReferenceImageNamingStrategy`](xref:UnityEngine.TestTools.Graphics.IReferenceImageNamingStrategy), then reference the class in the `ReferenceImageNamingStrategyType` parameter.

Follow these steps:

1. Create a class that implements `IReferenceImageNamingStrategy` and implements the `CreateDescriptor` method. For example:

    ```csharp
    using UnityEngine.TestTools.Graphics;

    public class CustomNamingStrategy : IReferenceImageNamingStrategy
    {
        public IReferenceImageFileDescriptor CreateDescriptor(
            GraphicsTestCase rawCase,
            string parameterizedTestName,
            ImageExtension extension,
            TextureFormat format)
        {
            return new ReferenceImageFileDescriptor(
                $"Custom_{parameterizedTestName}",
                extension,
                format);
        }
    }
    ```

2. Add the `ReferenceImageNamingStrategyType` parameter to the test attribute with your class type. For example:

    ```csharp
    [Test, GraphicsTest(ReferenceImageNamingStrategyType = typeof(CustomNamingStrategy))]
    public void MyTest(GraphicsTestCase testCase)
    {
        // Test code here
    }
    ```

Unity calls `CreateDescriptor` to generate the reference image file name. The method receives the test case, parameterized test name, image extension, and texture format. Return a [`ReferenceImageFileDescriptor`](xref:UnityEngine.TestTools.Graphics.ReferenceImageFileDescriptor) with the custom file name.

If the strategy type is invalid or doesn't implement `IReferenceImageNamingStrategy`, Unity falls back to the default `ParameterizedTestName` behavior.

## Additional resources

- [Graphics test workflow](../test-authoring/graphics-tests-introduction.md)
- [Graphics Test Settings component reference](../test-investigation/graphics-test-settings-component.md)
- [Strip shader variants from test builds](../performance-optimization/graphics-test-shader-stripping.md)
