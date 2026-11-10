using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The device uses the graphics API that an argument such as <c>-force-d3d12</c> asks for.
    /// </summary>
    class ForcedGraphicsApiCheck : IRunConfigurationCheck
    {
        const string k_Name = "Graphics API";

        public RunConfigurationCheckResult Evaluate(
            RunConfigurationSnapshot configuration, CommandLineReader commandLine)
        {
            var undocumented = ForcedGraphicsArguments.FindUndocumentedForms(
                commandLine, ForcedGraphicsArguments.k_GraphicsApiArguments.Keys);
            if (undocumented.Count > 0)
                return RunConfigurationCheckResult.Fail(
                    $"{k_Name}: {ForcedGraphicsArguments.DescribeUndocumentedForms(undocumented)}");

            var forced = new List<string>();
            foreach (var argument in ForcedGraphicsArguments.k_GraphicsApiArguments.Keys)
            {
                if (commandLine.CommandLineArgumentExists(argument))
                    forced.Add(argument);
            }

            if (forced.Count == 0)
                return RunConfigurationCheckResult.Pass($"{k_Name}: the command line doesn't force one.");

            if (forced.Count > 1)
                return RunConfigurationCheckResult.Fail(
                    $"{k_Name}: the command line forces more than one: {string.Join(" ", forced)}.");

            var expected = ForcedGraphicsArguments.k_GraphicsApiArguments[forced[0]];
            return ForcedGraphicsArguments.IsAskedBy(forced[0], configuration.GraphicsApi)
                ? RunConfigurationCheckResult.Pass(
                    $"{k_Name}: {forced[0]} asks for {expected}, and the device uses {configuration.GraphicsApi}.")
                : RunConfigurationCheckResult.Fail(
                    $"{k_Name}: {forced[0]} asks for {expected}, but the device uses {configuration.GraphicsApi}.");
        }
    }
}
