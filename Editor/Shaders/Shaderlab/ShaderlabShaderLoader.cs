using System.IO;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    class ShaderlabShaderLoader : IShaderLoader
    {
        public ShaderHandle LoadShader(string path, ShaderParams preparation)
        {
            var relativePath = Path.ChangeExtension(path, null);
            return new ShaderHandle(path, ShaderType.ShaderLab, relativePath: relativePath);
        }

        public void Dispose() { }
    }
}
