using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Represents a reference image to use for a test case.
    /// </summary>
    public sealed record ReferenceImage
    {
        /// <summary>
        /// Creates a new reference image with the specified name, texture format, and extension.
        /// </summary>
        /// <param name="name">
        /// The name of the reference image.
        /// </param>
        /// <param name="textureFormat">
        /// The type of texture to use for the reference image.
        /// </param>
        /// <param name="imageExtension">
        /// The extension of the reference image. If not set, the default is <see cref="ImageExtension.PNG"/>.
        /// </param>
        public ReferenceImage(
            string name,
            TextureFormat textureFormat = default,
            ImageExtension imageExtension = ImageExtension.PNG
        )
        {
            Init(name, textureFormat, imageExtension);
        }

        /// <summary>
        /// The name of the reference image.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The extension of the reference image. If not set, the default is <see cref="ImageExtension.PNG"/>.
        /// </summary>
        /// <remarks>
        /// This is only used for the reference image asset path.
        /// </remarks>
        public ImageExtension ImageExtension { get; private set; } = ImageExtension.PNG;

        /// <summary>
        /// The type of texture to use for the reference image.
        /// </summary>
        /// <remarks>
        /// This is used to determine how the reference image is to be loaded and used.
        /// </remarks>
        public TextureFormat TextureFormat { get; private set; }

        /// <summary>
        /// The path to the reference image asset. (Editor only)
        /// </summary>
        public string AssetPath
        {
            get => m_AssetPath ??= TestContentLoader.ContentLoader.AssetPath($"{Name}.{ImageExtension.ToLowerCase()}");
            internal init => m_AssetPath = value;
        }

        string m_AssetPath;

        /// <summary>
        /// The load message for the reference image, indicating any errors that occurred during loading.
        /// </summary>
        public string LoadMessage => m_LoadMessage;
        string m_LoadMessage;

        /// <summary>
        /// The reference image texture to use for the test case.
        /// </summary>
        /// <remarks>
        /// This is the texture that will be used for the test case.
        /// It is loaded from the asset path and may be post-processed if necessary.
        /// </remarks>
        public Texture2D Image => LoadImage();
        Texture2D m_LoadedImage;

        Texture2D LoadImage()
        {
            if (!TestContentLoader.ContentLoader.ShouldReloadAssets && m_LoadedImage != null)
            {
                GraphicsTestLogger.DebugLog($"Reference image '{Name}' has already been loaded.");
                return m_LoadedImage;
            }

            var image = TestContentLoader.ContentLoader.Load<Texture2D>(
                $"{Name}.{ImageExtension.ToLowerCase()}",
                out m_LoadMessage
            );

            GraphicsTestLogger.DebugLog(m_LoadMessage);

            if (image == null)
            {
                return null;
            }

            var previousImage = m_LoadedImage;
            var rawBundleImage = image;
            m_LoadedImage = image;

            // If the user requested a different texture format, honor the request
            if (image.format != TextureFormat && TextureFormat > 0)
            {
                GraphicsTestLogger.DebugLog(
                    $"Reference image '{Name}' is in format {image.format}, reloading as {TextureFormat}."
                );
                var converted = ReloadImageWithTextureFormat(image, TextureFormat);
                m_LoadedImage = converted;
                image = converted;
            }

            // Only destroy previousImage if it was a converted texture we created,
            // never the raw bundle asset. Compare against rawBundleImage (the asset
            // just loaded from the bundle) to avoid destroying bundle-owned textures.
            if (previousImage != null && previousImage != m_LoadedImage && previousImage != rawBundleImage)
                Object.Destroy(previousImage);

            return image;
        }

        static Texture2D ReloadImageWithTextureFormat(Texture2D texture, TextureFormat format)
        {
            var tex = new Texture2D(texture.width, texture.height, format, false, true);
            tex.SetPixels(texture.GetPixels());
            tex.Apply();
            return tex;
        }

        void Init(string name, TextureFormat textureFormat, ImageExtension extension)
        {
            if (!Enum.IsDefined(typeof(ImageExtension), extension))
                throw new ArgumentException(
                    $"Extension {extension} must be a valid ImageExtension value.",
                    nameof(extension)
                );

            Name = name;
            TextureFormat = textureFormat;
            ImageExtension = extension;
            m_LoadMessage = $"There has been no attempt to load the reference image '{name}'.";
        }
    }
}
