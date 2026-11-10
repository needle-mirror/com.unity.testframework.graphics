namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Selects the <see cref="TextureComparisonAlgorithm"/> that <c>ImageAssert.AreEqual</c> creates
    /// to compare a rendered image with its reference. Scenes store the value as an integer, so a
    /// member's value must never change.
    /// </summary>
    public enum ComparisonAlgorithm
    {
        /// <summary>
        /// Compares with
        /// <see cref="UnityEngine.TestTools.Graphics.LegacyColorDifference.LegacyColorDifferenceAlgorithm"/>,
        /// which evaluates the tests selected by <see cref="ImageComparisonSettings.ActiveImageTests"/>.
        /// It measures deltaE with the original deltaE formula, which existing reference images were made with.
        /// </summary>
        [Tooltip("Evaluates the tests selected in Active Image Tests, with the original deltaE formula, which "
                 + "existing reference images were made with.")]
        LegacyColorDifference = 0,

        /// <summary>
        /// Compares with <see cref="UnityEngine.TestTools.Graphics.TiledDeltaEAlgorithm"/>, which averages
        /// each pixel's deltaE above <see cref="ImageComparisonSettings.PerPixelCorrectnessThreshold"/> over
        /// the whole image and within each tile of a <see cref="ImageComparisonSettings.TilesPerAxis"/> grid.
        /// It measures deltaE with the corrected deltaE formula.
        /// </summary>
        [Tooltip("Evaluates the average deltaE over the whole image and within its most different tile, with the "
                 + "corrected deltaE formula.")]
        TiledDeltaE = 1,
    }
}
