namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// One thing <see cref="VerifyRunConfigurationAttribute"/> checks: that part of how the run renders
    /// is what the command line asks for.
    /// </summary>
    /// <remarks>
    /// Internal. <see cref="RunConfigurationVerifier"/> lists the checks, so a further check is a new
    /// implementation added there.
    /// </remarks>
    interface IRunConfigurationCheck
    {
        /// <summary>
        /// Compares <paramref name="configuration"/> with what <paramref name="commandLine"/> asks for. A
        /// check whose arguments are absent passes, and says so.
        /// </summary>
        RunConfigurationCheckResult Evaluate(RunConfigurationSnapshot configuration, CommandLineReader commandLine);
    }

    /// <summary>
    /// What one <see cref="IRunConfigurationCheck"/> found.
    /// </summary>
    readonly struct RunConfigurationCheckResult
    {
        RunConfigurationCheckResult(bool passed, string message)
        {
            Passed = passed;
            Message = message;
        }

        internal bool Passed { get; }

        /// <summary>
        /// What was compared, for the log when the check passes and for the test failure when it doesn't.
        /// </summary>
        internal string Message { get; }

        internal static RunConfigurationCheckResult Pass(string message) => new(true, message);

        internal static RunConfigurationCheckResult Fail(string message) => new(false, message);
    }
}
