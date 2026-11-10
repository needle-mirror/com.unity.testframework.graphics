using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.Builder
{
    struct CliSettingsConsistencyResult
    {
        internal string m_Message;
        internal bool m_Success;

        internal CliSettingsConsistencyResult(string msg, bool success)
        {
            m_Message = msg;
            m_Success = success;
        }
    }

    class CliSettingsConsistencyValidator
    {
        readonly CommandLineReader m_CommandLineReader;
        readonly TestSettingsReader m_SettingsReader;

        internal CliSettingsConsistencyValidator()
        {
            m_CommandLineReader = new CommandLineReader();
            m_SettingsReader = new TestSettingsReader();
        }

        internal CliSettingsConsistencyValidator(
            ICommandLineProvider commandLineProvider,
            ITestSettingsProvider settingsReader
        )
        {
            m_CommandLineReader = new CommandLineReader(commandLineProvider);
            m_SettingsReader = new TestSettingsReader(settingsReader);
        }

        // Defined in the runtime, where VerifyRunConfigurationAttribute checks the same arguments in Editor
        // and player runs.
        internal static readonly Dictionary<string, GraphicsDeviceType> k_ForceGfxApiArguments =
            ForcedGraphicsArguments.k_GraphicsApiArguments;
        internal static readonly Dictionary<string, RenderingThreadingMode> k_ForceThreadingArguments =
            ForcedGraphicsArguments.k_ThreadingArguments;
        internal const string k_ForceGfxJobsArgument = ForcedGraphicsArguments.k_GfxJobsArgument;
        internal static readonly Dictionary<string, RenderingThreadingMode> k_ForceGfxJobsValues =
            ForcedGraphicsArguments.k_GfxJobsValues;

        internal CliSettingsConsistencyResult ValidateEditor(
            GraphicsDeviceType editorGraphicsDeviceType,
            ref StringBuilder sb
        )
        {
            var undocumented = ForcedGraphicsArguments.FindUndocumentedForms(
                m_CommandLineReader, k_ForceGfxApiArguments.Keys);
            if (undocumented.Count > 0)
                return new CliSettingsConsistencyResult(
                    ForcedGraphicsArguments.DescribeUndocumentedForms(undocumented), false);

            var forceApiArguments = new List<string>();
            foreach (var key in k_ForceGfxApiArguments.Keys)
            {
                if (m_CommandLineReader.CommandLineArgumentExists(key))
                    forceApiArguments.Add(key);
            }
            var forceApiArgumentsArray = forceApiArguments.ToArray();
            if (forceApiArgumentsArray.Length > 1)
            {
                return new CliSettingsConsistencyResult(
                    $"Multiple conflicting commandline arguments were found: {string.Join(" ", forceApiArgumentsArray)}. Only one argument should be passed to force the Editor graphics API.",
                    false
                );
            }

            if (forceApiArgumentsArray.Length == 1)
            {
                var argument = forceApiArgumentsArray[0];
                sb.Append($"{argument} argument was passed to the Editor. Running with {editorGraphicsDeviceType}");
                GraphicsDeviceType? forcedApi = k_ForceGfxApiArguments[argument];
                if (forcedApi != editorGraphicsDeviceType)
                    return new CliSettingsConsistencyResult(
                        $"{argument} argument was passed to the Editor, but the Editor is running with {editorGraphicsDeviceType}",
                        false
                    );
            }

            return new CliSettingsConsistencyResult(string.Empty, true);
        }

        internal CliSettingsConsistencyResult ValidateEditorThreadingMode(
            RenderingThreadingMode currentThreadingMode,
            ref StringBuilder sb
        )
        {
            var undocumented = ForcedGraphicsArguments.FindUndocumentedForms(
                m_CommandLineReader, ForcedGraphicsArguments.ThreadingArgumentNames);
            if (undocumented.Count > 0)
                return new CliSettingsConsistencyResult(
                    ForcedGraphicsArguments.DescribeUndocumentedForms(undocumented), false);

            var forceArguments = new List<string>();
            foreach (var key in k_ForceThreadingArguments.Keys)
            {
                if (m_CommandLineReader.CommandLineArgumentExists(key))
                    forceArguments.Add(key);
            }

            // A single read tells us both whether -force-gfx-jobs is present and what value it carries, so
            // presence and value can never disagree (gfxJobsValue is empty when present without a value).
            var hasForceGfxJobs = m_CommandLineReader.TryGetArgumentValue(k_ForceGfxJobsArgument, out var gfxJobsValue);
            if (hasForceGfxJobs)
                forceArguments.Add(k_ForceGfxJobsArgument);

            if (forceArguments.Count > 1)
            {
                return new CliSettingsConsistencyResult(
                    $"Multiple conflicting commandline arguments were found: {string.Join(" ", forceArguments)}. Only one argument should be passed to force the Editor threading mode.",
                    false
                );
            }

            if (forceArguments.Count == 0)
                return new CliSettingsConsistencyResult(string.Empty, true);

            var argument = forceArguments[0];
            RenderingThreadingMode expectedMode;
            string descriptor;

            if (argument == k_ForceGfxJobsArgument)
            {
                if (string.IsNullOrEmpty(gfxJobsValue))
                {
                    return new CliSettingsConsistencyResult(
                        $"{k_ForceGfxJobsArgument} was passed to the Editor without a value. Expected one of: {string.Join(", ", k_ForceGfxJobsValues.Keys)}.",
                        false
                    );
                }
                if (!k_ForceGfxJobsValues.TryGetValue(gfxJobsValue, out expectedMode))
                {
                    return new CliSettingsConsistencyResult(
                        $"{k_ForceGfxJobsArgument} was passed to the Editor with an unrecognized value '{gfxJobsValue}'. Expected one of: {string.Join(", ", k_ForceGfxJobsValues.Keys)}.",
                        false
                    );
                }
                descriptor = $"{k_ForceGfxJobsArgument} {gfxJobsValue}";
            }
            else
            {
                expectedMode = k_ForceThreadingArguments[argument];
                descriptor = argument;
            }

            sb.Append($"{descriptor} argument was passed to the Editor. Running with {currentThreadingMode}");

            if (expectedMode != currentThreadingMode)
            {
                return new CliSettingsConsistencyResult(
                    $"{descriptor} argument was passed to the Editor, but the Editor is running with {currentThreadingMode}",
                    false
                );
            }

            return new CliSettingsConsistencyResult(string.Empty, true);
        }

        internal CliSettingsConsistencyResult ValidatePlayer(
            GraphicsDeviceType playerBuildGraphicsDeviceType,
            ref StringBuilder sb
        )
        {
            if (m_CommandLineReader.HasMultipleArgumentsThatMatch("-playerGraphicsAPI"))
                return new CliSettingsConsistencyResult(
                    $"Multiple conflicting commandline arguments were found. Only one argument should be passed to force the Player Settings graphics API.",
                    false
                );

            var playerGraphicsApiArgument = m_CommandLineReader.FindCommandLineArgument("-playerGraphicsAPI");
            GraphicsDeviceType? apiFromCli = null;
            if (!string.IsNullOrEmpty(playerGraphicsApiArgument))
            {
                if (!Enum.TryParse<GraphicsDeviceType>(playerGraphicsApiArgument, ignoreCase: true, out var parsed))
                    return new CliSettingsConsistencyResult(
                        $"Invalid value '{playerGraphicsApiArgument}' for -playerGraphicsAPI. Must be a valid GraphicsDeviceType.",
                        false
                    );
                apiFromCli = parsed;
            }
            var apiFromTestSettings = m_SettingsReader.TryGetTestSettings()?.PlayerGraphicsAPI;

            if (apiFromCli != null && apiFromTestSettings != null)
            {
                return new CliSettingsConsistencyResult(
                    $"Multiple conflicting commandline arguments were found: From CLI: {apiFromCli} From Test Settings: {apiFromTestSettings}. Only one argument should be passed to force the Player Settings graphics API.",
                    false
                );
            }

            if (apiFromCli != null)
            {
                sb.Append(
                    $"-playerGraphicsAPI={playerGraphicsApiArgument} argument was passed to the Editor, and the Player Settings Graphics API list starts with {playerBuildGraphicsDeviceType}"
                );
                if (apiFromCli != playerBuildGraphicsDeviceType)
                    return new CliSettingsConsistencyResult(
                        $"-playerGraphicsAPI={playerGraphicsApiArgument} argument was passed to the Editor, but the Player Settings Graphics API list starts with {playerBuildGraphicsDeviceType}",
                        false
                    );
            }

            if (apiFromTestSettings != null)
            {
                sb.Append(
                    $"\"playerGraphicsAPI\":\"{apiFromTestSettings}\" was found in TestSettings.json, and the Player Settings Graphics API list starts with {playerBuildGraphicsDeviceType}"
                );
                if (apiFromTestSettings != playerBuildGraphicsDeviceType)
                    return new CliSettingsConsistencyResult(
                        $"\"playerGraphicsAPI\":\"{apiFromTestSettings}\" was found in TestSettings.json, but the Player Settings Graphics API list starts with {playerBuildGraphicsDeviceType}",
                        false
                    );
            }

            return new CliSettingsConsistencyResult(string.Empty, true);
        }

        internal CliSettingsConsistencyResult Validate(
            GraphicsDeviceType editorGraphicsDeviceType,
            GraphicsDeviceType playerBuildGraphicsDeviceType,
            RenderingThreadingMode editorThreadingMode
        )
        {
            var sb = new StringBuilder();

            var editorResult = ValidateEditor(editorGraphicsDeviceType, ref sb);
            if (!editorResult.m_Success)
                return editorResult;

            var threadingResult = ValidateEditorThreadingMode(editorThreadingMode, ref sb);
            if (!threadingResult.m_Success)
                return threadingResult;

            var playerResult = ValidatePlayer(playerBuildGraphicsDeviceType, ref sb);
            if (!playerResult.m_Success)
                return playerResult;

            return new CliSettingsConsistencyResult(sb.ToString(), true);
        }
    }
}
