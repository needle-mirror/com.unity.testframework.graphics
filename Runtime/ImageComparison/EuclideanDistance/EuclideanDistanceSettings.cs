using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Settings for the Euclidean distance algorithm, including the maximum acceptable distance between two images.
    /// </summary>
    public class EuclideanDistanceSettings : ITextureComparisonSettings
    {
        /// <summary>
        /// Creates Euclidean distance settings and ensures MaximumDistance is valid.
        /// </summary>
        /// <param name="maximumDistance">Maximum acceptable Euclidean distance. Must be non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maximumDistance is negative.</exception>
        public EuclideanDistanceSettings(float maximumDistance)
        {
            if (maximumDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumDistance), "MaximumDistance must be non-negative.");

            MaximumDistance = maximumDistance;
        }

        /// <summary>
        /// Maximum acceptable Euclidean distance. If the average distance between two images exceeds this value,
        /// the comparison is considered a failure. A value of 0 means identical images are required.
        /// </summary>
        public float MaximumDistance { get; init; }
    }
}
