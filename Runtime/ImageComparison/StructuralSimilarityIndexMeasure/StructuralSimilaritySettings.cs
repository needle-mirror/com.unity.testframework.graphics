using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Settings used by the structural similarity algorithm when comparing images, including the threshold under which
    /// two images are considered dissimilar, the gaussian weight which is applied to pixels within
    /// each SSIM comparison window, and the size of the SSIM comparison window, and handling of linear space images.
    /// </summary>
    public class StructuralSimilaritySettings : ITextureComparisonSettings
    {
        readonly float m_GaussianWeight = 1.5f;
        readonly int m_WindowSize = 11;

        /// <summary>
        /// Creates settings and ensures MinimumIndexMeasure is set and valid (Between 0 and 1).
        /// </summary>
        /// <param name="minimumIndexMeasure">A value between 0 and 1, where 1 indicates perfect similarity.</param>
        /// <exception cref="ArgumentOutOfRangeException">MinimumIndexMeasure must be between 0 and 1.</exception>
        public StructuralSimilaritySettings(float minimumIndexMeasure)
        {
            if (minimumIndexMeasure < 0f || minimumIndexMeasure > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumIndexMeasure),
                    "MinimumIndexMeasure must be between 0 and 1."
                );
            }

            MinimumIndexMeasure = minimumIndexMeasure;
        }

        /// <summary>
        /// A value between 0 and 1, where 1 indicates perfect similarity (i.e. identical image).
        /// </summary>
        public float MinimumIndexMeasure { get; init; }

        /// <summary>
        /// Gets the side-length of the sliding window used in comparison.
        /// Must be a positive odd value.
        /// Throws ArgumentOutOfRangeException if negative, zero, or even.
        /// </summary>
        public int WindowSize
        {
            get => m_WindowSize;
            init
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(WindowSize),
                        "Sliding window size cannot be negative."
                    );

                if (value == 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(WindowSize),
                        "Sliding window size must be greater than zero."
                    );

                if ((value & 1) == 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(WindowSize),
                        "Sliding window size must be an odd value."
                    );

                m_WindowSize = value;
            }
        }

        /// <summary>
        /// The Gaussian sigma/weight used for the SSIM windowing function.
        /// Default is 1.5. Must be positive.
        /// </summary>
        public float GaussianWeight
        {
            get => m_GaussianWeight;
            init
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(GaussianWeight), "Gaussian weight must be positive.");
                m_GaussianWeight = value;
            }
        }

        /// <summary>
        /// List of luma calculations to use when measuring PSNR. Each instance contains the reference values and the
        /// values for which similarity to reference is measured. Provide this if you want to reuse luma calculations across
        /// different comparison algorithms (i.e. PSNR and SSIM).
        /// </summary>
        public List<LumaPipelineResult> LumaCalculations { get; init; } = new();

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
