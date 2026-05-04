using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace GraphicsTestFrameworkProject.Samples.AdvancedPatterns
{
    /*
    -- Tutorial [1] --
    GraphicsTestLogger provides a structured logging API that writes to both the
    Unity console and a dedicated log file (Logs/GraphicsTestLogs.log by default).

    Logs are:
      - Timestamped and categorized by severity (INFO / WARN / ERROR)
      - Written asynchronously to avoid blocking test execution
      - Buffered in-memory for inspection via GetLogBuffer()
      - Forwarded from Player to Editor over PlayerConnection

    Use GraphicsTestLogger instead of Debug.Log for graphics test diagnostics,
    as it provides a persistent, structured record of test execution.
    */

    [Category("Samples")]
    [TestOf(nameof(GraphicsTestLoggerExample))]
    internal class GraphicsTestLoggerExample
    {
        /*
        -- Tutorial [2] --
        Basic logging at different severity levels.
        Each method writes to the log file and, by default, to the Unity console.
        */

        [Test]
        [Description("Demonstrates basic log, warning, and error messages.")]
        public void BasicLogging_WritesAtAllSeverityLevels()
        {
            GraphicsTestLogger.Log(LogType.Log, "Informational message from test setup");
            GraphicsTestLogger.Log(LogType.Warning, "Threshold close to limit: 0.049 vs 0.05");
            GraphicsTestLogger.Log(LogType.Error, "Unexpected pixel format in captured image");

            GraphicsTestLogger.Log("Shorthand info log");
            GraphicsTestLogger.LogWarning("Shorthand warning log");
            GraphicsTestLogger.LogError("Shorthand error log");

            Assert.Pass("Check Logs/GraphicsTestLogs.log for output.");
        }

        /*
        -- Tutorial [3] --
        Debug logging: messages are always written to the log file, but only
        appear in the Unity console when debug mode is enabled.

        Enable debug mode by any of:
          - Setting GRAPHICS_TEST_FRAMEWORK_DEBUG=1 environment variable
          - Passing -graphics-test-framework-debug command-line argument
          - Defining GRAPHICS_TEST_FRAMEWORK_DEBUG scripting symbol
        */

        [Test]
        [Description("Demonstrates debug-level logging that is file-only unless debug mode is on.")]
        public void DebugLogging_WritesToFileAlways()
        {
            GraphicsTestLogger.DebugLog("Detailed pixel diff at (120, 340): deltaE = 0.023");
            GraphicsTestLogger.DebugWarning("GPU readback took 12ms, exceeding 10ms budget");
            GraphicsTestLogger.DebugError("Render texture format mismatch detected");

            Assert.Pass("Debug messages are in the log file; console output depends on debug mode.");
        }

        /*
        -- Tutorial [4] --
        Exception logging: formats the exception type, message, and stack trace
        into the log, making it easy to correlate test failures with root causes.
        */

        [Test]
        [Description("Demonstrates structured exception logging.")]
        public void ExceptionLogging_FormatsStackTrace()
        {
            try
            {
                throw new InvalidOperationException("Simulated failure during shader compilation");
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.LogException(ex);
            }

            Assert.Pass("Check the log file for the formatted exception with stack trace.");
        }

        /*
        -- Tutorial [5] --
        Custom log path: redirect output to a test-specific log file.
        This is useful when you want to keep logs from different test suites separate.
        */

        [Test]
        [Description("Demonstrates logging to a custom file path.")]
        public void CustomLogPath_WritesToSpecifiedFile()
        {
            const string customPath = "Logs/SampleTestOutput.log";

            GraphicsTestLogger.Log(LogType.Log, "Written to custom log path", customPath);
            GraphicsTestLogger.LogWarning("Also to custom path", customPath);

            Assert.That(GraphicsTestLogger.MostRecentLogPath, Is.EqualTo(customPath));
        }

        /*
        -- Tutorial [6] --
        Silent logging: pass logToConsole: false to write only to the log file
        without cluttering the Unity console. Useful for high-frequency diagnostics.
        */

        [Test]
        [Description("Demonstrates file-only logging that skips the Unity console.")]
        public void SilentLogging_SkipsConsoleOutput()
        {
            for (int i = 0; i < 10; i++)
            {
                GraphicsTestLogger.Log(
                    LogType.Log,
                    $"Frame {i}: pixel delta = {i * 0.001f:F4}",
                    logToConsole: false
                );
            }

            Assert.Pass("10 log entries written to file only; console remains clean.");
        }
    }
}
