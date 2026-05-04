using System;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    /// <summary>
    /// Interface for shader loaders
    /// </summary>
    public interface IShaderLoader : IDisposable
    {
        /// <summary>
        /// Loads a shader by its identifier
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the shader to load
        /// </param>
        /// <param name="shaderParams">
        /// Any relevant shaderParams parameters
        /// </param>
        /// <returns>
        /// A handle to the loaded shader
        /// </returns>
        ShaderHandle LoadShader(string identifier, ShaderParams shaderParams);
    }
}
