# Bundle test data with a test

Declare the assets a test needs at run time with the [`[RequireTestData]`](xref:UnityEngine.TestTools.Graphics.RequireTestDataAttribute) attribute, and load them through [`GraphicsTestCase.TestData`](xref:UnityEngine.TestTools.Graphics.GraphicsTestCase.TestData). The framework builds the declared assets into content bundles for player builds and loads them the same way on every platform, so tests don't need their own AssetBundle handling or Editor-only code paths.

## Declare test data

Add the `[RequireTestData]` attribute to a test fixture or test method with the assets it needs. Paths support `*` wildcards over one directory:

```csharp
[TestFixture]
[RequireTestData("ssao-testdata",
    "Assets/Scenes/500_SSAO/depth.exr",
    "Assets/Scenes/500_SSAO/*.json")]
public class SSAOShaderTestCases
{
    // ...
}
```

The first argument is always the logical bundle name; the remaining arguments are the asset paths. Declarations that share a bundle name, including declarations on different fixtures, are merged into one bundle. Pass `null` as the bundle name to derive it from the name of the type that declares the attribute.

You can apply the attribute multiple times, and to both the fixture and individual test methods; a test case receives the union of the declarations that apply to it.

## Load test data in a test

Load the declared assets through the `TestData` property of the test's `GraphicsTestCase` parameter:

```csharp
[GraphicsTest]
public IEnumerator MyTest(GraphicsTestCase testCase)
{
    Texture2D depth = testCase.TestData.Load<Texture2D>("depth");
    TextAsset metadata = testCase.TestData.Load<TextAsset>("Assets/Scenes/500_SSAO/settings.json");
    // ...
}
```

`Load<T>` accepts a full asset path, a file name, or a file name without extension. Use the full asset path to disambiguate assets that share a file name: a short name that matches several of them resolves to one of the two arbitrarily, and only the Editor warns about it.

In the Editor, assets load from their declared project paths through the AssetDatabase. In players they load from the built content bundles, including on platforms that stream StreamingAssets remotely (such as Android and WebGL). Before the first access in a player, wait for the content to load in a `[UnitySetUp]` method, exactly as for reference images:

```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    yield return TestContentLoader.WaitForContentLoadAsync(TimeSpan.FromSeconds(240));
}
```

To read a source file directly in the Editor (for example, to decode a texture without importer processing), use `GetAssetPath` to get the project-relative path of a declared asset.

## Missing test data fails

Tests must always have access to their declared assets, so missing test data is an error:

- At build time, a declared path that doesn't exist, a wildcard that matches no files, or a bundle that ends up empty fails the build with a message naming the declaration.
- At run time, `Load<T>` and `GetAssetPath` throw [`TestDataNotFoundException`](xref:UnityEngine.TestTools.Graphics.TestDataNotFoundException) when the asset is missing or its bundle failed to load. The message states what was declared, what was built, and what was searched.

For optional assets, use `TryLoad<T>`, which returns `false` instead of throwing.

## Customize the pipeline

Each stage of the test data pipeline is extensible:

- **Asset enumeration and addressing**: Override [`RequireTestDataAttribute.CreateDescriptor`](xref:UnityEngine.TestTools.Graphics.RequireTestDataAttribute.CreateDescriptor(System.Type)) to return a custom [`ITestDataDescriptor`](xref:UnityEngine.TestTools.Graphics.ITestDataDescriptor). Subclass [`TestDataDescriptor`](xref:UnityEngine.TestTools.Graphics.TestDataDescriptor) and override `GetAssetPaths` to collect assets programmatically, or `GetAddressableName` to define custom load names.
- **Content building**: Register a custom `IPlayerContentBuilder` with [`PlayerContentBuilders.Register`](xref:UnityEditor.TestTools.Graphics.Builder.PlayerContentBuilders) to ship additional content with test players alongside the reference image and test data builders. The registry is process-wide: register once per domain reload, or unregister when you are done.
- **Content loading**: Register a custom [`TestContentBundle`](xref:UnityEngine.TestTools.Graphics.TestContentBundle) implementation with `TestContentLoader.RegisterBundle` to load content from a custom source. Set its `LogicalName` to a declared bundle name to serve that test data, and `PartOfGlobalSearch` to false to keep its assets out of the reference image search. Registered bundles are searched after the ones the build settings declare.

## Additional resources

- [Run code before a test](custom-pre-build-steps.md)
- [Customizing and optimizing tests](build-customization-landing.md)
- [Write a rendering code test](../test-authoring/writing-graphics-tests-texture.md)
