namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provider used to mock the image exporter. By default, it writes images to disk. The Instance should be overwritten in test setups.
    /// </summary>
    static class ComparisonImageExporterProvider
    {
        internal static LegacyComparisonImageExporter Instance { get; set; } = new();

        /// <summary>
        /// Resets the image exporter to real exporter
        /// </summary>
        internal static void ResetToDefault()
        {
            Instance = new LegacyComparisonImageExporter();
        }
    }
}
