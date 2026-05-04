using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    ///  Exports expected, actual and difference images to disk or as player artifact based on the logic that was historically used in the ImageAssert.
    ///  Methods that actually writes the files can be overriden so that they can be mocked in tests.
    /// </summary>
    class LegacyComparisonImageExporter
    {
        static string ActualImagePath => GraphicsTestBuildSettings.LoadOrDefault().ActualImagesPath;
        const string k_ActualImagesFolderName = "ActualImages";

        internal virtual void WriteImages(
            Texture2D actual,
            Texture2D expected,
            Color32[] diffPixels,
            bool comparisonIsSuccess,
            LegacyImageExportOptions options
        )
        {
            ValidateArguments(actual, options);

            var shouldSaveImageOnSuccess = options.SaveActualImageOnSuccess && comparisonIsSuccess;
            var shouldSaveImageOnFailure =
                !comparisonIsSuccess && (options.SaveImagesOnFailure || options.SaveImagesToDiskOnFailure);

            if (!shouldSaveImageOnSuccess && !shouldSaveImageOnFailure)
            {
                return;
            }

            var actualExrFormat = GetActualExrFormat(options.FileExtension, actual.format);
            var imageMessage = new ImageMessage
            {
                PathName = FindImageDirectoryName(),
                ImageName = options.ActualImageFileName ?? FindImageName(),
                ActualImage = Encode(actual, options.FileExtension, actualExrFormat),
            };

            if (shouldSaveImageOnSuccess)
            {
                SaveImage(imageMessage, options.FileExtension);
            }
            else
            {
                if (expected != null && diffPixels != null)
                {
                    StageDiffAndExpectedImages(
                        imageMessage,
                        expected,
                        diffPixels,
                        options.FileExtension,
                        actualExrFormat
                    );
                }

                if (options.SaveImagesToDiskOnFailure)
                {
                    SaveImageToPersistentData(imageMessage, options.FileExtension);
                }

                if (options.SaveImagesOnFailure)
                {
                    SaveImage(imageMessage, options.FileExtension);
                }
            }
        }

        static void ValidateArguments(Texture2D actual, LegacyImageExportOptions options)
        {
            if (!actual)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            if (options.FileExtension != "png" && options.FileExtension != "exr")
            {
                throw new ArgumentException(nameof(actual));
            }
        }

        internal virtual string FindImageDirectoryName()
        {
            var settings = GraphicsTestBuildSettings.LoadOrDefault();
            PlatformSchema schema = null;
            if (settings.BuildPlatformSchemata is { Length: > 0 })
                schema = settings.BuildPlatformSchemata[0];
            if (schema == null && settings.PlatformSchemata is { Length: > 0 })
                schema = settings.PlatformSchemata[0];
            schema ??= PlatformSchema.AllPlatformSchema;

            return string.Join('/', ActualImagePath, GraphicsTestPlatform.GetCurrent(schema).ResultsPath);
        }

        internal virtual string FindImageName()
        {
            return TestContext.CurrentContext.Test.MethodName != null
                ? TestContext.CurrentContext.Test.Name.ToValidPath()
                : "NoName";
        }

        protected virtual void SaveImageToPersistentData(ImageMessage imageMessage, string extension)
        {
            var saveDir = Path.Combine(Application.persistentDataPath, k_ActualImagesFolderName, imageMessage.PathName);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            var actualImagePath = Path.Combine(saveDir, $"{imageMessage.ImageName}.{extension}");
            File.WriteAllBytes(actualImagePath, imageMessage.ActualImage);

            if (imageMessage.DiffImage != null)
            {
                var diffImagePath = Path.Combine(saveDir, $"{imageMessage.ImageName}.diff.{extension}");
                File.WriteAllBytes(diffImagePath, imageMessage.DiffImage);

                var expectedImagesPath = Path.Combine(saveDir, $"{imageMessage.ImageName}.expected.{extension}");
                File.WriteAllBytes(expectedImagesPath, imageMessage.ExpectedImage);
            }
        }

        protected virtual void SaveImage(ImageMessage imageMessage, string extension)
        {
            var isHDR = extension.ToUpperInvariant() == "EXR";

            // In the original logic,  SaveFailedImage was sent to player even if saveFailedImage was set to false. I guess it was a bug, and we change this behavior here.
#if UNITY_EDITOR
            ImageHandler.instance.SaveImage(imageMessage, isHDR, new ImageHandler.TextureImporterSettings());
#else
            PlayerConnection.instance.Send(ImageMessage.MessageId, imageMessage.Serialize());
#endif
        }

        static byte[] Encode(Texture2D texture, string extension, Texture2D.EXRFlags exrFlags = Texture2D.EXRFlags.None)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture), "Texture cannot be null");
            }

            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentException("Extension cannot be null or empty", nameof(extension));
            }

            if (extension.ToUpperInvariant() == "EXR")
            {
                return texture.EncodeToEXR(exrFlags);
            }

            return texture.EncodeToPNG();
        }

        static Texture2D.EXRFlags GetActualExrFormat(string fileExtension, TextureFormat format)
        {
            if (fileExtension == "exr")
            {
                return format == TextureFormat.RGBAHalf ? Texture2D.EXRFlags.None : Texture2D.EXRFlags.OutputAsFloat;
            }

            return Texture2D.EXRFlags.None;
        }

        static void StageDiffAndExpectedImages(
            ImageMessage imageMessage,
            Texture2D expected,
            Span<Color32> diffPixels,
            string fileExtension,
            Texture2D.EXRFlags actualExrFormat
        )
        {
            var diffImageFormat = fileExtension == "exr" ? TextureFormat.RGBAHalf : TextureFormat.RGB24;
            var diffImage = new Texture2D(expected.width, expected.height, diffImageFormat, false, true);

            var diffPixelsArray = new Color32[expected.width * expected.height];
            diffPixels.CopyTo((Span<Color32>)diffPixelsArray);
            diffImage.SetPixels32(diffPixelsArray, 0);
            diffImage.Apply(false);

            imageMessage.DiffImage = Encode(diffImage, fileExtension, actualExrFormat);
            imageMessage.ExpectedImage = Encode(expected, fileExtension, actualExrFormat);
        }
    }
}
