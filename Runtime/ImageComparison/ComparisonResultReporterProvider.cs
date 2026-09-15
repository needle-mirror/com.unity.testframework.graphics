namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provider used to mock the comparison result reporter.
    ///  The Instance should be overwritten in test setups.
    /// </summary>
    static class ComparisonResultReporterProvider
    {
        internal static ComparisonResultReporter Instance { get; set; } = CreateDefault();

        /// <summary>
        /// Resets the reporter to the real reporter
        /// </summary>
        internal static void ResetToDefault()
        {
            Instance = CreateDefault();
        }

        static ComparisonResultReporter CreateDefault()
        {
#if UNITY_TEST_PROTOCOL
            return new PlayerConnectionComparisonResultReporter();
#else
            return new ComparisonResultReporter();
#endif
        }
    }
}
