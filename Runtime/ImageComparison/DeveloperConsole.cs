using NUnit.Framework;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Test helpers for the on-screen developer console. When the console is open it draws over the
    /// screen, so any backbuffer or XR image capture includes it and fails the comparison.
    /// </summary>
    public static class DeveloperConsole
    {
        /// <summary>
        /// Whether the on-screen developer console is currently visible.
        /// </summary>
        public static bool IsVisible => Debug.developerConsoleVisible;

        /// <summary>
        /// Fails the current test if the on-screen developer console is visible. Call this before
        /// capturing an image from the backbuffer or an XR display.
        /// </summary>
        public static void ThrowIfVisible()
        {
            if (IsVisible)
                Assert.Fail(
                    "The Developer Console is open on-screen during image capture! This results in "
                    + "a test failure as drawing the console into the test screenshot causes the "
                    + "comparison to fail. This occurred because something logged an error during the test.");
        }
    }
}
