using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// This class is responsible for generating a shader variant list from the player log.
    /// It reads the log file and extracts the shader variants that were compiled during the test run.
    /// It also handles the conversion of shader not found errors to log entries.
    /// </summary>
    public static class GenerateShaderVariantList
    {
        static readonly TimeSpan k_Timeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// The string used to identify compiled shaders in the log.
        /// </summary>
        public static readonly string k_CompiledShaderString = "Uploaded shader variant to the GPU driver";

        /// <summary>
        /// The string used to identify compiled shaders in the log.
        /// </summary>
        public static readonly Regex s_CompiledShaderRegex = new Regex(
            @$"({k_CompiledShaderString}|Compiled shader): (?<shaderName>[^,]*), pass: (?<passName>[^,]*), stage: (?<stage>[^,]*), keywords (?<keywords>.*)",
            RegexOptions.None,
            k_Timeout
        );

        /// <summary>
        /// The string used to identify compiled compute shaders in the log.
        /// </summary>
        public static readonly Regex s_CompiledComputeShaderRegex = new Regex(
            "Compiled compute shader: (?<computeName>[^,]*), kernel: (?<kernelName>[^,]*), keywords (?<keywords>.*)",
            RegexOptions.None,
            k_Timeout
        );

        /// <summary>
        /// The string used to identify compiled shader snippets in a UnityShaderCompiler log.
        /// </summary>
        public static readonly Regex s_CompiledSnippetRegex = new Regex(
            @$"name: (?<shaderName>[^,]*) pass: (?<passName>[^,]*) stage: (?<stage>[^,]*) keywords: (?<keywords>.*)",
            RegexOptions.None,
            k_Timeout
        );

        /// <summary>
        /// The string used to identify compiled compute kernels in a UnityShaderCompiler log.
        /// </summary>
        public static readonly Regex s_CompiledComputeKernelRegex = new Regex(
            @$"name: (?<computeName>[^,]*) kernel: (?<kernelName>[^,]*) keywords: (?<keywords>.*)",
            RegexOptions.None,
            k_Timeout
        );

        /// <summary>
        /// The string used to identify shader variant not found errors in the log.
        /// </summary>
        public static readonly Regex s_ShaderVariantNotFoundRegex = new Regex(
            "Shader (?<shaderName>[^,]*), subshader (?<subShaderIndex>\\d+), pass (?<passIndex>\\d+), stage (?<stage>[^,]*): variant (?<keywords>.*) not found.",
            RegexOptions.None,
            k_Timeout
        );

        /// <summary>
        /// The string used to identify the absence of keywords in the log.
        /// </summary>
        public static readonly string s_NoKeywordText = "<no keywords>";

        /// <summary>
        /// The environment variable used to determine if the Graphics Test Stripper should be used.
        /// This is used to enable or disable the Graphics Test Stripper system.
        /// </summary>
        public static readonly string k_UseGraphicsTestStripperEnv = "USE_GFX_TEST_STRIPPER";

        /// <summary>
        /// The environment variable used to determine if the fast shader variant list generation should be used.
        /// </summary>
        public static readonly string k_UseFastShaderVariantListGeneration = "FAST_SHADER_TRACE_GENERATION";

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
            GraphicsSettings.logWhenShaderIsCompiled = true;
            GraphicsTestLogger.Log(LogType.Log, "Register log file processing");
            Application.quitting += ConvertShaderErrorsToLog;
        }
#endif

        static void ConvertShaderErrorsToLog()
        {
            var logFilePath = Application.consoleLogPath;

            StringBuilder finalList;
            // Read log while the handle is still controlled by Unity
            using (
                var logFile = new FileStream(
                    logFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                )
            )
            {
                using (var reader = new StreamReader(logFile, Encoding.Default))
                    AppendAllShaderLines(out finalList, reader.ReadToEnd(), true);
            }
            GraphicsTestLogger.Log(
                LogType.Log,
                "The following list of Compiled Shaders are directly converted from shader not found errors. You can ignore them as it's only used for the Graphics Test Shader Variants Stripper system."
            );

            GraphicsTestLogger.Log(LogType.Log, finalList.ToString());
        }

        /// <summary>
        /// Appends all shader lines from the player log to the final file.
        /// </summary>
        /// <param name="finalFile">
        /// The final file to which the shader lines will be appended.
        /// </param>
        /// <param name="playerLogContent">
        /// The content of the player log file.
        /// </param>
        /// <param name="ignoreValidShadersAndCompute">
        /// If true, valid shaders and compute shaders will be ignored.
        /// </param>
        public static void AppendAllShaderLines(
            out StringBuilder finalFile,
            string playerLogContent,
            bool ignoreValidShadersAndCompute = false
        )
        {
            var lines = new SortedSet<string>(StringComparer.Ordinal);
            AppendAllShaderLines(
                out finalFile,
                playerLogContent,
                lines,
                ignoreValidShadersAndCompute
            );
        }

        /// <summary>
        /// Appends all shader lines from the player log to the final file.
        /// This overload allows for an existing file content to be passed in.
        /// </summary>
        /// <param name="finalFile">
        /// The final file to which the shader lines will be appended.
        /// </param>
        /// <param name="playerLogContent">
        /// The content of the player log file.
        /// </param>
        /// <param name="existingFileContent">
        /// The existing file content to which the shader lines will be appended.
        /// </param>
        /// <param name="ignoreValidShadersAndCompute">
        /// If true, valid shaders and compute shaders will be ignored.
        /// </param>
        public static void AppendAllShaderLines(
            out StringBuilder finalFile,
            string playerLogContent,
            SortedSet<string> existingFileContent,
            bool ignoreValidShadersAndCompute = false
        )
        {
            var notFoundMatchSet = new HashSet<string>();

            using var reader = new StringReader(playerLogContent);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineTrimmed = line.Trim();

                if (!ignoreValidShadersAndCompute)
                {
                    var compiledShaderMatch = s_CompiledShaderRegex.Match(lineTrimmed);
                    if (compiledShaderMatch.Success)
                    {
                        var sanitizedLine = compiledShaderMatch.Value;
                        var allStageLine = s_CompiledShaderRegex.Replace(
                            sanitizedLine,
                            $"{k_CompiledShaderString}: $1, pass: $2, stage: all, keywords $4"
                        );

                        if (existingFileContent.Contains(allStageLine))
                            continue;

                        // Replace fragment by pixel to avoid duplication in the file
                        if (compiledShaderMatch.Groups["stage"].Value == "fragment")
                            sanitizedLine = s_CompiledShaderRegex.Replace(
                                sanitizedLine,
                                $"{k_CompiledShaderString}: $1, pass: $2, stage: pixel, keywords $4"
                            );

                        if (existingFileContent.Contains(sanitizedLine))
                            continue;

                        existingFileContent.Add(sanitizedLine);
                    }

                    var computeShaderMatch = s_CompiledComputeShaderRegex.Match(lineTrimmed);
                    if (computeShaderMatch.Success)
                    {
                        var sanitizedLine = computeShaderMatch.Value;

                        if (existingFileContent.Contains(sanitizedLine))
                            continue;

                        existingFileContent.Add(sanitizedLine);
                    }
                }
#if !UNITY_EDITOR
                // Shader not found error can be spammed quite a bit in the log, causing this process to stall with 10000s of calls
                if (notFoundMatchSet.Contains(lineTrimmed))
                    continue;

                var notFoundMatch = s_ShaderVariantNotFoundRegex.Match(lineTrimmed);
                if (notFoundMatch.Success)
                {
                    notFoundMatchSet.Add(lineTrimmed);
                    // Convert not found shader using the available data in the build
                    var shaderName = notFoundMatch.Groups["shaderName"].Value;
                    var shader = Shader.Find(shaderName);
                    if (shader == null)
                    {
                        GraphicsTestLogger.Log(
                            LogType.Error,
                            $"Could not find shader {shaderName}"
                        );
                        continue;
                    }
                    var dummyMaterial = new Material(shader);
                    int.TryParse(notFoundMatch.Groups["passIndex"].Value, out int passIndex);
                    existingFileContent.Add(
                        $"{k_CompiledShaderString}: {shaderName}, pass: {dummyMaterial.GetPassName(passIndex)}, stage: {notFoundMatch.Groups["stage"]}, keywords {notFoundMatch.Groups["keywords"]}"
                    );
                    Object.DestroyImmediate(dummyMaterial);
                }
#endif
            }

            finalFile = new StringBuilder();
            foreach (var s in existingFileContent)
                finalFile.AppendLine(s);
        }
    }
}
