namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The device is WARP, the Direct3D software rasterizer, exactly when <c>-force-driver-type-warp</c>
    /// asks for it.
    /// </summary>
    /// <remarks>
    /// Direct3D 11 creates a WARP device by itself when it can't create one on the GPU, and Direct3D 12
    /// switches to WARP when the adapter it selects is the Microsoft Basic Render Driver. The run goes on
    /// without an error, on a device it didn't ask for.
    /// </remarks>
    class WarpCheck : IRunConfigurationCheck
    {
        const string k_Name = "WARP";

        public RunConfigurationCheckResult Evaluate(
            RunConfigurationSnapshot configuration, CommandLineReader commandLine)
        {
            var undocumented = ForcedGraphicsArguments.FindUndocumentedForms(
                commandLine, new[] { ForcedGraphicsArguments.k_WarpArgument });
            if (undocumented.Count > 0)
                return RunConfigurationCheckResult.Fail(
                    $"{k_Name}: {ForcedGraphicsArguments.DescribeUndocumentedForms(undocumented)}");

            var asked = commandLine.CommandLineArgumentExists(ForcedGraphicsArguments.k_WarpArgument);

            if (asked)
                return configuration.IsWarp
                    ? RunConfigurationCheckResult.Pass(
                        $"{k_Name}: {ForcedGraphicsArguments.k_WarpArgument} asks for WARP, and the device is WARP.")
                    : RunConfigurationCheckResult.Fail(
                        $"{k_Name}: {ForcedGraphicsArguments.k_WarpArgument} asks for WARP, but the device is "
                        + $"{configuration.Device}.");

            return configuration.IsWarp
                ? RunConfigurationCheckResult.Fail(
                    $"{k_Name}: the device is WARP, the Direct3D software rasterizer, but the command line "
                    + $"doesn't ask for it with {ForcedGraphicsArguments.k_WarpArgument}. The GPU driver "
                    + "probably didn't load.")
                : RunConfigurationCheckResult.Pass($"{k_Name}: not asked for, and the device isn't WARP.");
        }
    }
}
