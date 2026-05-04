using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    abstract class ShaderWrapperGenerator
    {
        protected ShaderHandle ShaderHandle { get; }
        protected Dictionary<string, object> ShaderProperties { get; } = new();
        protected IList<string> Dependencies { get; } = new List<string>();
        protected string[] Arrange { get; } = Array.Empty<string>();
        protected string Act { get; }

        public ShaderWrapperGenerator(ShaderHandle shaderHandle, string act)
        {
            ShaderHandle = shaderHandle;
            Act = act;

            Dependencies.Add(shaderHandle.path);
        }

        public abstract string GenerateShaderWrapper();
    }
}
