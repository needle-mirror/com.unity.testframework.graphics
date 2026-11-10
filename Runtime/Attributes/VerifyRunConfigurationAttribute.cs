using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Stops a test when the run doesn't render the way its command line asks, so the test doesn't
    /// report results for a configuration that nobody asked for.
    /// </summary>
    /// <remarks>
    /// <para>The framework checks the run once, before the first test with this attribute:</para>
    /// <para>
    /// <list type="bullet">
    /// <item><description>The graphics API, if the command line forces one with <c>-force-d3d11</c>,
    /// <c>-force-d3d12</c>, <c>-force-vulkan</c>, <c>-force-metal</c>, <c>-force-glcore</c> or its alias
    /// <c>-force-opengl</c>, or <c>-force-gles</c>, including versioned forms such as <c>-force-glcore45</c>
    /// and <c>-force-gles32</c>. On Xbox consoles, the console's Direct3D 12 device satisfies
    /// <c>-force-d3d12</c>.</description></item>
    /// <item><description>The rendering threading mode, if the command line forces one with
    /// <c>-force-gfx-direct</c>, <c>-force-gfx-st</c>, <c>-force-gfx-mt</c> or
    /// <c>-force-gfx-jobs</c>.</description></item>
    /// <item><description>WARP, the Direct3D software rasterizer, which the device can be only if the
    /// command line asks for it with <c>-force-driver-type-warp</c>. Without the argument, WARP means the
    /// GPU driver didn't load.</description></item>
    /// </list>
    /// </para>
    /// <para>Write these arguments in the documented form, the name with one dash and the value after a space,
    /// as in <c>-force-gfx-jobs split</c>. A double dash or a value joined with <c>=</c> fails the check, because
    /// the engine reads those forms for some arguments and ignores them for others.</para>
    /// <para>The checks compare the device with the command line of the run, so they don't check a graphics API
    /// chosen when the player is built, in the Player Settings or with the Unity Test Runner's
    /// <c>--playerGraphicsAPI</c> option. To detect a player that falls back to another graphics API, also pass
    /// the matching argument to the run, such as <c>-force-vulkan</c> for a player built for Vulkan. The WARP
    /// check applies whichever way the graphics API was chosen.</para>
    /// <para>If a check fails, the first test with this attribute fails and lists every failed check, and
    /// the framework skips the later tests with this attribute. The log shows the result of every check:
    /// search it for <c>Run configuration</c>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [VerifyRunConfiguration]
    /// public class MySceneTests
    /// {
    ///     [UnityTest, SceneGraphicsTest("Assets/Scenes")]
    ///     public IEnumerator Run(SceneGraphicsTestCase testCase) { ... }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class VerifyRunConfigurationAttribute : Attribute, ITestAction
    {
        /// <inheritdoc cref="ITestAction.Targets"/>
        public ActionTargets Targets => ActionTargets.Test;

        /// <inheritdoc cref="ITestAction.BeforeTest"/>
        public void BeforeTest(ITest test)
        {
            var report = RunConfigurationVerifier.Report;
            if (report.Passed)
                return;

            if (report.ClaimFailure())
                Assert.Fail(
                    "This run doesn't render the way its command line asks, so this test and the later tests "
                    + $"with [VerifyRunConfiguration] are stopped:{report.Failures}");

            Assert.Ignore(
                "Skipped: this run doesn't render the way its command line asks. The first test with "
                + "[VerifyRunConfiguration] failed with the details.");
        }

        /// <inheritdoc cref="ITestAction.AfterTest"/>
        public void AfterTest(ITest test) { }
    }
}
