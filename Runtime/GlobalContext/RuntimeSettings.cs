namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Helper class for runtime automated graphics tests.
    /// </summary>
    public static class RuntimeSettings
    {
        internal static CommandLineReader CommandLineReader { get; set; } = new();

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
