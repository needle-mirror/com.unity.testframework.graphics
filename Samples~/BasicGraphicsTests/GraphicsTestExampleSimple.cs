using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

class GraphicsTestExampleSimple
{
    Texture2D actualImage;

    [SetUp]
    public void SetUp()
    {
        actualImage = new Texture2D(1, 1, TextureFormat.RGB24, false);
        actualImage.SetPixel(0, 0, Color.red);
        actualImage.Apply();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(actualImage);
    }

    [Test, GraphicsTest]
    public void SimpleExample_GraphicsTest(GraphicsTestCase testCase)
    {
        ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
    }

    [UnityTest, SceneGraphicsTest("Assets/Scenes")]
    public IEnumerator SimpleExample_SceneGraphicsTest(SceneGraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
        yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, actualImage);
    }
}
