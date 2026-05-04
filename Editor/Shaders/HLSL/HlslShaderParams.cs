using System;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    /// <summary>
    /// Shader parameters for HLSL shader execution
    /// </summary>
    public sealed class HlslShaderParams : ShaderParams
    {
        internal readonly string m_Function;
        internal readonly Type m_ReturnType;

        /// <summary>
        /// Creates a new instance of HlslShaderParams
        /// </summary>
        /// <param name="function">
        /// The function in test. This should be written in full,
        /// exactly as it would appear in HLSL, including any parameter values.
        /// For example, write "function()" or "function(1,2)"
        /// </param>
        /// <param name="returnType">
        /// The expected return type for the function in test.
        /// The return values should be as follows, based on the HLSL return type:
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
        /// </param>
        public HlslShaderParams(string function, Type returnType)
        {
            m_Function = function;
            m_ReturnType = returnType;
        }
    }
}
