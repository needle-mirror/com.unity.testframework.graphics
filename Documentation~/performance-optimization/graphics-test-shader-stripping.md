# Strip shader variants from test builds

To reduce build times and the size of the built Unity Player, remove shader variants you don't need when you build a test.

For more information about shader variants, refer to [Introduction to shader variants](https://docs.unity3d.com/Manual/shader-variants.html).

Follow these steps:

1. [Create a shader variant collection](https://docs.unity3d.com/Manual/shader-variant-collections.html) with the shaders your project uses.

2. In the **Graphics Tests** window, select **Settings** and make sure **Enable Shader Stripping** is enabled.

3. From the main menu, select **Assets** > **Create** > **Graphics Test Framework** > **Shader Variant List** to create a shader variant list.

4. Select **Update Shader Variants From SVC** and select your shader variant collection.

Unity now includes only the shader variants in the shader variant list in your test builds.

You can also create a list of shader variants from Unity log files. For more information, refer to [Shader Variant List Import Settings Inspector window reference](../performance-optimization/shader-variant-list-inspector.md).

## Additional resources

- [Customizing and optimizing tests](../build-customization/build-customization-landing.md)
- [Deduplicate reference images](reference-image-optimization.md)
