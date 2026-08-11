using System.IO;
#if UNITY_TEST_PROTOCOL
using Unity.TestProtocol;
using Unity.TestProtocol.Messages;
#endif
using UnityEngine.Networking.PlayerConnection;
#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics
{
#if UNITY_EDITOR
    /// <summary>
    /// Handles image events from the Unity Test Protocol.
    /// </summary>
    public class ImageHandler : ScriptableSingleton<ImageHandler>
    {
        /// <summary>
        /// The path where images will be saved.
        /// </summary>
        public string ImageResultsPath { get; set; }

        internal Action<string> ArtifactReporter { get; set; }

        internal void HandleImageEvent(MessageEventArgs messageEventArgs)
        {
            var imageMessage = ImageMessage.Deserialize(messageEventArgs.data);
            SaveImage(imageMessage);
        }

        /// <summary>
        /// Settings for the texture importer.
        /// </summary>
        public class TextureImporterSettings
        {
            /// <summary>
            /// Whether the texture is readable.
            /// </summary>
            public bool IsReadable { get; set; } = true;

            /// <summary>
            /// Whether to use mipmaps.
            /// </summary>
            public bool UseMipMaps { get; set; } = false;

            /// <summary>
            /// The NPOT scale for the texture.
            /// </summary>
            public TextureImporterNPOTScale NPOTScale { get; set; } = TextureImporterNPOTScale.None;

            /// <summary>
            /// The texture compression type.
            /// </summary>
            public TextureImporterCompression TextureCompressionType { get; set; } =
                TextureImporterCompression.Uncompressed;

            /// <summary>
            /// The filter mode for the texture.
            /// </summary>
            public FilterMode TextureFilterMode { get; set; } = FilterMode.Point;
        }

        /// <summary>
        /// Re-imports the texture with the specified settings.
        /// </summary>
        /// <param name="path">
        /// The path to the texture.
        /// </param>
        /// <param name="settings">
        /// The settings to apply to the texture importer.
        /// </param>
        public static void ReImportTextureWithSettings(string path, TextureImporterSettings settings)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
                return;
            importer.isReadable = settings.IsReadable;
            importer.npotScale = settings.NPOTScale;
            importer.mipmapEnabled = settings.UseMipMaps;
            importer.textureCompression = settings.TextureCompressionType;
            importer.filterMode = settings.TextureFilterMode;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Saves the image to the specified path.
        /// </summary>
        /// <param name="imageMessage">
        /// The image message containing the image data.
        /// </param>
        /// <param name="extension">
        /// The file extension (without the leading dot) to save with. Null or empty defaults to "png".
        /// </param>
        /// <param name="textureImporterSettings">
        /// The settings to apply to the texture importer.
        /// </param>
        public void SaveImage(
            ImageMessage imageMessage,
            string extension = "png",
            TextureImporterSettings textureImporterSettings = null
        )
        {
            SaveImage(imageMessage, IsAutomatedRun, extension, textureImporterSettings);
        }

        /// <summary>
        /// Saves the image to the specified path.
        /// </summary>
        /// <param name="imageMessage">
        /// The image message containing the image data.
        /// </param>
        /// <param name="isAutomatedRun">
        /// When true, a save directory under Assets/ is redirected to a '~' folder the AssetDatabase ignores.
        /// </param>
        /// <param name="extension">
        /// The file extension (without the leading dot) to save with. Null or empty defaults to "png".
        /// </param>
        /// <param name="textureImporterSettings">
        /// The settings to apply to the texture importer.
        /// </param>
        public void SaveImage(
            ImageMessage imageMessage,
            bool isAutomatedRun,
            string extension = "png",
            TextureImporterSettings textureImporterSettings = null
        )
        {
            if (string.IsNullOrEmpty(extension))
                extension = "png";

            var reportArtifact = ArtifactReporter ?? ReportArtifact;
            var saveDir = string.IsNullOrEmpty(ImageResultsPath) ? imageMessage.PathName : ImageResultsPath;
            if (isAutomatedRun)
                saveDir = ToImportIgnoredPath(saveDir);

            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            var actualImagePath = Path.Combine(saveDir, $"{imageMessage.ImageName}.{extension}");

            if (File.Exists(actualImagePath))
            {
                File.Delete(actualImagePath);
                GraphicsTestLogger.Log(LogType.Warning, $"Overwriting existing image at path: {actualImagePath}");
            }

            File.WriteAllBytes(actualImagePath, imageMessage.ActualImage);
            reportArtifact(actualImagePath);
            if (textureImporterSettings != null)
                ReImportTextureWithSettings(actualImagePath, textureImporterSettings);

            if (imageMessage.DiffImage != null)
            {
                var diffImagePath = Path.Combine(saveDir, $"{imageMessage.ImageName}.diff.{extension}");
                File.WriteAllBytes(diffImagePath, imageMessage.DiffImage);
                reportArtifact(diffImagePath);
                if (textureImporterSettings != null)
                    ReImportTextureWithSettings(diffImagePath, textureImporterSettings);
            }

            if (imageMessage.ExpectedImage != null)
            {
                var expectedImagesPath = Path.Combine(saveDir, $"{imageMessage.ImageName}.expected.{extension}");
                File.WriteAllBytes(expectedImagesPath, imageMessage.ExpectedImage);
                reportArtifact(expectedImagesPath);
                if (textureImporterSettings != null)
                    ReImportTextureWithSettings(expectedImagesPath, textureImporterSettings);
            }
        }

        static bool? s_IsAutomatedRun;

        internal static bool IsAutomatedRun =>
            s_IsAutomatedRun ??= ComputeIsAutomatedRun(Application.isBatchMode, RuntimeSettings.CommandLineReader);

        // UTR runs graphics playmode editors windowed (rendering needs the GPU), so batch mode alone is not enough
        internal static bool ComputeIsAutomatedRun(bool isBatchMode, CommandLineReader commandLine) =>
            isBatchMode || commandLine.CommandLineArgumentExists("-automated");

        // '~' folder: the AssetDatabase skips it, avoiding a post-run import of every result image
        internal static string ToImportIgnoredPath(string saveDir)
        {
            var normalized = saveDir.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                return saveDir;

            var topFolderEnd = normalized.IndexOf('/', "Assets/".Length);
            if (topFolderEnd < 0)
                topFolderEnd = normalized.Length;
            if (normalized[topFolderEnd - 1] == '~')
                return normalized;

            return normalized.Substring(0, topFolderEnd) + "~" + normalized.Substring(topFolderEnd);
        }

        private void ReportArtifact(string artifactPath)
        {
            var fullpath = Path.GetFullPath(artifactPath);
#if UNITY_TEST_PROTOCOL
            var message = ArtifactPublishMessage.Create(fullpath, true);
            GraphicsTestLogger.Log(LogType.Log, "[ImageHandler] Received Artifact: " + fullpath);
            Debug.Log(UnityTestProtocolMessageBuilder.Serialize(message));
#else
            GraphicsTestLogger.DebugWarning($"[ImageHandler] Artifact publishing requires com.unity.external.test-protocol. Please install the package to enable this feature. Received artifact path: {fullpath}");
#endif
        }
    }
#endif // UNITY_EDITOR
}
