using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The device uses the rendering threading mode that an argument such as <c>-force-gfx-mt</c> or
    /// <c>-force-gfx-jobs split</c> asks for.
    /// </summary>
    class ForcedThreadingModeCheck : IRunConfigurationCheck
    {
        const string k_Name = "Threading mode";

        public RunConfigurationCheckResult Evaluate(
            RunConfigurationSnapshot configuration, CommandLineReader commandLine)
        {
            var undocumented = ForcedGraphicsArguments.FindUndocumentedForms(
                commandLine, ForcedGraphicsArguments.ThreadingArgumentNames);
            if (undocumented.Count > 0)
                return RunConfigurationCheckResult.Fail(
                    $"{k_Name}: {ForcedGraphicsArguments.DescribeUndocumentedForms(undocumented)}");

            var forced = new List<string>();
            foreach (var argument in ForcedGraphicsArguments.k_ThreadingArguments.Keys)
            {
                if (commandLine.CommandLineArgumentExists(argument))
                    forced.Add(argument);
            }

            var hasGfxJobs = commandLine.TryGetArgumentValue(
                ForcedGraphicsArguments.k_GfxJobsArgument, out var gfxJobsValue);
            if (hasGfxJobs)
                forced.Add(ForcedGraphicsArguments.k_GfxJobsArgument);

            if (forced.Count == 0)
                return RunConfigurationCheckResult.Pass($"{k_Name}: the command line doesn't force one.");

            if (forced.Count > 1)
                return RunConfigurationCheckResult.Fail(
                    $"{k_Name}: the command line forces more than one: {string.Join(" ", forced)}.");

            RenderingThreadingMode expected;
            string asked;
            if (hasGfxJobs)
            {
                asked = $"{ForcedGraphicsArguments.k_GfxJobsArgument} {gfxJobsValue}".TrimEnd();
                if (!ForcedGraphicsArguments.k_GfxJobsValues.TryGetValue(gfxJobsValue, out expected))
                    return RunConfigurationCheckResult.Fail(
                        $"{k_Name}: {asked} doesn't name a mode. Use one of: "
                        + $"{string.Join(", ", ForcedGraphicsArguments.k_GfxJobsValues.Keys)}.");
            }
            else
            {
                asked = forced[0];
                expected = ForcedGraphicsArguments.k_ThreadingArguments[asked];
            }

            return configuration.ThreadingMode == expected
                ? RunConfigurationCheckResult.Pass($"{k_Name}: {asked} asks for {expected}, and the device uses it.")
                : RunConfigurationCheckResult.Fail(
                    $"{k_Name}: {asked} asks for {expected}, but the device uses {configuration.ThreadingMode}.");
        }
    }
}
