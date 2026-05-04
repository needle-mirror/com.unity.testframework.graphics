# Deduplicate reference images

To reduce storage space, replace duplicate reference images with a single image. For example when different platform tests have the same reference image.

Follow these steps:

1. To open the **Graphics Tests** window, select **Window** > **General** > **Graphics Tests**.
2. Select **Optimize Images**.

Unity compares reference images for each test across all platform directories. If two or more images are identical, Unity keeps one version and deletes the others, then moves the single remaining version to the shared `Assets/ReferenceImagesBase` folder in the **Project** window.

The deleted files don't affect tests, because tests check both platform-specific folders and `Assets/ReferenceImagesBase` for reference images.

## Deduplicate automatically

To optimize automatically when there's a change in the reference image folders, follow these steps:

1. In the **Graphics Tests** window, select **Settings**.
2. Enable **Auto Optimize**.

To configure optimization for a specific scene, add a Graphics Tests Settings component. For more information, refer to [Graphics Test Settings component reference](../test-investigation/graphics-test-settings-component.md). 

## Deduplicate from a C# script

To call [`ReferenceImageOptimizer.OptimizeReferenceImages`](xref:UnityEditor.TestTools.Graphics.ReferenceImageOptimizer) from a C# script, follow these steps:

1. Call the `OptimizeReferenceImages` API:

    ```csharp
    ReferenceImageOptimizer.OptimizeReferenceImages(filter: "", maxConcurrency: 4);
    ```

2. Subscribe to the `ReferenceImageOptimizer.OnStatsReceived` to run code when the results of the optimization are ready. For example:

    ```csharp
    ReferenceImageOptimizer.OnStatsReceived += (sender, args) =>
    {
        Debug.Log($"Test: {args.TestName}, Platforms: {args.Metrics.PlatformCount}, Divergence: {args.Metrics.AccumulatedDivergence}");
    };
    ```

3. Subscribe to the `ReferenceImageOptimizer.OnOptimizationComplete` event to run code when the optimization has finished deleting and moving reference images. For example:

    ```csharp
    ReferenceImageOptimizer.OnOptimizationComplete += (sender, result) =>
    {
        Debug.Log($"Status: {result.Status}, Deleted: {result.DeletedFiles.Count}, Moved: {result.MovedFiles.Count}");
    };
    ```

## Additional resources

- [Shader stripping](graphics-test-shader-stripping.md)
- [Customizing and optimizing tests](../build-customization/build-customization-landing.md)
- [The Graphics Tests window](../test-investigation/the-graphics-tests-window.md)
