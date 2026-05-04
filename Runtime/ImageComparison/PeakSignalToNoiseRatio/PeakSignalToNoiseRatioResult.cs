namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The result of a Peak Signal to Noise Ratio (PSNR) texture comparison.
    /// </summary>
    public struct PeakSignalToNoiseRatioResult : ITextureComparisonResult
    {
        /// <summary>
        /// The Peak Signal to Noise Ratio (PSNR) value for each texture.
        /// </summary>
        public float[] perTextureRatio { get; init; }

        /// <summary>
        /// The aggregated Peak Signal to Noise Ratio (PSNR) value.
        /// </summary>
        public float overallRatio { get; init; }

        /// <summary>
        /// Returns a string representation of the PSNR result.
        /// </summary>
        /// <returns>A string describing the result</returns>
        public override string ToString()
        {
            return $"Peak Signal-to-Noise Ratio: {overallRatio}";
        }
    }
}
