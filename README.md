# About Graphics Test Framework

Use the Graphics Test Framework to automate visual validation of rendering: compare captured output against reference images, catch regressions, and test across platforms, graphics APIs, and custom Scriptable Render Pipelines.

# Installing

Add this package with the [Package Manager](https://docs.unity3d.com/Manual/upm-ui.html). It depends on the Unity Test Framework and other packages listed in `package.json`.

# Using Graphics Test Framework

- Full documentation: [Graphics Test Framework](https://docs.unity3d.com/Packages/com.unity.testframework.graphics@latest/index.html).
- In the Editor, open **Window** > **General** > **Graphics Tests** to create tests, manage reference images, and inspect results.
- Optional **Samples** in the Package Manager (Basic Graphics Tests, Parameterized Tests, Image Comparison Examples, and more) show common patterns.

# Technical details

## Requirements

This package is compatible with:

* Unity **6000.0** and later (see `package.json` for the exact `unity` / `unityRelease` requirement).

## Package contents

| Location | Description |
| --- | --- |
| `Editor` | Editor tools: Graphics Tests window, test build and filtering, image utilities, and related services. |
| `Runtime` | Runtime APIs and attributes for authoring graphics tests (e.g. scene and code-based tests, image comparison helpers). |
| `Documentation~` | Package documentation source (the `~` folder is not imported into Unity projects as assets). |
| `Samples~` | Optional sample projects you can import from the Package Manager. |

## Document revision history

| Date | Reason |
| --- | --- |
| April 22, 2026 | Package README updated to match version 9. |
| January 25, 2019 | Initial document. |
