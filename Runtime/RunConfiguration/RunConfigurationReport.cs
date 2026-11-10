using System.Collections.Generic;
using System.Text;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// What every <see cref="IRunConfigurationCheck"/> found about a run.
    /// </summary>
    class RunConfigurationReport
    {
        bool m_FailureClaimed;

        internal RunConfigurationReport(
            RunConfigurationSnapshot configuration, IReadOnlyList<RunConfigurationCheckResult> results)
        {
            var summary = new StringBuilder($"Run configuration: {configuration}.");
            var failures = new StringBuilder();
            Passed = true;

            foreach (var result in results)
            {
                summary.Append($"\n- {result.Message}");
                if (result.Passed)
                    continue;

                Passed = false;
                failures.Append($"\n- {result.Message}");
            }

            Summary = summary.ToString();
            Failures = failures.ToString();
        }

        /// <summary>
        /// Whether every check passed.
        /// </summary>
        internal bool Passed { get; }

        /// <summary>
        /// The configuration and every check, for the log.
        /// </summary>
        internal string Summary { get; }

        /// <summary>
        /// Every failed check, one per line, for the test that reports them.
        /// </summary>
        internal string Failures { get; }

        /// <summary>
        /// Returns true the first time it is called and false after, so exactly one test per run fails
        /// with the failed checks.
        /// </summary>
        internal bool ClaimFailure()
        {
            if (m_FailureClaimed)
                return false;

            m_FailureClaimed = true;
            return true;
        }
    }
}
