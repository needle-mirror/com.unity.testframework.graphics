using System.IO;
using System.Threading;
using UnityEditor.TestTools.Graphics;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    class ShaderWriter : IShaderWriter
    {
        IAssetService AssetService { get; set; } = new AssetDatabaseService();

        static int s_ShaderCounter;

        static string GetTestShaderPath()
        {
            var id = Interlocked.Increment(ref s_ShaderCounter);
            return Path.Combine("Assets", $"TestShader_{id}.compute");
        }

        public ShaderHandle WriteShader(string shaderCode)
        {
            var testShaderPath = GetTestShaderPath();
            var generatedShaderPath = Path.Combine(Directory.GetCurrentDirectory(), testShaderPath);
            File.WriteAllText(generatedShaderPath, shaderCode);
            AssetService.ImportAsset(testShaderPath);

            return new ShaderHandle(generatedShaderPath, ShaderType.Hlsl, testShaderPath, true);
        }
    }
}
