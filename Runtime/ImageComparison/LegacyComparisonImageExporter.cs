using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.TestTools.Graphics.Platforms;
#if UNITY_TEST_PROTOCOL
using Unity.TestProtocol;
using Unity.TestProtocol.Messages;
#endif

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
        const string k_ExpectedImageSuffix = ".expected";
        const string k_DiffImageSuffix = ".diff";

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

            // Any build records BuildPlatformSchemata, so it is normally present. Falling back to the
            // authored schemata would pick arbitrarily now that they come from several sources.
            PlatformSchema schema = null;
            if (settings.BuildPlatformSchemata is { Length: > 0 })
                schema = settings.BuildPlatformSchemata[0];

            if (schema == null)
            {
                GraphicsTestLogger.Log(
                    LogType.Warning,
                    "No BuildPlatformSchemata found (has a build run?); falling back to AllPlatformSchema. "
                        + "Image comparison results may be exported to the wrong folder."
                );
                schema = PlatformSchema.AllPlatformSchema;
            }

            return string.Join('/', ActualImagePath, GraphicsTestPlatform.GetCurrent(schema).ResultsPath);
        }

        internal virtual string FindImageName()
        {
            return TestContext.CurrentContext.Test.MethodName != null
                ? TestContext.CurrentContext.Test.Name.ToValidPath()
                : "NoName";
        }

        internal virtual void SaveImageToPersistentData(ImageMessage imageMessage, string extension)
        {
            if (TryCombinePath(out var saveDir, Application.persistentDataPath, k_ActualImagesFolderName, imageMessage.PathName))
            {
                SaveImageSet(imageMessage, extension, saveDir);
            }
        }

        internal virtual void SaveImage(ImageMessage imageMessage, string extension)
        {
            // In the original logic,  SaveFailedImage was sent to player even if saveFailedImage was set to false. I guess it was a bug, and we change this behavior here.
#if UNITY_EDITOR
            ImageHandler.instance.SaveImage(imageMessage, extension, new ImageHandler.TextureImporterSettings());
#else
            if (!RuntimeSettings.isLogReadingMode)
            {
                PlayerConnection.instance.Send(ImageMessage.MessageId, imageMessage.Serialize());
                return;
            }

            SaveImagesForLogReadingRun(imageMessage, extension);
#endif
        }

        // Announcing each file as an artifact is the whole point of this path: without
        // com.unity.external.test-protocol nothing would ever collect the files, so fall back to the
        // persistent data path rather than leaving them somewhere nobody looks.
        // Not compiled out under UNITY_EDITOR - SaveImage never reaches it there, but leaving it callable
        // is what lets an EditMode test cover this path without a player build.
        internal virtual void SaveImagesForLogReadingRun(ImageMessage imageMessage, string extension)
        {
#if UNITY_TEST_PROTOCOL
            var logDirectory = FindLogDirectory();
            if (string.IsNullOrEmpty(logDirectory))
            {
                GraphicsTestLogger.LogWarning(
                    "LegacyComparisonImageExporter: log-reading mode is active but the player has no log file " +
                    "to write next to. Falling back to the persistent data path.");
                SaveImageToPersistentData(imageMessage, extension);
                return;
            }

            if (TryCombinePath(out var directory, logDirectory, imageMessage.PathName))
            {
                SaveImageSet(imageMessage, extension, directory, ReportArtifact);
            }
#else
            GraphicsTestLogger.LogWarning(
                "LegacyComparisonImageExporter: log-reading mode needs com.unity.external.test-protocol to " +
                "publish the failed images as artifacts. Writing them to the persistent data path instead.");
            SaveImageToPersistentData(imageMessage, extension);
#endif
        }

        // On some platforms this is the artifacts directory the runner asked for, on others (UWP) it is the
        // player's own sandboxed log location. Either way it is the log the runner reads, so a path reported
        // from next to it is one the runner can reach.
        internal virtual string FindLogDirectory() => GetLogDirectory(Application.consoleLogPath);

        // Returns null when there is no log to write next to.
        //
        // A path that already carries a directory is taken as it is. Resolving it first would corrupt a
        // platform-specific root such as Switch's "host:/", which Path.GetFullPath does not recognise as
        // rooted and so prefixes with the working directory. Only a bare "-logFile player.log", which has no
        // directory part of its own, needs resolving to find the executable's directory.
        internal static string GetLogDirectory(string logPath)
        {
            // Empty when logging is off, and the calls below reject an empty path outright.
            if (string.IsNullOrEmpty(logPath))
            {
                return null;
            }

            try
            {
                var directory = Path.GetDirectoryName(logPath);
                return string.IsNullOrEmpty(directory)
                    ? Path.GetDirectoryName(Path.GetFullPath(logPath))
                    : directory;
            }
            catch (Exception e) when (e is ArgumentException or IOException or NotSupportedException)
            {
                GraphicsTestLogger.LogWarning(
                    $"LegacyComparisonImageExporter: could not resolve the player log path '{logPath}' " +
                    $"({e.GetType().Name}: {e.Message}).");
                return null;
            }
        }

        // The single owner of the "<directory>/<name>[.expected|.diff].<extension>" layout, so that adding a
        // destination does not mean restating it. onFileWritten lets a caller publish, index or log the
        // results without this method having to know what any of that means.
        internal virtual void SaveImageSet(
            ImageMessage imageMessage,
            string extension,
            string directory,
            Action<string> onFileWritten = null
        )
        {
            if (!TryCreateDirectory(directory))
            {
                return;
            }

            WriteImageFile(directory, imageMessage.ImageName, extension, imageMessage.ActualImage, onFileWritten);
            WriteImageFile(directory, imageMessage.ImageName + k_ExpectedImageSuffix, extension, imageMessage.ExpectedImage, onFileWritten);
            WriteImageFile(directory, imageMessage.ImageName + k_DiffImageSuffix, extension, imageMessage.DiffImage, onFileWritten);
        }

        static void WriteImageFile(string directory, string fileName, string extension, byte[] data, Action<string> onFileWritten)
        {
            if (data == null)
            {
                return;
            }

            if (TryCombinePath(out var filePath, directory, $"{fileName}.{extension}") &&
                TryWriteAllBytes(filePath, data))
            {
                onFileWritten?.Invoke(filePath);
            }
        }

        // The directory these paths are built on comes from settings and from the platform, so it can hold
        // anything, and Path.Combine rejects some of it outright rather than letting the write fail. Reported
        // rather than thrown for the same reason as the calls below.
        static bool TryCombinePath(out string combined, params string[] parts)
        {
            try
            {
                combined = Path.Combine(parts);
                return true;
            }
            catch (ArgumentException e)
            {
                GraphicsTestLogger.LogWarning(
                    $"LegacyComparisonImageExporter: could not build a path from '{string.Join("', '", parts)}' " +
                    $"({e.GetType().Name}: {e.Message}). Comparison images will not be saved.");
                combined = null;
                return false;
            }
        }

        // No Exists check: CreateDirectory is a no-op when the directory already exists, which is the common
        // case here since every test in a project shares a PathName. It does throw when a file occupies the
        // path, and writes can be refused outright inside a sandboxed player. Both are reported rather than
        // thrown - this runs while a test is already failing, and a second exception would bury the real one.
        static bool TryCreateDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                GraphicsTestLogger.LogWarning(
                    $"LegacyComparisonImageExporter: could not create '{directory}' ({e.GetType().Name}: {e.Message}). " +
                    "Comparison images will not be saved.");
                return false;
            }
        }

        static bool TryWriteAllBytes(string filePath, byte[] data)
        {
            try
            {
                File.WriteAllBytes(filePath, data);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                GraphicsTestLogger.LogWarning(
                    $"LegacyComparisonImageExporter: could not write '{filePath}' ({e.GetType().Name}: {e.Message}).");
                return false;
            }
        }

        // allowOverwrite is on because the runner reruns failed tests, so the same image is published more
        // than once within a run. Matches ImageHandler.ReportArtifact.
        void ReportArtifact(string filePath)
        {
#if UNITY_TEST_PROTOCOL
            var fullPath = Path.GetFullPath(filePath);
            PublishArtifactMessage(UnityTestProtocolMessageBuilder.Serialize(ArtifactPublishMessage.Create(fullPath, true)));
#endif
        }

        // The seam a test overrides to read what would be published. Announcing an artifact means logging a
        // "##utp" line, and the test runner reads those back out of this run's own output, so a test that let
        // one through would have the runner collect a file the test invented.
        internal virtual void PublishArtifactMessage(string utpMessage)
        {
            Debug.Log(utpMessage);
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
