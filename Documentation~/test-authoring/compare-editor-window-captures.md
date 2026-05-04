# Write a Unity Editor window test

To test UI rendering and custom Unity Editor windows, write a test that captures the Unity Editor interface.

For more information about using a test after you write it, refer to [Run a graphics test for the first time](running-graphics-tests-first-time.md) and [Run a test and check the results](running-graphics-tests.md).

## Capture an Editor window

To capture an Editor window as a texture, use the [`EditorWindowCapture.CaptureAsync`](xref:UnityEditor.TestTools.Graphics.EditorWindowCapture.CaptureAsync(UnityEditor.EditorWindow,UnityEditor.TestTools.Graphics.EditorWindowCaptureSettings)) API. Follow these steps:

1. Create an `EditorWindow` object with the window you want to capture. For example:

    ```csharp
    EditorWindow window = EditorWindow.CreateWindow<SceneView>();
    ```

2. Create capture settings using [`EditorWindowCaptureSettings`](xref:UnityEditor.TestTools.Graphics.EditorWindowCaptureSettings). For example:

    ```csharp
    EditorWindowCaptureSettings settings = new EditorWindowCaptureSettings(
        width: 1024,
        height: 768,
        delayBeforeCapture: TimeSpan.FromSeconds(0.5)
    );
    ```

    **Note:** You can use `EditorWindowCaptureSettings.Default` for a default 512 &times; 512 capture with no delay. 

3. Use the `EditorWindowCapture.CaptureAsync` API to capture the window. For example:

    ```csharp
    Texture2D capture = await EditorWindowCapture.CaptureAsync(window, settings);
    ```

You can also pass in actions to the window before you capture it using a `(window)` setup action. For example the following changes the window title:

```csharp
EditorWindowCaptureSettings settings = new EditorWindowCaptureSettings(
    ...
    (window) => { window.titleContent = new GUIContent("Test Window"); }
)
```

For more information, refer to [Asynchronous tests](https://docs.unity3d.com/Manual/test-framework/reference-async-tests.html).

## Capture the Scene view

If you capture the Scene view window, you can use a [`SceneViewCaptureSettings`](xref:UnityEditor.TestTools.Graphics.SceneViewCaptureSettings) object instead of an `EditorWindowCaptureSettings` object, and set two additional settings:

- `ImageComparisonViewpoint`: Sets the Scene view camera transform before capture to ensure a consistent viewpoint.
- `Timeout`: Sets a timeout for async operations such as shader compilation, to avoid indefinite waits.

You can use `SceneViewCaptureSettings.Default` to create settings for a standard Scene view capture.

## Additional resources

- [Write a scene test](writing-graphics-tests.md)
- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)
- [Run a graphics test for the first time](running-graphics-tests-first-time.md)
