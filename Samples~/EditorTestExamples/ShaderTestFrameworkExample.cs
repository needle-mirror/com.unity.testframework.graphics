using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Graphics.Shaders;

namespace GraphicsTestFrameworkProject.Samples.EditorTestExamples
{
    /*
    -- Tutorial [1] --
    ShaderTestFramework provides a structured API for loading and executing
    shaders in tests. It supports both ShaderLab (.shader) and HLSL (.hlsl) files.

    The workflow is:
      1. Create a ShaderTestFramework instance (implements IDisposable)
      2. Load a shader via LoadShader(), getting a ShaderHandle
      3. Execute the shader via ExecuteShader<T>(), specifying the return type
      4. Assert on the result
      5. Dispose the framework to clean up generated files

    The framework automatically generates wrapper shaders for HLSL functions
    so individual functions can be unit-tested in isolation.

    This sample ships with its own shader files (Shaders/ subfolder) and copies
    them to a temporary Assets location at setup time to avoid collisions with
    any existing project shaders.
    */

    [Category("Samples")]
    [TestOf(nameof(ShaderTestFrameworkExample))]
    internal class ShaderTestFrameworkExample
    {
        const string k_TempShaderFolder = "Assets/Temp_ShaderTestFrameworkSamples";
        const string k_SampleHlslName = "SampleHLSLShader.hlsl";
        const string k_SampleShaderlabName = "SampleShaderlabShader.shader";

        string hlslShaderPath;
        string shaderlabShaderPath;
        ShaderTestFramework shaderTestFramework;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            shaderTestFramework = new ShaderTestFramework();

            var sampleShadersDir = FindSampleShadersDirectory();
            Assume.That(sampleShadersDir, Is.Not.Null,
                "Could not locate the sample Shaders/ directory. Ensure the Editor Test Examples sample is imported.");

            if (!Directory.Exists(k_TempShaderFolder))
                Directory.CreateDirectory(k_TempShaderFolder);

            hlslShaderPath = Path.Combine(k_TempShaderFolder, k_SampleHlslName);
            shaderlabShaderPath = Path.Combine(k_TempShaderFolder, k_SampleShaderlabName);

            File.Copy(Path.Combine(sampleShadersDir, k_SampleHlslName), hlslShaderPath, true);
            File.Copy(Path.Combine(sampleShadersDir, k_SampleShaderlabName), shaderlabShaderPath, true);

            AssetDatabase.Refresh();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            shaderTestFramework.Dispose();

            if (AssetDatabase.IsValidFolder(k_TempShaderFolder))
                AssetDatabase.DeleteAsset(k_TempShaderFolder);
        }

        static string FindSampleShadersDirectory()
        {
            var guids = AssetDatabase.FindAssets("SampleHLSLShader t:TextAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(k_SampleHlslName))
                    return Path.GetDirectoryName(path);
            }

            var searchPaths = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(k_SampleHlslName));
            foreach (var guid in searchPaths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("EditorTestExamples") && path.EndsWith(k_SampleHlslName))
                    return Path.GetDirectoryName(path);
            }

            return null;
        }

        /*
        -- Tutorial [2] --
        Testing an HLSL function that returns a scalar float.
        HlslShaderParams takes the function call string (exactly as written in HLSL)
        and the expected C# return type.

        Type mapping:
          HLSL float     -> C# float
          HLSL int       -> C# int
          HLSL uint      -> C# uint
          HLSL float2    -> C# Vector2
          HLSL float3    -> C# Vector3
          HLSL float4    -> C# Vector4
          HLSL float4x4  -> C# Matrix4x4
        */

        [Test]
        [Description("Loads an HLSL shader and executes a float-returning function.")]
        public void Hlsl_FloatFunction_ReturnsExpected()
        {
            var shaderParams = new HlslShaderParams("SampleFloatFunction()", typeof(float));

            var shader = shaderTestFramework.LoadShader(hlslShaderPath, shaderParams);
            var result = shaderTestFramework.ExecuteShader<float>(shader);

            Assert.That(result, Is.EqualTo(1.0f).Within(0.001f));
        }

        /*
        -- Tutorial [3] --
        Testing an HLSL function that returns a vector type.
        The function string should include parameter values as they would appear in HLSL.
        */

        [Test]
        [Description("Loads an HLSL shader and executes a Vector3-returning function.")]
        public void Hlsl_Vector3Function_ReturnsExpected()
        {
            var shaderParams = new HlslShaderParams("SampleFloat3Function()", typeof(Vector3));

            var shader = shaderTestFramework.LoadShader(hlslShaderPath, shaderParams);
            var result = shaderTestFramework.ExecuteShader<Vector3>(shader);

            Assert.That(result, Is.EqualTo(Vector3.one));
        }

        /*
        -- Tutorial [4] --
        Testing a ShaderLab shader.
        ShaderLab shaders use ShaderlabShaderParams (no function targeting) and
        return ShaderlabShaderData containing the Fragment output as a Texture2D
        and Vertex positions as Vector3[].
        */

        [Test]
        [Description("Loads a ShaderLab shader and inspects its fragment output.")]
        public void Shaderlab_FragmentOutput_HasExpectedPixel()
        {
            // ShaderLab path must match the shader's internal name (Shader "Name") + .shader
            var shader = shaderTestFramework.LoadShader("SampleTest/ShaderlabShader.shader", new ShaderlabShaderParams());
            var result = shaderTestFramework.ExecuteShader<ShaderlabShaderData>(shader);

            Assert.That(result.Fragment, Is.Not.Null, "Fragment output should be a valid Texture2D");

            var pixel = (Vector4)result.Fragment.GetPixel(0, 0);
            Assert.That(pixel, Is.EqualTo(new Vector4(1, 1, 1, 1)));
        }

        /*
        -- Tutorial [5] --
        Parameterized HLSL function testing.
        Use NUnit [TestCase] to test the same function with different inputs.
        The function call string can include literal parameter values.
        */

        [Test]
        [TestCase("SampleIntAdd(1, 2)", 3)]
        [TestCase("SampleIntAdd(0, 0)", 0)]
        [TestCase("SampleIntAdd(-1, 1)", 0)]
        [Description("Tests an HLSL function with multiple input combinations.")]
        public void Hlsl_ParameterizedFunction_ReturnsExpected(string functionCall, int expectedResult)
        {
            var shaderParams = new HlslShaderParams(functionCall, typeof(int));

            var shader = shaderTestFramework.LoadShader(hlslShaderPath, shaderParams);
            var result = shaderTestFramework.ExecuteShader<int>(shader);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        /*
        -- Tutorial [6] --
        Error handling: ShaderTestFramework throws NotImplementedException for
        unsupported shader types (ShaderGraph, Compute, RayTracing, Cg).
        Always wrap in using or dispose explicitly to clean up generated wrapper shaders.
        */

        [Test]
        [Description("Demonstrates that unsupported shader extensions throw.")]
        public void UnsupportedShaderType_ThrowsException()
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                shaderTestFramework.LoadShader("Unsupported.compute", new ShaderlabShaderParams());
            });
        }
    }
}
