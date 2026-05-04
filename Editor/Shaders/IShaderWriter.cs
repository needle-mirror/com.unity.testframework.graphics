namespace UnityEngine.TestTools.Graphics.Shaders
{
    interface IShaderWriter
    {
        ShaderHandle WriteShader(string shaderCode);
    }
}
