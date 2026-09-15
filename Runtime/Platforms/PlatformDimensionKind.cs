namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// What a platform dimension says about a run, which decides how strongly a reference image
    /// filed under it competes with one filed under another dimension.
    /// </summary>
    public enum PlatformDimensionKind
    {
        /// <summary>
        /// How the run was set up: colour space, graphics API, XR device. The default, because a
        /// dimension that says nothing special about precedence should not claim any.
        /// </summary>
        Configuration = 0,

        /// <summary>
        /// What the machine is, such as its device model. An image filed under a real identity value
        /// was captured for that machine, so its schema resolves before any configuration schema.
        /// Mark a sentinel that names no machine with <see cref="NotAnIdentityAttribute"/>.
        /// </summary>
        Identity,

        /// <summary>
        /// What a fixture deliberately varied, such as a renderer feature toggled per test fixture.
        /// Contexts describe the run rather than the machine, so they never affect which schema wins.
        /// </summary>
        Context,
    }
}
