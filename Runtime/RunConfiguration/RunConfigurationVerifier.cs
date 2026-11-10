using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Checks, once per run, that the run renders the way its command line asks.
    /// </summary>
    static class RunConfigurationVerifier
    {
        static readonly IRunConfigurationCheck[] k_Checks =
        {
            new ForcedGraphicsApiCheck(),
            new ForcedThreadingModeCheck(),
            new WarpCheck(),
        };

        static RunConfigurationReport s_Report;

        /// <summary>
        /// Evaluated once, on first read, which is the first test with
        /// <see cref="VerifyRunConfigurationAttribute"/>. The graphics device is up by then.
        /// </summary>
        internal static RunConfigurationReport Report
        {
            get
            {
                if (s_Report != null)
                    return s_Report;

                s_Report = Evaluate(RunConfigurationSnapshot.Current, RuntimeSettings.CommandLineReader);
                GraphicsTestLogger.Log(s_Report.Passed ? LogType.Log : LogType.Warning, s_Report.Summary);
                return s_Report;
            }
        }

        /// <summary>
        /// Runs every check against <paramref name="configuration"/> and <paramref name="commandLine"/>.
        /// </summary>
        /// <remarks>
        /// Takes its inputs rather than reading them, so the checks can be tested without a device or a
        /// command line.
        /// </remarks>
        internal static RunConfigurationReport Evaluate(
            RunConfigurationSnapshot configuration, CommandLineReader commandLine)
        {
            var results = new List<RunConfigurationCheckResult>(k_Checks.Length);
            foreach (var check in k_Checks)
                results.Add(check.Evaluate(configuration, commandLine));

            return new RunConfigurationReport(configuration, results);
        }

        /// <summary>
        /// Replaces the report until disposed, so a test can run tests that carry the attribute against a
        /// report of its own instead of the run's.
        /// </summary>
        internal static IDisposable UseReport(RunConfigurationReport report)
        {
            var previous = s_Report;
            s_Report = report;
            return new ReportScope(previous);
        }

        sealed class ReportScope : IDisposable
        {
            readonly RunConfigurationReport m_Previous;

            internal ReportScope(RunConfigurationReport previous) => m_Previous = previous;

            public void Dispose() => s_Report = m_Previous;
        }
    }
}
