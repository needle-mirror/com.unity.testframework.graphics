using System.Collections.Generic;
using UnityEditor.TestTools.Graphics;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    class HlslShaderLoader : IShaderLoader
    {
        IAssetService AssetService { get; set; } = new AssetDatabaseService();

        readonly IList<ShaderHandle> m_GeneratedShaders = new List<ShaderHandle>();
        HlslShaderWrapperGenerator ShaderWrapperGenerator { get; set; }
        IShaderWriter ShaderWriter { get; set; } = new ShaderWriter();

        public ShaderHandle LoadShader(string identifier, ShaderParams shaderParams)
        {
            var hlslShaderParams =
                shaderParams as HlslShaderParams ?? throw new System.ArgumentNullException(nameof(shaderParams));

            var dependency = new ShaderHandle(identifier, ShaderType.Hlsl);
            ShaderWrapperGenerator = new HlslShaderWrapperGenerator(
                dependency,
                hlslShaderParams.m_ReturnType,
                hlslShaderParams.m_Function
            );
            var wrapper = ShaderWrapperGenerator.GenerateShaderWrapper();
            var handle = ShaderWriter.WriteShader(wrapper);

            m_GeneratedShaders.Add(handle);
            return handle;
        }

        public void Dispose()
        {
            foreach (var handle in m_GeneratedShaders)
            {
                if (AssetService.AssetPathExists(handle.relativePath))
                {
                    AssetService.DeleteAsset(handle.relativePath);
                }
            }
        }
    }
}
