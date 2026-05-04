using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    class TextureImporterWrapper : ITextureImporter
    {
        readonly TextureImporter m_Inner;

        public TextureImporterWrapper(TextureImporter inner)
        {
            m_Inner = inner;
        }

        public bool isReadable
        {
            get => m_Inner.isReadable;
            set => m_Inner.isReadable = value;
        }

        public bool sRGBTexture
        {
            get => m_Inner.sRGBTexture;
            set => m_Inner.sRGBTexture = value;
        }

        public TextureImporterCompression textureCompression
        {
            get => m_Inner.textureCompression;
            set => m_Inner.textureCompression = value;
        }

        public bool mipmapEnabled
        {
            get => m_Inner.mipmapEnabled;
            set => m_Inner.mipmapEnabled = value;
        }

        public FilterMode filterMode
        {
            get => m_Inner.filterMode;
            set => m_Inner.filterMode = value;
        }

        public TextureImporterNPOTScale npotScale
        {
            get => m_Inner.npotScale;
            set => m_Inner.npotScale = value;
        }

        public TextureWrapMode wrapMode
        {
            get => m_Inner.wrapMode;
            set => m_Inner.wrapMode = value;
        }

        public void SaveAndReImport() => m_Inner.SaveAndReimport();

        public void SaveAndReimport() => m_Inner.SaveAndReimport();
    }
}
