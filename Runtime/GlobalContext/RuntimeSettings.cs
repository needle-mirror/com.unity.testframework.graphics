using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Helper class for runtime automated graphics tests.
    /// </summary>
    public static class RuntimeSettings
    {
        internal const string k_UtpOutputModeArgument = "-utp-output-mode";
        internal const string k_UtpOutputModeLog = "log";

        internal static CommandLineReader CommandLineReader { get; set; } = new();

        // Whether the runner asked the player to report its results through the player log rather than over
        // player connection, which it does by passing "-utp-output-mode log". Unlike the settings below it
        // cannot be baked in at build time - the same player can be launched by either runner - and caching
        // it here would freeze it before a test can substitute the reader. Not derived from
        // PlayerConnection.instance.isConnected, which is also false while a connection is still coming up
        // and would divert artifacts to disk on a run whose log nobody reads.
        internal static bool isLogReadingMode =>
            CommandLineReader.CommandLineArgumentExists(k_UtpOutputModeArgument) &&
            RequestsLogOutputMode(CommandLineReader.GetRawArguments());

        // Unlike the other arguments read here, the "-arg=value" spelling is not accepted: utp-reporter,
        // which decides where the results go, rejects "-utp-output-mode=log" and only takes the flag on its
        // own with the value in the next argument.
        static bool RequestsLogOutputMode(string[] args)
        {
            for (var i = 0; args != null && i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], k_UtpOutputModeArgument, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(args[i + 1], k_UtpOutputModeLog, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Use this property to check if standard images (non-XR) should be used as reference when running tests in XR mode.
        /// </summary>
        public static bool reuseTestsForXR { get; } = _reuseTestsForXR;

        // Cache result to avoid GC.
        private static bool _reuseTestsForXR
        {
#if UNITY_EDITOR
            get => CommandLineReader.CommandLineArgumentExists("-xr-reuse-tests");
#elif XR_REUSE_TESTS_STANDALONE
            get => true;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Use this property to check if standard images (non-RG) should be used as reference when running tests in RenderGraph mode.
        /// </summary>
#if !UNITY_6000_4_OR_NEWER
        public static bool reuseTestsForRenderGraph { get; } = _reuseTestsForRenderGraph;

        // Cache result to avoid GC.
        private static bool _reuseTestsForRenderGraph
        {
#if UNITY_EDITOR
            get => CommandLineReader.CommandLineArgumentExists("-render-graph-reuse-tests");
#elif RENDER_GRAPH_REUSE_TESTS_STANDALONE
            get => true;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Use this property to check if URP_COMPATIBILITY_MODE scripting define should be set.
        /// </summary>
        public static bool urpCompatibilityMode { get; } = _urpCompatibilityMode;

        private static bool _urpCompatibilityMode
        {
#if URP_COMPATIBILITY_MODE
            get => true;
#elif UNITY_EDITOR
            get => CommandLineReader.CommandLineArgumentExists("-urp-compatibility-mode");
#else
            get => false;
#endif
        }
#else
        public static bool reuseTestsForRenderGraph { get; } = true;
#endif
    }
}
