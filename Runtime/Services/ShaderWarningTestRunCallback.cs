using System.IO;
using NUnit.Framework.Interfaces;
using UnityEngine.TestRunner;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Test run callback that collects shader warnings through the test run.
    /// When <see cref="GraphicsTestBuildSettings.ShaderWarningsAsErrors"/> is enabled,
    /// the prebuild step (<c>GraphicsTestAutoBuilder.Setup</c>) starts collection before the
    /// build. This callback preserves that session so warnings from both the build and
    /// test execution phases are captured in a single report at <see cref="RunFinished"/>.
    /// If no prebuild collection is active, a new session is started here.
    /// </summary>
    class ShaderWarningTestRunCallback : ITestRunCallback
    {
        public void RunStarted(ITest testsToRun)
        {
            if (!GraphicsTestBuildSettings.LoadOrDefault().ShaderWarningsAsErrors)
                return;

            if (!ShaderWarningCollector.IsCollecting)
                ShaderWarningCollector.StartCollecting();
        }

        public void RunFinished(ITestResult testResults)
        {
            if (!ShaderWarningCollector.IsCollecting)
                return;

            ShaderWarningCollector.StopAndReport();

            if (ShaderWarningCollector.HasWarnings)
            {
                var count = ShaderWarningCollector.CollectedWarnings.Count;
                var msg = $"Shader warnings detected ({count}) during the build and test run. See {ShaderWarningCollector.OutputFilePath}";
                GraphicsTestLogger.Log(LogType.Error, msg);
                Debug.LogError(msg);
            }
        }

        public void TestStarted(ITest test) { }

        public void TestFinished(ITestResult result) { }
    }
}
