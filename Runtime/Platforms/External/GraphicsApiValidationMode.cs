namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Whether a graphics API validation layer is active during a test run.
    /// This is used with <see cref="IgnoreGraphicsTestAttribute"/> to ignore a test when a
    /// validation layer is enabled. Combine with a <c>GraphicsDeviceType</c> value in the
    /// ignore to scope it to specific graphics APIs.
    /// </summary>
    public enum GraphicsApiValidationMode
    {
        /// <summary>
        /// No graphics API validation layer is active.
        /// </summary>
        None = 0,

        /// <summary>
        /// A graphics API validation layer is active. How the layer is enabled is API-specific
        /// (for Direct3D12, the <c>-force-d3d12-debug-as-errors</c> command line argument).
        /// </summary>
        Enabled = 1,
    }
}
