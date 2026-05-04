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

        // Graphics API arguments, used to start the Editor with the selected api
        // List compiled from: https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html
        internal static readonly Dictionary<string, GraphicsDeviceType> k_ForceGfxApiArguments = new()
        {
            { "-force-d3d11", GraphicsDeviceType.Direct3D11 },
            { "-force-d3d12", GraphicsDeviceType.Direct3D12 },
            { "-force-glcore", GraphicsDeviceType.OpenGLCore },
            { "-force-gles", GraphicsDeviceType.OpenGLES3 },
            { "-force-vulkan", GraphicsDeviceType.Vulkan },
            { "-force-metal", GraphicsDeviceType.Metal },
        };

        internal CliSettingsConsistencyResult ValidateEditor(
            GraphicsDeviceType editorGraphicsDeviceType,
            ref StringBuilder sb
        )
        {
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
            GraphicsDeviceType playerBuildGraphicsDeviceType
        )
        {
            var sb = new StringBuilder();

            var editorResult = ValidateEditor(editorGraphicsDeviceType, ref sb);
            if (!editorResult.m_Success)
                return editorResult;

            var playerResult = ValidatePlayer(playerBuildGraphicsDeviceType, ref sb);
            if (!playerResult.m_Success)
                return playerResult;

            return new CliSettingsConsistencyResult(sb.ToString(), true);
        }
    }
}
