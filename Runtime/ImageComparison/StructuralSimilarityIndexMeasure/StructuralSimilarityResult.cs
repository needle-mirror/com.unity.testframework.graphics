namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The result of a Structural Similarity Index Measure texture comparison.
    /// </summary>
    public readonly struct StructuralSimilarityResult : ITextureComparisonResult
    {
        /// <summary>
        /// SSIM value for each compared image.
        /// </summary>
        public float[] PerTextureIndexMeasure { get; init; }

        /// <summary>
        /// Average SSIM when multiple reference - comparison images pairs are provided
        /// </summary>
        public float AverageIndexMeasure { get; init; }

        /// <summary>
        /// Returns a string representation of the SSIM result.
        /// </summary>
        /// <returns>A string describing the result</returns>
        public override string ToString()
        {
            return $"Average Structural Similarity Index Measure: {AverageIndexMeasure}";
        }
    }
}
