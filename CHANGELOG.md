# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

---

## [9.0.0-pre.7] - 2026-05-19

### Fixed

- Fixed `GraphicsTestBuildSettings.Save()` silently dropping `PlatformSchema` types whose `IPlatformNode` is not currently registered with `PlatformNodeRegistry`. `PlatformSchema.OnBeforeSerialize` now preserves already-resolved `Types` entries as a fallback when the registry lookup fails, so extras introduced by test platforms survive serialization.

## [9.0.0-pre.6] - 2026-05-04

### Added

- Added `-enable-shader-stripping` command-line argument for manipulating shader stripping from CLI.
- Exposed command-line argument values in the GraphicsTestBuildSettings class for documentation purposes.

## [9.0.0-pre.5] - 2026-05-04

### Fixed

- Fixed errors being raised by `AssetDatabase.ImportAsset` when post-processing reference images. 

## [9.0.0-pre.4] - 2026-04-23

### Added

- Added `ReferenceImageRootSource` enum and optional `IReferenceImageNamingStrategy` interface so parameterized graphics tests can use a reference image root other than the full parameterized test name (for example one reference per scene via `SceneAssetFileStem` on `GraphicsTestAttributeBase`).

### Changed

- Made D3D12 validation test framework classes cross-platform to match `UnityEngine.D3D12Validation`.

## [9.0.0-pre.3] - 2026-04-10

- Added D3D12 validation layer support to detect GPU validation errors during tests when running with `-force-d3d12-debug-as-errors`.
- Updated `com.unity.test-framework` dependency to 1.8.0.

## [9.0.0-pre.2] - 2026-04-08

### Fixed

- Fixed `ReferenceImageOptimizer.OptimizeReferenceImages` crashing when the optimization task faults, due to unconditional access to `task.Result` on a faulted task.
- Fixed `ReferenceImageOptimizer.OptimizeReferenceImages` allowing concurrent runs by adding an early return when already running.
- Fixed `ReferenceImageAssetBundle.LoadBundleAsync` falling through to `AssetBundle.LoadFromFile` when the bundle file does not exist.
- Fixed `EuclideanDistance.Compare` throwing `NullReferenceException` in player builds due to a null compute shader, now throws a descriptive `InvalidOperationException`.
- Fixed `EuclideanDistance.Compare` sync-over-async anti-pattern by removing unnecessary `Task.Run` wrapper.
- Fixed `EuclideanDistance.CompareAsync` leaking `ComputeBuffer` when GPU dispatch or data readback throws an exception.
- Fixed `ImageMessage.Serialize` operator precedence bug where `??` produced incorrect `MemoryStream` capacity preallocation.
- Fixed `StructuralSimilarity.Compare` leaking `NativeArray` allocations from locally created `LumaPipelineResult` objects.
- Fixed `TestUtils.GetTestResultsFolderPath` malformed `[Obsolete]` attribute message containing a stray parenthesis.
- Fixed `TaskManager.Complete` not disposing `ProgressStatus` entries, leaking per-subtask `CancellationTokenSource` objects.
- Fixed `TaskManager.Dispose` and `CancelAll` iterating `ConcurrentDictionary` keys during modification; now snapshots via `ToArray()`.
- Fixed `ReferenceImageAutoOptimizer` not disposing old `CancellationTokenSource` before replacing it.
- Fixed `PlatformNodeRegistry.LoadPluginsFromAssemblies` allowing a single bad `IPlatformNode` type to crash all node loading.
- Fixed `GraphicsTestLogger.Log` throwing `NullReferenceException` when passed a null message.
- Fixed `SceneFileSystemWatcher` and `GraphicsTestsWindow` persistence using fragile `string.Replace` for path manipulation on Windows; now uses `Path.GetDirectoryName`.
- Fixed `SceneFileSystemWatcher.s_RecompileRequested` missing `volatile`, causing a potential visibility race between thread-pool callbacks and the main thread.
- Fixed `CompareAsync` XML docs on `StructuralSimilarity` and `PeakSignalToNoiseRatio` incorrectly referencing `NotImplementedException` instead of `NotSupportedException`.
- Fixed `EditorWindowCapture.WaitForShadersToCompileAsync` not restoring `ShaderUtil.allowAsyncCompilation` on timeout, leaving async compilation permanently disabled for the Editor session.
- Fixed `ShaderVariantListImporter` throwing `IndexOutOfRangeException` when importing an empty or newly created `.shadervariantlist` file.
- Fixed `ShaderlabShaderExecutor` leaking `Camera` GameObject and `Mesh` across shader test runs; resources are now cleaned up in a `finally` block.
- Fixed `ShaderVariantListImporterEditor` crashing with `FileNotFoundException` when the user cancels the `OpenFilePanel` dialog.
- Fixed `ConvertTestFiltersToIgnoreAttribute` embedding unescaped `filter.reason` strings, which could produce invalid C# attribute syntax when the reason contains quotes or backslashes.
- Fixed `GraphicsTestsWindow.OnEnable` stacking duplicate visual trees and callback subscriptions when the window is re-enabled after a domain reload.
- Fixed `TestListener` registering duplicate `EditorConnection` handlers after each domain reload, causing test results to be processed multiple times.
- Fixed `LinkWatcher` accumulating duplicate `hyperLinkClicked` handlers after each domain reload.
- Fixed `GraphicsTestsWindow` LogView `bindItem` stacking `ContextualMenuManipulator` instances on every rebind, causing duplicate context menus.
- Fixed `GraphicsTestsWindow` LogView auto-scroll logic accumulating `valueChanged` handlers on the vertical scroller.
- Fixed `GraphicsTestsWindow.ImageComparisonView.OnTestBuilderFinished` setting the wrong tab view (`m_TabView` instead of `m_ComparisonTabView`), causing incorrect tab selection.
- Fixed `BakeLightingAttribute` not unsubscribing `logMessageReceived` callback if `Lightmapping.Bake()` throws an exception.
- Fixed `ImageCapture.CaptureFrame` leaking temporary `RenderTexture` when `ReadPixels` or `Apply` throws an exception in HDR mode.
- Fixed `AssetBundleBuilder.BuildContent` throwing `NullReferenceException` when a test case has a null `ReferenceImageDescriptor`.
- Fixed `RemoteReferenceImageAssetBundle.ContainsAsset` not normalizing the asset name, causing inconsistent results versus `LoadAsset` which strips the file extension.
- Fixed `ShaderTestFramework.LoadShader` caching a null loader in the dictionary when `Activator.CreateInstance` fails, poisoning subsequent calls.
- Fixed `ShaderTestFramework.Dispose` allowing one failing loader `Dispose` to skip cleanup of remaining loaders.
- Fixed `ShaderlabShaderLoader.LoadShader` crashing with `ArgumentOutOfRangeException` on paths with no file extension; now uses `Path.ChangeExtension`.
- Fixed `CliSettingsConsistencyValidator.ValidatePlayer` throwing `ArgumentException` on invalid `-playerGraphicsAPI` values instead of returning a validation failure.
- Fixed `ResultsUtility.IsWithinRoot` prefix-matching allowing path escape when the root path does not end with a directory separator.
- Fixed `TestListener.OnTestFinishedMessageReceived` throwing `NullReferenceException` when deserialized JSON produces a null `results` array.
- Fixed `GraphicsTestNewComparisonWindow.OnOpen` throwing `NullReferenceException` when the UXML asset fails to load.
- Fixed `GraphicsTestsWindow` image comparison view leaking heatmap `Texture2D` instances on every update by destroying the previous texture before assigning a new one.
- Fixed `GraphicsTestsWindow` LogView `ScrollToItem` passing index -1 when the filtered log list is empty.
- Fixed `GraphicsTestBuildSettingsEditor.OnEnable` throwing `SerializedObjectNotCreatableException` on assembly reload when the target `ScriptableObject` is temporarily null.
- Fixed `EditorGraphicsTestBuildManager.Build` not validating the `platforms` parameter, throwing a `NullReferenceException` instead of `ArgumentNullException` when null.
- Fixed `GraphicsTestCase.AdditionalReferenceImages` throwing `NullReferenceException` when `ReferenceImageDescriptor` is null.
- Fixed `ShaderWriter` always overwriting the same `Assets/TestShader.compute` file, causing multiple HLSL shader handles to alias to the last written shader.
- Fixed `HlslShaderExecutor.ExecuteShader` silently returning `default(T)` on GPU readback error instead of throwing an exception.
- Fixed `MainThreadDispatcher.RunOnMainThread` deferring execution via the queue even when already on the Unity main thread; now executes synchronously.
- Fixed `GraphicsTestsWindow.CreateOrShowWindow` potentially returning null when `HasOpenInstances` is true but the window object cannot be found; now falls through to `CreateWindow`.
- Fixed `ResultsUtility.ExtractImagesFromTestProperties` reading from `TestContext.CurrentContext` instead of the `test` parameter, causing incorrect property extraction when the two differ.
- Fixed `HlslShaderWrapperGenerator.GenerateShaderWrapper` concatenating `Arrange` and `Act` blocks without a newline separator in the generated compute shader.
- Fixed `TestContentLoader.LoadContent` running the `ContinueWith` continuation on the thread pool instead of dispatching to the Unity main thread for safe API access.
- Fixed `ImageAssert.AreEqual` overwriting the `out ImageComparisonResults` parameter with a default (zeroed) value on the success path, causing callers to receive incorrect metrics.
- Fixed `ImageAssert.CheckGCAllocWithCallstack` leaving `camera.targetTexture` set to the temporary render texture on early exit paths; now cleared in the `finally` block.
- Fixed `ImageAssert.AreEqualAsync` ignoring `AsyncGPUReadback.hasError`, silently producing invalid comparison results on GPU readback failure.
- Fixed `PeakSignalToNoiseRatio.Psnr` returning `Infinity` when comparing identical images (MSE = 0); now returns `float.MaxValue`.
- Fixed `ImageMessage.GetBytes` allowing unbounded memory allocation from corrupt payloads; now validates the declared length against remaining stream bytes.
- Fixed `PlayerGraphicsTestBuildManager.Build` not validating the `platforms` parameter, matching the guard already present in the Editor variant.
- Fixed `GraphicsTestsWindow.SelectTest` selecting an arbitrary tree item (id 0) when the requested test name is not found; now early-returns.
- Fixed `GraphicsTestsWindow` persistence layer crashing the window on corrupt `EditorPrefs` or test results JSON; deserialization is now wrapped in `try/catch`.
- Fixed `ReferenceImageOptimizer` leaving `k_IsRunning` permanently stuck when the optimization task is canceled, blocking all future runs.
- Fixed `PlatformSchema.OnBeforeSerialize` throwing `NullReferenceException` when `Types` is null or `rootPath` is null; now initializes defensively.
- Fixed `PlatformSchema.GetTypes` adding null entries to the `Types` list when `Type.GetType` fails to resolve a type name.
- Fixed `CommandLineReader` prefix matching allowing false positives (e.g. `-log` matching `-logFile`); now requires exact match or `=` delimiter.
- Fixed `ReferenceImageAssetBundle.LoadFromFile` throwing `NullReferenceException` when `AssetBundle.LoadFromFile` returns null; now sets `Failed` state and logs at Error level.
- Fixed `IgnoreGraphicsTestData.MatchesPlatform` throwing `ArgumentNullException` when `m_Platforms` is null after deserialization.
- Fixed `ReferenceImageOptimizer` averaged delta computation using the wrong denominator (`metrics.Count` instead of `deltaEMetrics.Count`), diluting the averaged heatmap.

### Changed

- `ReferenceImageAssetBundle.LoadBundleAsync` now logs missing bundles at `Warning` level instead of `Log`.
- `ReferenceImageAssetBundle.LoadFromFile` error catch block now logs at `Error` level instead of `Log`.

## [9.0.0-pre.1] - 2026-03-31

- Removed dependency to com.unity.external.test-protocol as it is only used by internal tests and should not be a dependency for the package itself.

## [9.0.0-exp.57] - 2026-03-05

### Added

- Added `TestNotSupportedOnAttribute` and `TestOnlySupportedOnAttribute` syntactic sugar attributes for marking platform-incompatible tests with non-overridable ignores.
- Added `GraphicsTestParamAttribute` and `GraphicsTestParamSourceAttribute` to replace the deprecated `GTestCaseAttribute` and `GTestCaseSourceAttribute`, unified through the `IGraphicsTestArgProvider` interface.
- Added `GraphicsTestParamParameterizer` to unify test case parameterization logic.
- Added `GlobalContext<TEnum>` abstract base class that merges the `IGlobalContextProvider` and `IPlatformNode` systems, eliminating boilerplate register/unregister code.
- Added `ShaderWarningCollector` service and `ShaderWarningTestRunCallback` to catch shader warnings during builds and test runs, with opt-in `-shader-warnings-as-errors` CLI flag.
- Added `IAssetService` abstraction (replacing direct `AssetDatabase` calls) to improve testability across the Editor assembly.
- Added `EuclideanDistanceResult` and `EuclideanDistanceSettings` as distinct types, replacing the monolithic `EuclideanDistanceComparisonResult`.
- Added path sanitization and directory traversal protection in `ResultsUtility` for untrusted XML input.
- Added `TreeViewModel`, `HeatmapManager`, `IgnoreDataExporter`, and `ColumnNames` as extracted, focused classes for the Graphics Tests Window.
- Added USS stylesheet for the Graphics Tests Window, replacing inline C# style definitions.
- Added `ShaderWarningsAsErrors` toggle to the `GraphicsTestBuildSettings` inspector.
- Added comprehensive package samples: Basic Graphics Tests, Parameterized Tests, Image Comparison Examples, Platform Filtering Examples, Editor Test Examples, and Advanced Patterns.
- Added new documentation pages: Platform System, Comparison Algorithms, Resolving Failing Tests, Graphics Test Logging, Editor Window Capture, and Sample Projects.

### Changed

- Removed all `System.Linq` usage across the entire package (Runtime, Editor, and Tests), replacing with allocation-free imperative alternatives.
- Simplified the `GraphicsTestBuildSettings` `Save()` and `Load()` methods. Settings remain in `Assets/Resources/` and are loaded at runtime via `Resources.LoadAll`.
- Simplified the `GlobalContextManager` into a thin facade over `PlatformNodeRegistry`, removing the need for explicit `RegisterGlobalContext`/`UnregisterGlobalContext` calls.
- Simplified `IgnoreGraphicsTestAttribute`: removed the legacy multi-parameter constructor, unsealed the class to allow inheritance, and changed the default `allowOverrideIgnore` to `true`.
- Replaced hardcoded image paths (`Assets/ReferenceImages`, `Assets/ReferenceImagesBase`, `Assets/ActualImages`) with configurable settings from `GraphicsTestBuildSettings`.
- Renamed `ImageComparisonSettings` properties to PascalCase for consistency (e.g., `RMSEThreshold`).
- Improved performance across the package: cached reflection in `GameViewSize`, used `GetPixelData<T>()` for zero-copy pixel access, replaced `RenderTexture.ReadPixels` with `Graphics.Blit` for GPU-based resize, added `TryGetValue`/`TryAdd` for dictionary lookups, and used `HashSet` for set operations.
- Refactored and restructured the entire documentation, replacing the old per-assembly layout with a task-oriented structure.
- Updated `ConvertTestFiltersToIgnoreAttribute` to emit the simplified `IgnoreGraphicsTestAttribute` constructor.

### Removed

- Removed `CodeBasedGraphicsTestAttribute`, `GTestCaseAttribute`, `GTestCaseSourceAttribute`, and their parameterizers in favor of the new `GraphicsTestParam*` family.
- Removed `EuclideanDistanceComparisonResult` (replaced by `EuclideanDistanceResult` and `EuclideanDistanceSettings`).
- Removed `GraphicsTestsWindow.ResultsLoader` (functionality absorbed into other window components).
- Removed `GraphicTestVariantStripper.asmdef` (consolidated into the main Editor assembly).
- Removed legacy documentation files under `Documentation~/Editor/` and `Documentation~/Runtime/`.

## [9.0.0-exp.56] - 2026-03-03

- Add variant navigation buttons to image comparison view

## [9.0.0-exp.55] - 2026-03-02

- Added helper method to reliably change player resolution

## [9.0.0-exp.54] - 2026-02-13

- Updated ImageAssert.AreEqual xmldoc to be more acurrate.

## [9.0.0-exp.53] - 2026-02-09

- Changed field to property to not attempt serialization

## [9.0.0-exp.52] - 2026-02-09

- Removed unused field

## [9.0.0-exp.51] - 2026-02-06

- Made the UnityEngine.TestTools.Graphics assembly internals visible to UnityEngine.TestTools.Graphics.Contexts.
- Added a TestSettingsReader static helper to read specific settings from client code, without needing to add a new TestSetting. 

## [9.0.0-exp.50] - 2026-02-04

- Removed automatic resizing of game view before running tests in-editor.

## [9.0.0-exp.49] - 2026-02-03

- Significantly improved the performance of test discovery by caching test case data and reducing redundant computations.

## [9.0.0-exp.48] - 2026-02-02

-  Added SSIM image comparison support.
-  Added support to reuse Luma calculations across different image comparisons.
-  Renamed ITextureComparisonThreshold to ITextureComparisonSettings.

## [9.0.0-exp.47] - 2026-01-30

- Small fix to NaN image comparison to clarify which image contains the pixels with issues.

## [9.0.0-exp.46] - 2026-01-30

- Added support for getting the target graphicsVendor from the test settings files.

## [9.0.0-exp.45] - 2025-01-29

- Major refactor of graphics test build workflow: now supports grouped scene lists, robust platform/test case extraction, and improved asset/image handling.
- Refactored `GraphicsTestAttribute` to inherit from `GraphicsTestAttributeBase` and use `DefaultGraphicsTestCaseSource`.
- UI and attribute changes to support new scene grouping and filtering mechanisms.

## [9.0.0-exp.44] - 2026-01-27

- Fixed an issue where an image comparison of different images would silently succeed if one of them has a +-infinity value in it.
- Added improved reporting of NaN and/or non-finite values including index of the first found bad value.
- Removed GPU NaN checker.

## [9.0.0-exp.43] - 2026-01-21

- Fixed an issue where platform node types could be registered multiple times in the NodeRegistry.

## [9.0.0-exp.42] - 2026-01-13

- Removed automatic resizing of backbuffer captures.
- Removed `ImageResolution` property from `ImageComparisonSettings` class.

## [9.0.0-exp.41] - 2026-01-12

- Added the evaluated RMSE value to the message displayed when `ImageAssert` fails.

## [9.0.0-exp.40] - 2026-01-08

- Fixed the "Convert Filters to Attributes" button by making the underlying TestFilter object serializable.

## [9.0.0-exp.39] - 2026-01-07

- Bumped com.unity.external.test-protocol to signed version.

## [9.0.0-exp.38] - 2025-12-17

- Added `IgnoreGraphicsTest` option to ignore based on Rendering.RenderingThreadingMode.

## [9.0.0-exp.37] - 2025-12-12

- Updated usage of ShaderType enum.

## [9.0.0-exp.36] - 2025-12-04

- Changed the representation of image extensions from a string to an enum `ImageExtension`.

## [9.0.0-exp.35] - 2025-12-03

- Bumped dependencies to ensure dependency packages are signed.

## [9.0.0-exp.34] - 2025-11-25

- Fixed an issue where trying to add an already existing tab to the Test Results view would result in an exception during the Prebuild step.

## [9.0.0-exp.33] - 2025-11-06

- Fixed an issue where the reference image assets would be loaded from the most generic platform path first (instead of the most specific).

## [9.0.0-exp.32] - 2025-11-05

- Added Shader Test Framework extension to test HLSL and ShaderLab shaders in isolation.

## [9.0.0-exp.31] - 2025-10-30

- Added support for ATI Graphics Vendor.

## [9.0.0-exp.30] - 2025-10-27

- Added support for multiple reference images per graphics test case.

## [9.0.0-exp.29] - 2025-10-13

- Forced Render Graph Context to handle URP Compatibility mode removal.

## [9.0.0-exp.28] - 2025-10-10

- Added support for multiple images in PSNR.

## [9.0.0-exp.27] - 2025-10-05

- Added a new API to capture one or multiple frames from camera or buffer.

## [9.0.0-exp.26] - 2025-09-11

- Added new `GraphicsTestsWindow` to replace all other UI. Found under `Window > General > Graphics Tests`.
- Added "View in Graphics Tests Window" link to the start of `GraphicsTest` output.
- Optimized the `Reference Image Optimization` process
- Removed `TestResultWindow` and associated assets.
- Removed `ReferenceImageAnalyzerWindow`.
- Removed `GraphicsTestSettingsWindow`.
- Moved all menu items to the `GraphicsTestBuildSettings` scriptable object or `GraphicsTestsWindow`.
- Moved the OverrideIgnoreAttributes command-line argument to the `GraphicsTestBuildSettings` scriptable object.
- Moved the SaveActualImages command-line argument to the `GraphicsTestBuildSettings` scriptable object.
- Abstracted command-line argument reading using `ICommandLineProvider` interface.
- Added an optional `ActualImageFileName` property to `LegacyImageExportOptions`. Use it if you want to control the file name of an actual image file after comparison.

## [9.0.0-exp.25] - 2025-08-25

- Removed several workarounds for filtering based on UTF command-line and TestRunner
- In the AutoBuilder, replaced PrebuildSetup with PrebuildSetupWithTestData
- In the AutoBuilder, replaced PostbuildCleanup PostBuildCleanupWithTestData

## [9.0.0-exp.24] - 2025-07-30

- Reworked the ImageAssert class to reduce complexity and improve test coverage
- Extracted the current image difference algorithm in its own class
- Added a new texture comparison assertion API that can work with different comparison algorithm

## [9.0.0-exp.23] - 2025-07-30

- Added support for Switch 2 platform.

## [9.0.0-exp.22] - 2025-07-02

- Reworked the Reference Image Optimizer system.

## [9.0.0-exp.21] - 2025-07-01

- Added `-urp-compatibility-mode` command-line argument that sets the `URP_COMPATIBILITY_MODE` define for player builds.

## [9.0.0-exp.20] - 2025-06-30

- Added an AssetPostprocessor to automatically enforce reference image import settings.

## [9.0.0-exp.19] - 2025-06-04

- Fixed an issue where the Graphics Test Cleanup would run even when no Setup was run
- Added a conditional compilation directive for the now-deprecated TreeView API in the Graphics Test Results window.
- Updated the `ShaderVariantListImporter` to be able to parse variants from UnityShaderCompiler logs.
- Improved the `GraphicsTestShaderStripper` to allow for way faster shader preprocessing.

## [9.0.0-exp.18] - 2025-05-14

- Added support for different texture formats for reference images on all platforms.

## [9.0.0-exp.17] - 2025-05-09

- Fixed documentation and structure to comply with validation rules.

## [9.0.0-exp.16] - 2025-05-08

- Fixed test case naming to use the correct test name for the purposes of the TestRunner.
- Added an internal Test Tree to help with selecting test cases for the test run.
- Refactor the PlatformProvider architecture getter to support building x64 players on arm64 machines.

## [9.0.0-exp.15] - 2025-04-29

- Refactored the backslash sanitization to use a common method for all paths.
- Refactored the `SceneGraphicsTestCaseSource` to use the new path sanitization method and broke it into smaller methods for better readability.

## [9.0.0-exp.14] - 2025-04-08

- Added command-line argument to override the IgnoreGraphicsTest attribute (`-override-graphics-test-ignores`)
- Added `IgnoreGraphicsTestMode` enum to specify the matching mode of the IgnoreGraphicsTest attribute patterns.
- Added `allowOverrideIgnore` bool to the `IgnoreGraphicsTestAttribute` to allow overriding the attribute in the command line (default: true)
- Allowed filtering test suites based on the `IgnoreGraphicsTestAttribute` attribute.
- Modified filtering to consider the Full Name of the test case

## [9.0.0-exp.13] - 2025-04-06

- Reformatted most of the code to follow CSharpier formatting rules.
- Updated XML documentation to pass experimental documentation validation.
- Added upgrade guide 8 -> 9
- Added what's new section for test framework 9
- Changed the accessibility modifiers of some classes and methods to be more restrictive.
- Added API documentation for all public classes and methods

## [9.0.0-exp.12] - 2025-04-01

- Removed the unused and outdated `GenerateCodeCoverage` class.

## [9.0.0-exp.11] - 2025-03-28

- Add `ActivateContext`, `OnContextRegistered` and `OnContextUnregistered` methods to the `IGlobalContextProvider` interface.

## [9.0.0-exp.10] - 2025-03-24

- Modified `SceneGraphicsTestCaseSource` to allow SceneGraphicsTestCase subclasses to access data.

## [9.0.0-exp.9] - 2025-03-19

- Added validation in the IGraphicsTestBuildManager to ensure that test cases' TestMode is consistent with the TestBuildContext
- Changed test filtering to consider the full name of the test case when filtering tests for the build
- Optimized test filtering calls and refactored to a separate class
- Added tests for the test filtering functionality
- Added extra validation for contexts in `IgnoreGraphicsTestAttribute`

## [9.0.0-exp.8] - 2025-03-17

- Added the `TestModeAttribute` to allow for the selection of the test mode for a test assembly.
- Added the `TestMode` enum to define the test mode for a test assembly (mirrors the `TestMode` enum in `UnityEditor.TestRunner`).

## [9.0.0-exp.7] - 2025-03-13

- Fixed an issue where writing test images to the same directory would lead to an IOException.
- Fixed an issue where test filtering would have no effect on the scenes built for the Graphics Test Framework.

## [9.0.0-exp.6] - 2025-02-26

- Fixed an issue where the command-line argument for enabling RenderGraph was `-rendergraph-reuse-tests` instead of `render-graph-reuse-tests` as expected by the URP package.
- Added a transition scene to each Graphics Test build to facilitate state changes and to avoid leaking between tests.
- Changed the return type of `GlobalContextManager.RegisterGlobalContext` to be the context that is registered.

## [9.0.0-exp.5] - 2025-02-20

- Added a method overload for `ImageAssert.AreEqual` that allows to control logged messages and returns the results of the image-comparison job as an `ImageComparisonResults` struct.
- Modified `BaseImageOptimization.StrategySingleBaseMostCommonImage` to choose the base image based on the accumulated-divergence for the comparison of all reference-images for a Graphics Test Case.
- Added `BaseImageOptimization.CachedMetricsPerImage` dictionary containing the image-comparison metrics for the last invocation of `BaseImageOptimization.Optimization`.
- Added extra parameters to `BaseImageOptimization.Optimization` to be able to control image-modifications and name-based filtering for the Graphics Test Cases in a project.
- Added `ReferenceImageAnalyzerWindow` to be able to gather and analyze image-comparison metrics for the Graphics Test Cases in a project.

## [9.0.0-exp.4] - 2025-02-17

- Added support for TestFixtures to be used in the Graphics Test Framework. This allows for the setup and teardown of test data to be done once for multiple tests.
- Added the `SupportsComputeShaders` boolean to `GraphicsDeviceInfo` to indicate if the device supports compute shaders.
- Fixed the IgnoreAttribute not correctly filtering tests based on Global Context.
- Fixed an issue where `SceneGraphicsTestCaseSource` would not work correctly with multiple regex patterns.

## [9.0.0-exp.3] - 2025-02-03

- Multiple asset bundles will now be built for each platform (eg. base references images) to avoid having asset bundles with hundreds of images. The current max size is 8 images, but we plan to potentially raise that and expose it to the GraphicsTestBuildSettings.
- Asset bundles will now be loaded in order of the platform specificity, descending. So, most specific platform first and base images last.
- If tests are generated through scenes, the test list will now respect the order that the build settings have the scenes in.
- It is now possible to use any TextureFormat and any file extension for reference images
- Memory allocation errors now use an Assertion and not just and Exception

## [9.0.0-exp.2] - 2025-01-15

- Fixed overwrites in the `EditorBuildSettings` scenes which caused scenes to be rewritten out of order. Now, if scenes are placed in the `EditorBuildSettings` in a specific order, the order is maintained. The rest of the scenes, if any, are appended after them.
- Fixed several issues in the `BakeLighting` attribute, including filtering out scenes based on the filtered tests, fixed baking APVs correctly.
- Fixed the `TestFiltersEditorWindow` not correctly displaying the converted attributes for some reason.
- Reverted the `ShaderStripper` functionality to its 8.9.1 capacities. No meaningful changes had been made in 9.0 yet, and it was tough to reason about whether it was functioning correctly.
- Fixed `IgnoreGraphicsTest` attribute not correctly filtering scenes out of the build
- Fixed documentation for `isInclusive`
- Fixed inclusive test filters ignoring too many test cases
- Added the path of the bundle asset path to the load message for reference images.
- Added the ability to add a command-line argument to enable `GRAPHICS_TEST_FRAMEWORK_DEBUG` mode
- Added regex support for `SceneGraphicsTest` scene path definitions

## [9.0.0-exp.1] - 2024-12-20

### Added

#### Graphics Test Platform

- Added the `GraphicsTestPlatform` class to provide information about the platform used in the test run.
- Added the `IGraphicsTestPlatformProvider` interface to provide information about the platform used in the test run.
- Added the `PlayerBuildGraphicsTestPlatformProvider` class to provide information about the `GraphicsTestPlatform` used for a player build.
- Added the `EditorGraphicsTestPlatformProvider` class to provide information about the `GraphicsTestPlatform` used in the editor.
- Added the `RuntimeGraphicsTestPlatformProvider` class to provide information about the `GraphicsTestPlatform` used at runtime.
- Added the `MatchesPlatform` method to the `TestFilterConfig` class.

#### Graphics Test Build

- Added the `GraphicsTestBuilder` class to build the graphics tests (replacing the `SetupGraphicsTestCases` class).
- Added the `IGraphicsTestBuildManager` interface to manage the build process for graphics tests.
- Added the `PlayerGraphicsTestBuildManager` class to manage the build process for graphics tests for a player.
- Added the `EditorGraphicsTestBuildManager` class to manage the build process for graphics tests for the editor.
- Added the `GraphicsTestBuildResult` enum to provide the result of the graphics test build process.
- Added the `GraphicsTestBuildContext` enum to provide context information for the graphics test build process.
- Added the `GraphicsTestBuildSettings` scriptable object to provide settings for the graphics test build process and transfer them between the editor and player.

#### Other

- Added the `ReferenceImage` class to represent a reference image. It contains the path to the reference image and reference image base. It is used to defer loading the reference image until it is needed.
- Added the ability to filter tests based on the `-testFilter` command-line argument.
- Added an `ArchitectureExtensions` class to convert from `System.Runtime.InteropServices.Architecture` to our `Architecture` enum.
- Added a `StereoRenderingPathExtensions` class to convert from `StereoRenderingPath` to our `StereoRenderingModeFlags` enum.
- Added a `BuildTargetExtensions` class to convert from `BuildTarget` to `RuntimePlatform` enum.
- Added a `StringExtensions` class to provide an extension method for string sanitization.
- Added the `IgnoreGraphicsTest` attribute to make it easier to ignore tests.

### Changed

- Moved test filtering functionality to the `TestFilterUtility` class.
- Moved Asset bundle building functionality to the `AssetBundleBuilder` class.
- Moved Asset bundle loading functionality to the `AssetBundleLoader` class.
- Moved Light Baking related functionality to the `LightBakingUtility` class.
- Moved all Test Runner related functionality to the `TestRunnerUtility` class.
- Moved all runtime Reference Image related functionality to the `ReferenceImageUtility` class.
- Moved all XR related functionality to the `XRPlatform` class.
- Moved the `ImageHandler` class to its own file.
- Moved `CopyImageToReferenceFolder` functionality to the `EditorReferenceImageUtility` class.
- Moved `CreateSceneListFileFromBuildSettings` functionality to the `PlayerGraphicsTestBuildManager` class.
- Moved `GameView` related functionality to the `GameViewSize` class.
- Replaced Errors with Exceptions in `CliArgumentsCheck` class.
- Refactored and cleaned up the `GraphicsTestCase` class.
- Reduced the responsibilities and implementation details of the `GraphicsTestCaseProvider` classes.
- Renamed `RuntimePlatformExtension` to `RuntimePlatformExtensions`.
- Moved the `GraphicsTestLogger` class to the `UnityEngine.TestTools.Graphics` namespace to make it accessible from a player.
- Replaced calls to Debug.Log with calls to `GraphicsTestLogger.Log()`.
- Reorganized menus under `Tools/Graphics Test Framework` in the Unity Editor.
- Split up documentation to separate files for better readability and maintainability.
- Reformatted the Changelog file to adhere to MarkdownLint rules.
- Reformatted the package.json file.

### Removed

- Deprecated the `SetupGraphicsTestCases.Setup()` method as it was replaced by `GraphicsTestBuilder.Build()`.
- Removed `CaptureSceneView` as it was replaced by `EditorWindowCapture`.
- Removed the `TestPlatform` class as it was replaced by the `GraphicsTestPlatform` class.
- Removed the `GetTestCaseFromPath` method from the `IGraphicsTestCaseProvider` as it was unused.
- Removed several unused methods.
- Removed several old conditional compilation directives.
- Removed several instances of commented out code.
- Removed reference image loading functionality from the CodeBasedGraphicsTestAttribute class.
- Removed the `TestUtils` class.
- Removed the `EditorUtils` class.
- Completely removed the functionality of the `TestFilters` system, but added a converter to the `IgnoreGraphicsTest` attribute for easy migration.

## [8.9.1-exp.1] - 2024-10-10

- Updated the regular expression to match the new log format for shader variants sent to the GPU driver.

## [8.9.0-exp.1] - 2024-07-30

- Added parameter to `ImageAssert.AreEqual` to allow for saving images to the application persistent data path without an Editor connection.

## [8.8.0-exp.1] - 2024-07-23

- There is now an ImageAssertAsync function to use AsyncGPUReadback to get test results
- Added support for the WebGPU graphics API

## [8.7.1-exp.1] - 2024-07-15

- There is now a function to check if there are any "useless" variants in the shader variant list (multiple variant leading to the same compiled shader code).

## [8.7.0-exp.1] - 2024-06-12

- Enable the use of GraphicsTestLogger at Runtime for certain platforms.
- Reorganized menu items in the Tools/Graphics Test Framework menu.
- Several minor fixes and improvements.

## [8.6.3-exp.1] - 2024-05-29

- Update ImageAssert to properly encode RGBAFloat images as full-precision EXR files, rather than half-precision.
- Fixed UUM-73039: Error is thrown if reference image is not present on Android and WebGL

## [8.6.2-exp.2] - 2024-05-10

- Renamed package displayName to "Graphics Test Framework" in the package manifest
- Reformatted this document and the package manifest

## [8.6.2-exp.1] - 2024-05-03

- Added CliArgumentsCheck class to validate graphics related command-line arguments.

## [8.6.1-exp.1] - 2024-04-29

- Added the GraphicsTestLogger class to log the graphics test-related information.
- Added utility tool to copy image references to the appropriate folder.

## [8.6.0-exp.1] - 2024-04-09

- Added EditorWindowCapture class to capture the content of an EditorWindow.
- Made CaptureSceneView methods obsolete as it was replaced by EditorWindowCapture.

## [8.5.1-exp.1] - 2024-03-21

- Added Windows ARM64 support.
- Added TestPlatform as an abstraction of the platform used in the test run.
- Added tests for the RuntimePlatformExtensions class.

## [8.5.0-exp.1] - 2024-03-12

- Introduced GraphicsDeviceInfo class to provide information about the graphics device used in the test run.

## [8.4.1-exp.1] - 2024-03-08

- Moved and renamed the Validation Tests to be more descriptive

## [8.4.0-exp.1] - 2024-03-07

- Introduced VisionOS support.

## [8.3.3-exp.1] - 2024-03-06

- Update the regex used to match the shader variant compilation to reflect changes in the editor logger.

## [8.3.2-exp.1] - 2024-01-29

- Only bake scenes included in the build

## [8.3.1-exp.1] - 2023-12-01

- Force the Shader Variant List generator to always use Ordinal sorting (instead of undetermined comparer).

## [8.3.0-exp.1] - 2023-09-29

- Add the Architecture enum to TestFilterConfig
- Update SetupGraphicsTestCases code to allow tests to be filtered according to processor architecture

## [8.2.2-exp.1] - 2023-08-18

- Move scene filter into GetScenePaths method so all callers get the correct scene list.

## [8.2.1-exp.1] - 2023-08-16

- Make GetScenePaths method public for reuse outside of the class

## [8.2.0-exp.1] - 2023-08-10

- Remove warning from CustomTestRunCallback due to unnecessary Monobehaviour inheritance

## [8.1.0-exp.1] - 2023-08-08

- Update the BuildScenes method, in that way so it also cover consoles behavior

## [8.0.0-exp.1] - 2023-08-07

- Enable asynchronous AssetBundle loading for WebGL and Android platforms

## [7.17.4-exp.1] - 2023-08-04

- Added conditional for the custom BuildPlayer, to avoid executing it when running in the standalone mode.

## [7.17.3-exp.1] - 2023-07-07

- Added a check in the HDR image comparison to avoid propagating NaNs in the image comparison.

## [7.17.2-exp.1] - 2023-06-06

- Added a custom test run callback to prevent OnGUI() callbacks from UTF component at runtime while testing.

## [7.17.1-exp.1] - 2023-05-30

- Update com.unity.addressables dependency.

## [7.17.0-exp.1] - 2023-05-11

- Fixed Shader Variant stripper not correctly supported on Vulkan.

## [7.16.0-exp.1] - 2023-04-21

- Added a custom runtimeplayer platform parameter as overload to the method BuildTargetToRuntimePlatform.

## [7.15.0-exp.1] - 2023-03-22

- Added log message for the expected image path to the test failure

## [7.14.1-exp.1] - 2023-03-08

- Fixed DXR shaders not compatible with graphics test stripper.

## [7.14.0-exp.1] - 2023-03-06

- Add EmbeddedLinux and QNX to EditorUtils

## [7.13.0-exp.1] - 2023-03-06

- Add Graphics Test Shader Stripping system, when used in test projects, allows to speed up a lot the shader compilation by removing unused shader variants.

## [7.12.0-exp.3] - 2023-01-31

- Fixed XR filter check that returned true even with 0 loaders selected.
- Added functionality to allow setting the resolution in code instead of per-scene assets.

## [7.12.0-exp.2] - 2023-01-16

- Wrap Stadia platform and GLES2 graphics API in !2023_1_OR_NEWER due to deprecation in 2023.1 and higher.

## [7.12.0-exp.1] - 2023-01-16

- Added GUI setting option, which enabled, saves actual images even if the tests pass. Added an extra "-save-actual-images" command line option to the player so that the feature can be enabled while running the tests form command line.
- Renamed `HandleFailedImageEvent` method to `HandleImageEvent`, `FailedImageMessage` class to `ImageMessage`

## [7.11.1-exp.1] - 2022-11-22

- Added 'ImageComparisonSettings.TargetMSAASamples' integer that is respected when creating a target texture for the test framework to run when UseBackbuffer is false.

## [7.11.0-exp.1] - 2022-11-22

- Added `ImageAssert.CheckGCAllocWithCallstack()` to render an image from the given camera and check if it allocated memory while doing so, outputting the callstack of the GC.Alloc if found.
- Moved the `SetupProject` class behind the `UnityEditor.TestTools.Graphics` namespace to be able to call it from external code. Also added a non-namespaced wrapper that calls it to make the change non-breaking.

## [7.10.1-exp.1] - 2022-10-17

- Added `GenerateCodeCoverage` class that contains the method used to automate the code coverage analysis on the scene-based graphics tests using the `com.unity.testtools.codecoverage` package's on demand recording.

## [7.10.0-exp.8] - 2022-09-26

- Update documentation

## [7.10.0-exp.7] - 2022-09-08

- Fix version of UTF in the asmdef define

## [7.10.0-exp.6] - 2022-09-07

- Added an extra "-render-graph-reuse-tests" command line option and define to the player so that RenderGraph test code can be enabled while still using the regular (non-RG) reference images.
- Add support for LinuxHeadlessSimulation platform

## [7.10.0-exp.5] - 2022-08-30

- When using the Test Scene Asset, if the "SceneName_RPAssetName" reference image is not found, fall back to the base "SceneName" reference image.

## [7.10.0-exp.4] - 2022-08-17

- Fixed a performance regression that caused reference image assets to be reimported at every selection in the Test Results window.

## [7.10.0-exp.3] - 2022-08-17

- Update version defines for Unity Test Framework

## [7.10.0-exp.2] - 2022-07-25

- Removed `com.unity.subsystemregistration` package dependency.

## [7.10.0-exp.1] - 2022-05-27

- Added new image assert function that implements functionality to do image asserts with floating point images. These images can have negative numbers and are written out as EXR.
- Added new image comparison function that implements MSE/RMSE comparisons.
- Added support for applying texture import settings for the written ActualImages.

## [7.9.0-exp.1] - 2022-05-18

- Added SRP Test Scene Asset. Which allows users to add scenes combined with SRP Assets. So that we can test 1 scene with several different SRP's.

## [7.8.23-exp.2] - 2022-04-21

- Added support for parametric CodeBasedGraphicsTests.
- Added support for Playmode CodeBasedGraphicsTests.
- Made Graphics Test Results window TreeView scrollable
- Added compatibility between CodeBasedGraphicsTests and the ReferenceImagesBase folder

## [7.8.23-exp.1] - 2022-04-11

- Added support for new PS5 GraphicsDeviceType.

## [7.8.22-exp.2] - 2022-04-06

- Added `UNITY_TESTS_FRAMEWORK` defines for the package assemblies so they can be referred by test projects without adding to the `testables` section of the manifest.json file.

## [7.8.22-exp.1] - 2022-04-04

- Reworked `CaptureSceneView` to use the back-buffer instead of a screenshot.

## [7.8.21-exp.2] - 2022-04-04

- Fixed the issue in `ImageAssert.cs` with running the optimization for reference images.
- Fixed the issue in the optimization implementation.

## [7.8.21-exp.1] - 2022-03-22

- Added `CodeBasedGraphicsTestAttribute` which allows a unit test to be marked as a graphics test, and root paths of reference images and actual images to be specified.
- Added `UnityEditor.TestTools.Graphics.EditorUtils` class which provides a few utility functions. Please refer to the documentation page.
- Added `UnityEngine.TestTools.Graphics.TestUtils` class which provides utility functions for generating test result folder path based on give test configurations.
- Changed `GraphicsTestCase` class now provides new properties returning the test name and the `CodeBasedGraphicsTestAttribute` for unit test based graphics tests.

## [7.8.20-exp.1] - 2022-03-21

- Add Optimization feature available for reference images.

## [7.8.19-exp.2] - 2022-02-16

- Allow custom scene views as input to CaptureSceneView.

## [7.8.19-exp.1] - 2022-02-16

- Add the CaptureSceneView class that enables scene view graphics tests.

## [7.8.18-exp.1] - 2022-02-15

- Updated xbox references to reflect enum changes in the editor.

## [7.8.17-exp.3] - 2022-02-07

- Updated `nuget.newtonsoft-json` and `xr.legacyinputhelpers` dependencies.

## [7.8.17-exp.2] - 2022-01-28

- Fixed a missing clear in UTR framework in order to make Gamecore pass correctly the Graphic Compositor test.

## [7.8.17-exp.1] - 2021-11-18

- Add support for tests on Apple Silicon (M1) devices.
- Moves reference images for M1 macs under OSXPlayer_AppleSilicon and OSXEditor_AppleSilicon.
- Leaves reference image paths for Intel macs untouched (they remain under OSXPlayer and OSXEditor).

## [7.8.16-preview] - 2021-08-06

- Fixed an issue where the framework would build asset bundles for standalone when running in the editor, when using 1.2 version of Unity Test Framework package.

## [7.8.15-preview] - 2021-07-29

- Add [RequiresPlayMode] tags to the tests intended for PlayMode, allowing them to still run in PlayMode in the newest UTF versions.
- Change the logic in SetupGraphicsTestCases to draw the targetPlatform from the settings directly, rather than from the filter.

## [7.8.14-preview] - 2021-07-23

- Add "GlobalResolutionSetter" component that lets you set different device resolution per platform

## [7.8.12-preview] - 2021-05-26

- Fix issue with test filter and XR reusable tests

## [7.8.11-preview] - 2021-05-11

- Bump com.unity.xr.management from 3.2.15 to 4.0.5

## [7.8.10-preview] - 2021-04-26

- Make `StripParametricTestCharacters` replace "," with "-".
- Make `StripParametricTestCharacters` replace "(" with "_".
- Make `StripParametricTestCharacters` replace ")" with "_".

## [7.8.9-preview] - 2021-04-23

- Make `StripParametricTestCharacters` replace "," with "_".

## [7.8.8-preview] - 2021-04-08

- Reenable AreEqual_WidthDifferentSizeImages_ThrowsAssertionException (was removed in 7.8.2-preview)
- Bump com.unity.addressables from 1.16.15 to 1.17.15

## [7.8.7-preview] - 2021-03-10

- Remove BlackBerry player support.
- Don't clear the GICache on every bake.

## [7.8.6-preview] - 2020-03-08

- Fix typo in GC Alloc messages
- Remove unused code
- Fix for undeterministic RuntimePlatform -> string conversion

## [7.8.5-preview] - 2020-02-18

- Fix buildOptions error
- Avoid RenderTexture usage for GC tests when possible

## [7.8.4-preview] - 2020-02-10

- More build options in ApplySettings

## [7.8.3-preview] - 2021-02-03

- Add support for new console platforms
- Fixes for the CHANGELOG.md validation
- NDA platform validator configuration added

## [7.8.2-preview] - 2021-01-29

- Disable AreEqual_WidthDifferentSizeImages_ThrowsAssertionException

## [7.8.1-preview] - 2021-01-28

- Test filter sort now uses stable sorting with additional properties

## [7.8.0-preview] - 2021-01-07

- Reference dependencies needed for isolation testing

## [7.7.1-preview] - 2020-11-30

- Add support for new GraphicsDeviceTypes

## [7.7.0-preview] - 2020-11-16

- Add support for XR reusable tests

### [7.6.0-preview] - 2020-11-04

- Add SetupProject class

## [7.5.1-preview] - 2020-10-07

- Update SetupGraphicsTestCases.cs to support "Player Build: BuildConfiguration" setting for Hybrid scenes

## [7.5.0-preview] - 2020-09-24

- Bump XR Management version from 3.0.6 to 3.2.15

## [7.4.1-preview] - 2020-09-09

- Disabled ImageAssertTests.PerPixelTest on device to avoid issues with TestCaseSource.

## [7.4.0-preview] - 2020-08-27

- Added the ability to test the number of incorrect pixels against a set ratio.
- Added the ability to test the sRGB-encoded color channels against a threshold.
- Added the ability to test the alpha channel against a threshold.

## [7.3.0-preview] - 2020-07-09

- Added optional callback on ImageAssert triggered after all cameras are rendered.

## [7.2.3-preview] - 2020-07-06

- Enable multiple scenes per test filter and clean up UI a bit.
- Fixes a memory allocation in the Profiler.Get function that was counted as memory allocation in the render loop of SRP.

## [7.2.2-preview] - 2020-06-08

- Wrap built in xr checks in 2020_2_OR_NEWER due to built in xr deprecation in 2020.2 and higher.
- Test filter fixes for multiple matching filters

## [7.2.1-preview] - 2020-05-01

- Backwards compatibility to 2019.3

## [7.2.0-preview] - 2020-04-30

- Add the option for tests to use the back buffer instead of rendering to a render texture first
- Fix LoadedXRDevice to use XR SDK first

## [7.1.13-preview] - 2020-04-06

- Update reference versions of json and utp

## [7.1.12-preview] - 2020-03-24

- Bug fix for where all scenes would be baked when only one was selected.
- Bug fix for Xbox where tests would fail due to XR APIs

## [7.1.11-preview] - 2020-03-20

- Fix for OSX Metal automation

## [7.1.10-preview] - 2020-03-20

- Add build targets for DX12 and OSX Metal

## [7.1.9-preview] - 2010-03-19

- Use Standalone XR settings for Editor play mode XR

## [7.1.8-preview] - 2020-03-18

- Fix Test Result Window

## [7.1.7-preview] - 2020-03-17

- Change MockHMD folder to None for playmode

## [7.1.6-preview] - 2020-03-16

- Improved messaging in GC Alloc
- Test filters no longer override disabled tests in build settings
- Adds a check so if vr is supported and that array is empty, set xrsdk to MockHMD

## [7.1.5-preview] - 2020-02-14

- Fixing issues where Standalone tests wouldn't work for some projects

## [7.1.4-preview] - 2020-02-13

- Adding GC Alloc changes for HDRP

## [7.1.3-preview] - 2019-11-25

- Updating dependency names

## [7.1.2-preview] - 2019-11-04

- Adding com.unity.nuget.test-protocol and com.unity.newtonsoft-json as dependencies

## [7.1.1-preview] - 2019-09-23

- Adding script for testing with different Graphics APIs

## [7.1.0-preview] - 2019-09-09

- Separated Graphics Test Framework into its own repository

## [6.6.0] - 2019-04-01

## [6.5.0] - 2019-03-07

## [6.4.0] - 2019-02-21

## [6.3.0] - 2019-02-18

## [6.2.0] - 2019-02-15

## [6.1.0] - 2019-02-13

## [6.0.0] - 2019-02-23

## [5.2.0] - 2018-11-27

## [5.1.0] - 2018-11-18

## [5.0.0-preview] - 2018-09-28

## [4.0.0-preview] - 2019-09-21

## [3.3.0] - 2018-08-03

## [3.2.0] - 2018-07-30

## [3.1.0] - 2018-07-26

## [0.1.0] - 2018-05-04

### This is the first release of *Unity Package com.unity.testframework.graphics*

- ImageAssert for comparing images
- Automatic management of reference images and test case generation
