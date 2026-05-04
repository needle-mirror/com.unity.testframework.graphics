using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    interface ITextureImporter : IAssetImporter
    {
        bool isReadable { get; set; }
        bool sRGBTexture { get; set; }
        TextureImporterCompression textureCompression { get; set; }
        bool mipmapEnabled { get; set; }
        FilterMode filterMode { get; set; }
        TextureImporterNPOTScale npotScale { get; set; }
        TextureWrapMode wrapMode { get; set; }
        void SaveAndReImport();
    }

    static class AssetImporterWrapperFactory
    {
        internal static IAssetImporter GetImporterAtPath(AssetImporter importer)
        {
            return importer switch
            {
                TextureImporter textureImporter => new TextureImporterWrapper(textureImporter),
                _ => null,
            };
        }
    }
}
