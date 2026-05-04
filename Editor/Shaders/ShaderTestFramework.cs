using System;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    /// <summary>
    /// Framework for shader testing.
    /// </summary>
    /// <remarks>
    /// Manages loading, executing, and resource cleanup for shaders under test.
    /// </remarks>
    public class ShaderTestFramework : IDisposable
    {
        readonly Dictionary<Type, IShaderLoader> m_ShaderLoaders = new();

        /// <summary>
        /// Loads a shader by its identifier and type.
        /// </summary>
        /// <param name="path">
        /// The path of the shader to load
        /// </param>
        /// <param name="shaderParameters">
        /// The preparation parameters for the shader.
        /// </param>
        /// <returns>
        /// A handle to the loaded shader
        /// </returns>
        /// <remarks>
        /// Currently supports loading ShaderLab and HLSL shaders.
        /// </remarks>
        /// <exception cref="NotImplementedException">
        /// Thrown when the shader type is not supported.
        /// </exception>
        public ShaderHandle LoadShader(string path, ShaderParams shaderParameters)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var shaderType = extension switch
            {
                ".shader" => typeof(ShaderlabShaderLoader),
                ".hlsl" => typeof(HlslShaderLoader),
                _ => throw new NotImplementedException($"Shader loader for {extension} is not implemented."),
            };

            if (m_ShaderLoaders.TryGetValue(shaderType, out var loader))
                return loader.LoadShader(path, shaderParameters);

            loader = Activator.CreateInstance(shaderType) as IShaderLoader
                ?? throw new Exception($"Failed to create shader loader for {path}.");
            m_ShaderLoaders[shaderType] = loader;
            return loader.LoadShader(path, shaderParameters);
        }

        /// <summary>
        /// Executes the given shader and returns the result
        /// </summary>
        /// <typeparam name="T">
        /// The type of the shader result data.
        /// This must match the expected return type of the function or shader being executed.
        /// <br/>For Shaderlab, always use <see cref="ShaderlabShaderData"/>.
        /// <br/>For HLSL, use the following types:
        /// <list type="table">
        ///   <listheader>
        ///     <term>HLSL Type</term>
        ///     <description>C# Type</description>
        ///   </listheader>
        ///   <item>
        ///     <term>float</term>
        ///     <description>float</description>
        ///   </item>
        ///   <item>
        ///     <term>float2,int2</term>
        ///     <description>Vector2</description>
        ///   </item>
        ///   <item>
        ///     <term>float3,int3</term>
        ///     <description>Vector3</description>
        ///   </item>
        ///   <item>
        ///     <term>float4,int4</term>
        ///     <description>Vector4</description>
        ///   </item>
        ///   <item>
        ///     <term>int</term>
        ///     <description>int</description>
        ///   </item>
        ///   <item>
        ///     <term>uint</term>
        ///     <description>uint</description>
        ///   </item>
        ///   <item>
        ///     <term>float4x4</term>
        ///     <description>Matrix4x4</description>
        ///   </item>
        /// </list>
        /// </typeparam>
        /// <param name="shader">
        /// The shader to execute.
        /// </param>
        /// <returns>
        /// The shader execution result, in the type of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when the shader type is not supported. Currently supported shader types are HLSL and Shaderlab.
        /// </exception>
        public T ExecuteShader<T>(ShaderHandle shader)
        {
            switch (shader.type)
            {
                case ShaderType.ShaderLab:
                {
                    return (T)(object)new ShaderlabShaderExecutor<ShaderlabShaderData>().ExecuteShader(shader);
                }
                case ShaderType.Hlsl:
                {
                    return new HlslShaderExecutor<T>().ExecuteShader(shader);
                }
                case ShaderType.ShaderGraph:
                case ShaderType.Cg:
                case ShaderType.Compute:
                case ShaderType.RayTracing:
                default:
                    throw new NotImplementedException($"Shader type {shader.type} not supported.");
            }
        }

        /// <summary>
        /// Disposes the shader test framework, resetting state and releasing resources
        /// </summary>
        public void Dispose()
        {
            foreach (var shaderLoader in m_ShaderLoaders.Values)
            {
                try
                {
                    shaderLoader.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            m_ShaderLoaders.Clear();
        }
    }
}
