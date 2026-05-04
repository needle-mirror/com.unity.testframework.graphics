using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Utility class for graphics test nodes.
    /// </summary>
    public static class TestUtils
    {
        /// <summary>
        /// Gets the test results folder path for a given graphics test platform.
        /// </summary>
        /// <param name="colorSpace">
        /// The color space used for the test.
        /// </param>
        /// <param name="runtimePlatform">
        /// The runtime platform used for the test.
        /// </param>
        /// <param name="graphicsApi">
        /// The graphics API used for the test.
        /// </param>
        /// <param name="xrsdk">
        /// The XR SDK used for the test.
        /// </param>
        /// <returns>
        /// The test results folder path for the given graphics test platform.
        /// </returns>
        /// <remarks>
        /// This method is obsolete and will be removed in a future version.
        /// Use <see cref="GraphicsTestPlatform.Current"/> instead.
        /// </remarks>
        [Obsolete("Use GraphicsTestPlatform instead.", true)]
        public static string GetTestResultsFolderPath(
            ColorSpace colorSpace,
            RuntimePlatform runtimePlatform,
            GraphicsDeviceType graphicsApi,
            string xrsdk = "None"
        ) => throw new NotImplementedException("Use GraphicsTestPlatform instead.");

        /// <summary>
        /// Gets the current test results folder path.
        /// This method is obsolete and will be removed in a future version.
        /// Use <see cref="GraphicsTestPlatform.Current.ResultsPath"/> instead.
        /// </summary>
        /// <returns>
        /// The current test results folder path.
        /// </returns>
        [Obsolete("Use GraphicsTestPlatform.Current.ResultsPath instead.")]
        public static string GetCurrentTestResultsFolderPath() =>
            GraphicsTestPlatform.Current.ResultsPath;
    }
}
