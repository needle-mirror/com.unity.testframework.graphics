using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Utility class for extracting images from the TestResults.xml file.
    /// This class is used to extract images from the test results and save them to the specified directory.
    /// </summary>
    public class ResultsUtility
    {
        internal static IAssetService AssetService { get; set; } = new AssetDatabaseService();

        static readonly System.Reflection.MethodInfo s_GetEnumPropertyValueMethod =
            typeof(ResultsUtility).GetMethod(
                "GetEnumPropertyValue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

        static string ActualImagesRoot => GraphicsTestBuildSettings.LoadOrDefault().ActualImagesPath;
        const int k_UnknownEnumFallbackValue = 255;

        T GetEnumPropertyValue<T>(XmlDocument doc, string name)
            where T : Enum
        {
            var node = doc.SelectSingleNode(string.Format("//property[@name='{0}']", name));
            if (node == null)
                return (T)Enum.ToObject(typeof(T), k_UnknownEnumFallbackValue);

            return (T)Enum.Parse(typeof(T), node.Attributes["value"].Value);
        }

        internal static void ExtractImagesFromResultsXml()
        {
            ExtractImagesFromResultsXml(out _);
        }

        /// <summary>
        /// Opens a file dialog to select a TestResults.xml, extracts images, and outputs
        /// the <see cref="GraphicsTestPlatform"/> parsed from the XML properties.
        /// </summary>
        /// <returns>True if a file was selected and processed successfully.</returns>
        internal static bool ExtractImagesFromResultsXml(out GraphicsTestPlatform platform)
        {
            return ExtractImagesFromResultsXml(null, out platform);
        }

        /// <summary>
        /// Opens a file dialog to select a TestResults.xml, extracts images using the
        /// specified <paramref name="schema"/>, and outputs the parsed <see cref="GraphicsTestPlatform"/>.
        /// </summary>
        /// <param name="schema">The platform schema to use. When null, the default all-platform schema is used.</param>
        /// <param name="platform">The platform that is extracted from the operation.</param>
        /// <returns>True if a file was selected and processed successfully.</returns>
        internal static bool ExtractImagesFromResultsXml(PlatformSchema schema, out GraphicsTestPlatform platform)
        {
            platform = null;
            var filePath = EditorUtility.OpenFilePanel(
                "Select TestResults.xml file",
                Environment.CurrentDirectory,
                "xml"
            );
            if (string.IsNullOrEmpty(filePath))
                return false;

            var instance = new ResultsUtility();
            platform = instance.ExtractImagesAndGetPlatform(filePath, schema);
            return true;
        }

        static Enum GetEnumValueDynamically(ResultsUtility instance, Type enumType, object doc, string propertyName)
        {
            if (s_GetEnumPropertyValueMethod == null)
                throw new InvalidOperationException(
                    "The results extraction process has failed because the reflection process to convert platforms has failed."
                );

            var genericMethod = s_GetEnumPropertyValueMethod.MakeGenericMethod(enumType);
            return (Enum)genericMethod.Invoke(instance, new[] { doc, propertyName });
        }

        GraphicsTestPlatform ExtractImagesAndGetPlatform(string xmlFilePath, PlatformSchema schema = null)
        {
            if (!Directory.Exists(ActualImagesRoot))
                Directory.CreateDirectory(ActualImagesRoot);

            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            var sortedTypes = new List<Type>(GraphicsTestPlatform.Current.Data.Keys);
            sortedTypes.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            var platforms = new List<Enum>();
            foreach (var type in sortedTypes)
            {
                platforms.Add(GetEnumValueDynamically(this, type, doc, type.AssemblyQualifiedName));
            }

            var platform =
                schema != null
                    ? new GraphicsTestPlatform(schema, platforms.ToArray())
                    : new GraphicsTestPlatform(platforms.ToArray());
            var path = Path.Combine(ActualImagesRoot, platform.ResultsPath);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var canonicalRoot = Path.GetFullPath(ActualImagesRoot);
            var imagesWritten = new HashSet<string>();
            var xmlDir = Path.GetDirectoryName(xmlFilePath);

            var failedTestCases = doc.SelectNodes("//test-case[@result!='Passed']");
            if (failedTestCases != null)
            {
                foreach (XmlNode node in failedTestCases)
                {
                    if (!(node is XmlElement failedTestCase))
                        continue;

                    var testName = SanitizeFileName(failedTestCase.Attributes["name"].Value);

                    // Strategy 1: base64-encoded Image property embedded in the XML
                    var imageProperty = (XmlElement)
                        failedTestCase.SelectSingleNode("./properties/property[@name='Image']");
                    if (imageProperty != null)
                    {
                        var bytes = Convert.FromBase64String(imageProperty.Attributes["value"].Value);
                        var imagePath = Path.Combine(path, testName + ".png");
                        if (!IsWithinRoot(imagePath, canonicalRoot))
                        {
                            GraphicsTestLogger.Log(LogType.Warning, $"Skipping image write outside allowed root: {imagePath}");
                            continue;
                        }
                        File.WriteAllBytes(imagePath, bytes);
                        imagesWritten.Add(imagePath);

                        var diffProperty = (XmlElement)
                            failedTestCase.SelectSingleNode("./properties/property[@name='DiffImage']");
                        if (diffProperty != null)
                        {
                            bytes = Convert.FromBase64String(diffProperty.Attributes["value"].Value);
                            imagePath = Path.Combine(path, testName + ".diff.png");
                            if (!IsWithinRoot(imagePath, canonicalRoot))
                            {
                                GraphicsTestLogger.Log(LogType.Warning, $"Skipping diff image write outside allowed root: {imagePath}");
                                continue;
                            }
                            File.WriteAllBytes(imagePath, bytes);
                            imagesWritten.Add(imagePath);
                        }

                        continue;
                    }

                    // Strategy 2: locate artifact files published via ##utp ArtifactPublish messages
                    var outputNode = failedTestCase.SelectSingleNode("output");
                    if (outputNode == null)
                        continue;

                    foreach (var artifactPath in ParseArtifactPublishDestinations(outputNode.InnerText))
                    {
                        var sourcePath = ResolveArtifactPath(artifactPath, xmlDir);
                        if (sourcePath == null)
                            continue;

                        var destPath = Path.Combine(path, Path.GetFileName(sourcePath));
                        if (!IsWithinRoot(destPath, canonicalRoot))
                        {
                            GraphicsTestLogger.Log(LogType.Warning, $"Skipping artifact copy outside allowed root: {destPath}");
                            continue;
                        }
                        File.Copy(sourcePath, destPath, true);
                        imagesWritten.Add(destPath);
                    }
                }
            }

            AssetService.Refresh();

            ReferenceImageUtility.Default.SetupReferenceImageImportSettings(imagesWritten);

            return platform;
        }

        static string SanitizeFileName(string name)
        {
            var sanitized = name.Replace("..", "").Replace('\\', '_').Replace('/', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                sanitized = sanitized.Replace(c, '_');
            return sanitized;
        }

        static bool IsWithinRoot(string filePath, string canonicalRoot)
        {
            var canonical = Path.GetFullPath(filePath);
            var normalizedRoot = canonicalRoot.EndsWith(Path.DirectorySeparatorChar)
                ? canonicalRoot
                : canonicalRoot + Path.DirectorySeparatorChar;
            return canonical.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        static readonly TimeSpan k_RegexTimeout = TimeSpan.FromSeconds(5);

        static readonly Regex k_ArtifactPublishRegex = new(
            @"##utp:\{""type"":""ArtifactPublish""[^}]*""destination"":""([^""]+)""[^}]*\}",
            RegexOptions.Compiled,
            k_RegexTimeout
        );

        // Handles nested braces or different key ordering
        static readonly Regex k_ArtifactPublishFallbackRegex = new(
            @"##utp:.*?""ArtifactPublish"".*?""destination""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled,
            k_RegexTimeout
        );

        static IEnumerable<string> ParseArtifactPublishDestinations(string output)
        {
            var results = new List<string>();
            var matched = false;

            try
            {
                var matches = k_ArtifactPublishRegex.Matches(output);
                foreach (Match match in matches)
                {
                    matched = true;
                    results.Add(match.Groups[1].Value);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                GraphicsTestLogger.Log(
                    LogType.Warning,
                    "Primary artifact-publish regex timed out; attempting fallback pattern."
                );
                matched = false;
            }

            if (!matched && output.Contains("ArtifactPublish"))
            {
                GraphicsTestLogger.DebugLog(
                    "Primary regex found no matches in output containing 'ArtifactPublish'; trying fallback pattern."
                );

                try
                {
                    var fallback = k_ArtifactPublishFallbackRegex.Matches(output);
                    foreach (Match match in fallback)
                        results.Add(match.Groups[1].Value);
                }
                catch (RegexMatchTimeoutException)
                {
                    GraphicsTestLogger.Log(
                        LogType.Warning,
                        "Fallback artifact-publish regex also timed out; artifact images may not be recovered."
                    );
                }
            }

            return results;
        }

        /// <summary>
        /// Tries to locate an artifact file on disk given its original destination path
        /// and the directory containing the TestResults.xml.
        /// Checks: (1) the absolute destination, (2) the Assets/... subtree relative to
        /// ancestor directories of the XML file (handles UTR's test-results layout).
        /// Rejects paths containing ".." segments after the Assets/ anchor to prevent
        /// directory traversal from untrusted XML content.
        /// </summary>
        const int k_MaxParentWalkDepth = 10;

        static string ResolveArtifactPath(string destination, string xmlDir)
        {
            // 1. Exact absolute path (local test run — file is still in the test project).
            //    Restrict to files under the XML directory tree to prevent untrusted XML
            //    from exfiltrating arbitrary local files.
            if (File.Exists(destination))
            {
                var canonicalXmlRoot = Path.GetFullPath(xmlDir);
                if (IsWithinRoot(destination, canonicalXmlRoot))
                    return destination;

                GraphicsTestLogger.Log(LogType.Warning,
                    $"Rejected absolute artifact path outside XML directory tree: {destination}");
                return null;
            }

            var normalizedDest = destination.Replace('\\', '/');

            // 2. Walk up from the XML directory looking for the Assets/ActualImages subtree
            //    that UTR copies artifacts into.
            var assetsIndex = normalizedDest.IndexOf("Assets/", StringComparison.Ordinal);
            if (assetsIndex < 0)
            {
                GraphicsTestLogger.DebugLog($"Could not find 'Assets/' in artifact path: {destination}");
                return null;
            }

            var relativeTail = normalizedDest.Substring(assetsIndex);

            if (ContainsTraversalSegment(relativeTail))
            {
                GraphicsTestLogger.Log(LogType.Warning, $"Rejected artifact path with directory traversal: {destination}");
                return null;
            }

            var dir = xmlDir;
            var depth = 0;
            while (!string.IsNullOrEmpty(dir) && depth < k_MaxParentWalkDepth)
            {
                var candidate = Path.Combine(dir, relativeTail);
                if (File.Exists(candidate) && Directory.Exists(Path.Combine(dir, "Assets")))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
                depth++;
            }

            GraphicsTestLogger.DebugLog($"Could not resolve artifact path '{destination}' from XML directory '{xmlDir}'");
            return null;
        }

        static bool ContainsTraversalSegment(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/../") || normalized.EndsWith("/.."))
                return true;
            if (normalized.StartsWith("../"))
                return true;
            return normalized == "..";
        }

        /// <summary>
        /// Extracts images from the test properties and saves them to the specified directory.
        /// The images are saved in the format testName.png and testName.diff.png.
        /// The directory is created if it does not exist.
        /// The images are also imported with the reference image import settings.
        /// </summary>
        /// <param name="test">
        /// The test whose properties are to be extracted.
        /// The test must have the properties "Image" and/or "DiffImage" set.
        /// </param>
        public static void ExtractImagesFromTestProperties(TestContext.TestAdapter test)
        {
            if (!(test.Properties.ContainsKey("Image") || test.Properties.ContainsKey("DiffImage")))
                return;

            var dirName = Path.Combine(ActualImagesRoot, GraphicsTestPlatform.Current.ResultsPath);

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            var imagesWritten = new HashSet<string>();

            if (test.Properties.ContainsKey("Image"))
            {
                var bytes = Convert.FromBase64String((string)test.Properties.Get("Image"));
                var path = Path.Combine(dirName, test.Name.ToValidPath() + ".png");
                File.WriteAllBytes(path, bytes);
                imagesWritten.Add(path);
            }

            if (test.Properties.ContainsKey("DiffImage"))
            {
                var bytes = Convert.FromBase64String(
                    (string)test.Properties.Get("DiffImage")
                );
                var path = Path.Combine(dirName, test.Name.ToValidPath() + ".diff.png");
                File.WriteAllBytes(path, bytes);
                imagesWritten.Add(path);
            }

            AssetService.Refresh();

            ReferenceImageUtility.Default.SetupReferenceImageImportSettings(imagesWritten);
        }
    }
}
