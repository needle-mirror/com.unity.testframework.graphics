# Run code before a test

Add setup logic that runs before the graphics test build process. For example, you can use pre-build steps to validate scenes, configure project settings, or prepare assets.

**Note:** To ship assets with a test player, declare them with the [`[RequireTestData]`](require-test-data.md) attribute instead of building AssetBundles in a pre-build step. The framework then builds and loads them for you on every platform.

## Bake lighting before a test

To bake lighting before a test, add the `[TestFixture]` and [`[BakeLighting]`](xref:UnityEngine.TestTools.Graphics.BakeLightingAttribute) attribute to your test class with the paths of the scenes to bake:

For example:

```csharp
[TestFixture]
[BakeLighting("Assets/Scenes/TestScene.unity", order: 1)]
public class LightingTests
{
    // Tests that require baked lighting
}
```

The order parameter determines when the lighting bake occurs relative to other pre-build steps. Unity executes tests with attributes in ascending order. You can use negative numbers.

## Add your own code before a test

To add your own code before a test run, create a custom attribute that extends [`GraphicsPrebuildSetupAttribute`](xref:UnityEngine.TestTools.Graphics.GraphicsPrebuildSetupAttribute). The code runs before Unity collects the reference images and registers AssetBundles.

Follow these steps:

1. Create a class that extends `GraphicsPrebuildSetupAttribute`.

2. Implement the `Setup` method with your code.

3. In your test class, add the name of your attribute as an attribute above the class declaration, along with `[TestFixture]`.

    For example:

    ```csharp
    [TestFixture]
    [MyAttribute(order: 0)]
    ```

    The `order` attribute determines the order. You can use negative numbers. You can also set the order in the constructor, for example `public ValidateScenesAttribute(int order = 0) : base(order) { }`

**Note:** If you use Unity Editor APIs in the code, make sure to wrap the code in a `#if UNITY_EDITOR` macro to ensure the build compiles successfully.

### Example

The following example creates an attribute that validates scenes exist.

```csharp
public class ValidateScenesAttribute : GraphicsPrebuildSetupAttribute
{
    public ValidateScenesAttribute(int order = 0) : base(order) { }

    public override void Setup()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (!File.Exists(scene.path))
                Debug.LogError($"Scene missing: {scene.path}");
        }
    }
}
```

To use the attribute from the example, add the following above a test method:

```csharp
[TestFixture]
[ValidateScenes(order: 0)]
```

## Additional resources

- [Customizing and optimizing tests](build-customization-landing.md)
- [Write a scene test](../test-authoring/writing-graphics-tests.md)