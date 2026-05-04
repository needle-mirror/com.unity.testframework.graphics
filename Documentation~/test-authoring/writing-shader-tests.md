# Write a shader test

To test shaders without rendering a full scene, create tests that check [ShaderLab shader objects](https://docs.unity3d.com/Manual/SL-Reference.html) or [HLSL shader functions](https://docs.unity3d.com/Manual/SL-ShaderPrograms.html).

**Note**: You can't test shader graphs, compute shaders, CG shaders, or ray tracing shaders.

For more information about using a test after you write it, refer to [Run a graphics test for the first time](running-graphics-tests-first-time.md) and [Run a test and check the results](running-graphics-tests.md).

## Write a ShaderLab test

To write a ShaderLab test, follow these steps:

1. Create an Editor-only test assembly that references `UnityEditor.TestTools.Graphics`.

2. Add the shader you want to test to the `Assets` folder of the **Project** window.

3. Create a [`ShaderTestFramework`](xref:UnityEngine.TestTools.Graphics.Shaders.ShaderTestFramework) instance. The recommended approach is to create and dispose of it in `[OneTimeSetUp]` and `[OneTimeTearDown]` methods. Refer to the [HLSL section](#run-code-before-or-after-the-test) for an example.

4. Create a C# method with a `[Test]` attribute.

5. In the method, create a [`ShaderlabShaderParams`](xref:UnityEngine.TestTools.Graphics.Shaders.ShaderlabShaderParams) object. You don't need to pass in any parameters.

    ```csharp
    ShaderlabShaderParams shaderParams = new ShaderlabShaderParams();
    ```

6. Get a handle to the shader by calling `shaderTestFramework.LoadShader` with the internal name of the shader and the parameters.

    ```csharp
    ShaderHandle shader = shaderTestFramework.LoadShader("SampleTest/ShaderlabShader.shader", shaderParams);
    ```

7. Call `ExecuteShader` with the shader handle.

    ```csharp
    ShaderlabShaderData result = shaderTestFramework.ExecuteShader<ShaderlabShaderData>(shader);
    ```

8. Use the `Vertex` and `Fragment` properties of the returned [`ShaderlabShaderData`](xref:UnityEngine.TestTools.Graphics.Shaders.ShaderlabShaderData) in your `Assert` statement. For example:

    ```csharp
    Assert.That(result.Fragment, Is.Not.Null);
    ```

    `Vertex` is a Vector3 array of the vertex output positions. `Fragment` is the fragment output as a texture.

## Write an HLSL method test

Unity automatically generates wrapper shaders for HLSL methods, so you can test the method without a surrounding ShaderLab shader.

To write an HLSL method test, follow these steps:

1. Create an Editor-only test assembly that references `UnityEditor.TestTools.Graphics`.

2. Add the HLSL file you want to test to the `Assets` folder of the **Project** window.

3. Create a [`ShaderTestFramework`](xref:UnityEngine.TestTools.Graphics.Shaders.ShaderTestFramework) instance. 

    ```csharp
    ShaderTestFramework shaderTestFramework;
    ```

4. Create a test method with a `[Test]` attribute. Inside the method, create an [`HlslShaderParams`](xref:UnityEngine.TestTools.Graphics.Shaders.HlslShaderParams) object with the HLSL function you want to call and the expected C# return type. For example:

    ```csharp
    [Test]
    public void FloatFunction_ReturnsExpected()
    {
        HlslShaderParams shaderParams = new HlslShaderParams("SampleFloatFunction()", typeof(float));
    }
    ```

    For vector return types, use `Vector` or `Matrix`. For example, use `Vector3` for an HLSL `float3` or `int4`, or `Matrix4x4` for an HLSL `float4x4`.

5. Call `LoadShader` with the path to your `.hlsl` file and the shader parameters.

    ```csharp
    ShaderHandle shader = shaderTestFramework.LoadShader("Assets/Shaders/SampleTestShader.hlsl", shaderParams);
    ```

6. Call `ExecuteShader<T>`. `T` must match the return type of the shader.

    ```csharp
    float result = shaderTestFramework.ExecuteShader<float>(shader);
    ```

    For vector return types, use `Vector` or `Matrix`. For example, use `Vector3` for an HLSL `float3` or `int4`, or `Matrix4x4` for an HLSL `float4x4`.

7. Check the result with an `Assert` statement. For example:

    ```csharp
    Assert.That(result, Is.EqualTo(1.0f));
    ```

### Run code before or after the test

The recommended best practice is to do the following:

- Create a `OneTimeSetup` method to initialize variables or state that you share across tests, such as a `ShaderTestFramework` instance.
- Create a `OneTimeTearDown` method to clean up any shared resources.

For example:

```csharp
[OneTimeSetUp]
public void OneTimeSetUp()
{
    shaderTestFramework = new ShaderTestFramework();
}

[OneTimeTearDown]
public void OneTimeTearDown()
{
    shaderTestFramework.Dispose();
}
```

For more information, refer to [Setting up and tearing down tests](https://docs.unity3d.com/Manual/test-framework/reference-unitysetup-and-unityteardown.html).

## Add parameters to shader tests

To parameterize a shader test, use the `[TestCase]` attribute with literal parameters in the function call string. For example:

```csharp
[Test]
[TestCase("SampleIntAdd(1, 2)", 3)]
[TestCase("SampleIntAdd(0, 0)", 0)]
[TestCase("SampleIntAdd(-1, 1)", 0)]
public void IntAdd_ReturnsExpected(string functionCall, int expected)
{
    ...
}
```

For more information, refer to [Test cases](https://docs.unity3d.com/6000.5/Documentation/Manual/test-framework/course/test-cases.html).

## Additional resources

- [Graphics test workflow](graphics-tests-introduction.md)
- [Write a scene test](writing-graphics-tests.md)
- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)
