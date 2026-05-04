using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The settings for the PSNR algorithm, including the threshold under which the Peak Signal to Noise Ratio (PSNR) is considered too low.
    /// </summary>
    public class PeakSignalToNoiseRatioSettings : ITextureComparisonSettings
    {
        /// <summary>
        /// Creates a PSNR threshold and ensures Value is valid.
        /// Typical valid range is non-negative; 30 dB is a common perceptual threshold.
        /// </summary>
        /// <param name="value">PSNR threshold in dB.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
        public PeakSignalToNoiseRatioSettings(float value)
        {
            if (value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "PSNR threshold Value must be non-negative.");

            Value = value;
        }

        /// <summary>
        /// Value under which the test constraint fails. Typical values are between 20 and 40, with 30 being the human
        /// perceptible threshold.
        /// </summary>
        public float Value { get; init; }

        /// <summary>
        /// List of Luma calculations to use when measuring PSNR. Each instance contains calculation for an actual and an
        /// expected. Provide this if you want to reuse luma calculations across
        /// different comparison algorithms (e.g. PSNR and SSIM).
        /// </summary>
        public List<LumaPipelineResult> LumaCalculations { get; init; }

        /// <summary>
        /// Specifies how luminance calculations should handle different color spaces.
        /// </summary>
        /// <remarks>
        /// This property controls whether linear color space images are accepted,
        /// and how color space conversions are applied during luminance computation.
        /// </remarks>
        public LumaColorSpaceMode ColorSpaceHandling { get; init; }
    }
}
