namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Selects how the reference image file stem is derived when a graphics test method is parameterized.
    /// </summary>
    /// <remarks>
    /// When <see cref="GraphicsTestAttributeBase.ReferenceImageNamingStrategyType"/> is set and produces a descriptor,
    /// this value is ignored for that method.
    /// </remarks>
    public enum ReferenceImageRootSource
    {
        /// <summary>
        /// Use the parameterized NUnit test case name (default, historical behavior).
        /// </summary>
        ParameterizedTestName = 0,

        /// <summary>
        /// For <see cref="SceneGraphicsTestCase"/>, use the scene asset file name without extension as the reference image root.
        /// All parameter combinations for the same scene then share one reference image.
        /// If the raw case is not scene-based, falls back to <see cref="ParameterizedTestName"/> and logs a warning.
        /// </summary>
        SceneAssetFileStem = 1,
    }
}
