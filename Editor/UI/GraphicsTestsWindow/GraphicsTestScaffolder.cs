using System.IO;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    /// <summary>
    /// Creates a graphics test folder with an assembly definition and a sample test file.
    /// </summary>
    static class GraphicsTestScaffolder
    {
        const string k_DefaultFolder = "Assets/Tests/GraphicsTests";

        /// <summary>
        /// Creates the graphics test project at <c>Assets/Tests/GraphicsTests</c>.
        /// </summary>
        internal static void CreateDefault()
        {
            Create(k_DefaultFolder);
        }

        /// <summary>
        /// Creates the graphics test scaffold at the given asset-relative path.
        /// </summary>
        internal static void Create(string folderPath)
        {
            var absolutePath = Path.GetFullPath(folderPath);
            Directory.CreateDirectory(absolutePath);
            Directory.CreateDirectory(Path.Combine(absolutePath, "Scenes"));

            var projectName = SanitizeAssemblyName(PlayerSettings.productName);
            if (string.IsNullOrEmpty(projectName))
                projectName = SanitizeAssemblyName(Path.GetFileName(absolutePath));
            if (string.IsNullOrEmpty(projectName))
                projectName = "MyProject";

            var asmdefPath = Path.Combine(absolutePath, "GraphicsTests.asmdef");
            if (!File.Exists(asmdefPath))
                File.WriteAllText(asmdefPath, GenerateAsmdef(projectName));

            var testPath = Path.Combine(absolutePath, "SampleGraphicsTests.cs");
            if (!File.Exists(testPath))
                File.WriteAllText(testPath, GenerateSampleTest(folderPath));

            AssetDatabase.Refresh();

            GraphicsTestLogger.Log(
                $"Created graphics test project at '{folderPath}' with assembly '{projectName}.GraphicsTests'.");
        }

        static string GenerateAsmdef(string projectName)
        {
            return $@"{{
    ""name"": ""{projectName}.GraphicsTests"",
    ""rootNamespace"": """",
    ""references"": [
        ""UnityEngine.TestRunner"",
        ""UnityEditor.TestRunner"",
        ""UnityEngine.TestTools.Graphics"",
        ""UnityEditor.TestTools.Graphics""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": true,
    ""precompiledReferences"": [
        ""nunit.framework.dll""
    ],
    ""autoReferenced"": false,
    ""defineConstraints"": [
        ""UNITY_INCLUDE_TESTS""
    ],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}";
        }

        static string GenerateSampleTest(string folderPath)
        {
            var scenesFolder = folderPath.Replace('\\', '/');
            if (!scenesFolder.EndsWith("/"))
                scenesFolder += "/";
            scenesFolder += "Scenes";

            return $@"using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

/// <summary>
/// Sample graphics test class created by the Graphics Test Framework scaffolder.
/// Replace these examples with your own tests.
/// </summary>
class SampleGraphicsTests
{{
    Texture2D m_ActualImage;

    [SetUp]
    public void SetUp()
    {{
        m_ActualImage = new Texture2D(1, 1, TextureFormat.RGB24, false);
        m_ActualImage.SetPixel(0, 0, Color.red);
        m_ActualImage.Apply();
    }}

    [TearDown]
    public void TearDown()
    {{
        Object.DestroyImmediate(m_ActualImage);
    }}

    [Test, GraphicsTest]
    public void SampleCodeBasedTest(GraphicsTestCase testCase)
    {{
        ImageAssert.AreEqual(testCase.ReferenceImage.Image, m_ActualImage);
    }}

    [UnityTest, SceneGraphicsTest(""{scenesFolder}"")]
    public IEnumerator SampleSceneBasedTest(SceneGraphicsTestCase testCase)
    {{
        SceneManager.LoadScene(testCase.ScenePath, LoadSceneMode.Single);
        yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, m_ActualImage);
    }}
}}
";
        }

        static string SanitizeAssemblyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '.' && chars[i] != '_')
                    chars[i] = '_';
            }

            var result = new string(chars).Trim('_');
            return result;
        }
    }
}
