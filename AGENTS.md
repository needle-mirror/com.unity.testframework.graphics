# Graphics Test Framework

`com.unity.testframework.graphics` is a public package, published to the registry and documented on
docs.unity3d.com. Changes here reach external users and every in-repo graphics test project.

A graphics test renders a scene and compares the result against a stored reference image. The
framework decides which tests exist, builds the content a player needs to run them, captures the
image, picks the reference to compare against, and reports the difference.

Two assemblies: `Runtime/` (`UnityEngine.TestTools.Graphics`, runs in players) and `Editor/`
(`UnityEditor.TestTools.Graphics`).

| Path | Responsibility |
|------|----------------|
| `Runtime/Attributes` | `SceneGraphicsTest`, `IgnoreGraphicsTest` and friends — how a test is declared and skipped. |
| `Runtime/TestCases` | Turning those attributes into the test cases NUnit runs. |
| `Runtime/ImageComparison` | Capture and comparison. `ImageAssert` is the entry point; algorithms derive from `TextureComparisonAlgorithm`. |
| `Runtime/Platforms` | Platform nodes and the schema resolution that maps a running platform to a reference-image directory. |
| `Runtime/GlobalContext` | Configuration from outside the project: `CommandLineReader`, `-testSettingsFile`, global contexts. |
| `Runtime/Settings` | `GraphicsTestBuildSettings`, the per-project settings asset. |
| `Runtime/PlayerContent` | Loading scenes, reference images and test data in a built player. |
| `Editor/Builder` | Building test players and the content bundles they load. |
| `Editor/UI` | The Graphics Tests window. |

* Tests for changes in this package belong in `Tests/SRPTests/Projects/GraphicsTestFramework_Tests`,
  not in a consumer test project. That project's `Assets/Tests/Editor/Harness/` already provides
  `GraphicsTestBuildSettingsScope`, `TempAssetTreeScope` and `PlatformTupleScope` for tests that need
  to mutate settings, write temporary assets, or pin a platform tuple.

* A new setting that changes test behaviour must be overridable without editing the settings asset —
  either a command-line argument wired through `GraphicsTestBuildSettings.OverwriteSettingsFromCommandLine`,
  or a key read from `-testSettingsFile` through `TestSettingsReader`. Read arguments with
  `CommandLineReader` rather than `Environment.GetCommandLineArgs`.

* Prefer an opt-in over changing a default. A default that changes for everyone affects all graphics
  test projects and all external consumers at once.

* Comparison algorithms derive from `TextureComparisonAlgorithm` and are resolved through that
  abstraction. Don't enumerate or switch on the concrete algorithms.

* Public fields and properties are API. Remove or rename one and consumers break; keep the old member
  as a forwarding shim instead. The package is not covered by automated API validation, so nothing
  will catch this for you.

Repository-wide conventions: [AGENTS.md](../../../../AGENTS.md) at the repo root.
Contribution and publishing process:
[Contributions.md](../../../../Documentation/InternalDocs/docs/TestAutomation/test-development/graphics-testing/legacy-utr-runner/Contributions.md).
