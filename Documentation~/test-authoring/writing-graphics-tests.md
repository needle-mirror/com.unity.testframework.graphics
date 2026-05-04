# Write a scene test

Write a graphics test that compares a scene to a reference image.

To write a scene test, follow these steps:

1. Create a script that includes the `NUnit.Framework`, `UnityEngine.TestTools.Graphics`, and `UnityEngine.SceneManagement` namespaces.

2. Create a class with a test method that accepts a [`SceneGraphicsTestCase`](xref:UnityEngine.TestTools.Graphics.SceneGraphicsTestCase) parameter. The test framework automatically inputs the test case into the test method.

    For example:

    ```csharp
    using NUnit.Framework;
    using UnityEngine.TestTools.Graphics;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    class MySceneGraphicsTest
    {
        // The test method must accept a SceneGraphicsTestCase parameter
        public IEnumerator MySceneTest(SceneGraphicsTestCase testCase)
        {
        }
    }
    ```
    
3. Add the `[UnityTest]` attribute to the test method, and a [`SceneGraphicsTest`](xref:UnityEngine.TestTools.Graphics.SceneGraphicsTestAttribute) attribute with the path to the folder that contains the scenes you want to test. For example:

     ```csharp
        [UnityTest, SceneGraphicsTest("Assets/Scenes")]
        public IEnumerator MySceneTest(SceneGraphicsTestCase testCase)
        {
            // Test code here
        }
     ```

    The scene path parameter accepts single `.unity` scene files, asset paths, directory paths, or regex patterns. For example, `Assets/Scenes/[0-9]+`.

    Unity generates a `testCase` for each scene in the folder.

4. Load the scene using `SceneManager.LoadScene` with the `ScenePath` property of the `testCase`. For example:

    ```csharp
    SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);

    // Wait a frame for rendering to complete
    yield return null;
    ```

5. Compare the camera view to the reference image in the `testCase` using the [`ImageAssert`](xref:UnityEngine.TestTools.Graphics.ImageAssert) API. For example:

    ```csharp
    // Get the camera view
    Camera camera = Object.FindFirstObjectByType<Camera>();

    // Check the camera view matches the reference image
    ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera);
    ```

The test always fails the first time you run it because there's no reference image yet. For more information, refer to [Run the test for the first time](running-graphics-tests-first-time.md).

## Exclude a scene from testing

To exclude a scene in the `Scenes` folder, use [`[IgnoreGraphicsTest]`](xref:UnityEngine.TestTools.Graphics.IgnoreGraphicsTestAttribute).

**Note**: You can't exclude a scene by removing it from a [build profile](https://docs.unity3d.com/Manual/BuildSettings.html). The Unity Test Framework package doesn't use build profiles to find scenes.

## Example

```csharp
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEngine;
using UnityEngine.SceneManagement;

class MySceneGraphicsTest
{
    [UnityTest, SceneGraphicsTest("Assets/Scenes")]
    public IEnumerator RenderScene(SceneGraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
        yield return null;

        Camera camera = Object.FindFirstObjectByType<Camera>();
        ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera);
    }
}
```

## Additional resources

- [Graphics test workflow](graphics-tests-introduction.md)
- [Reference image optimization](../performance-optimization/reference-image-optimization.md)
- [Graphics Tests window reference](../test-investigation/the-graphics-tests-window.md)




