using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Result of a Euclidean distance comparison between two textures, including the average and maximum distance, as well as the per-pixel deltas and dimensions of the compared textures.
    /// </summary>
    public struct EuclideanDistanceResult : ITextureComparisonResult
    {
        /// <summary>
        /// The average Euclidean distance across all pixels between the expected and actual textures. A lower value indicates more similarity, with 0 meaning identical images.
        /// </summary>
        public double Average { get; }

        /// <summary>
        /// The maximum Euclidean distance found between any single pair of corresponding pixels in the expected and actual textures. This value can help identify if there are specific areas of significant difference, even if the average distance is low.
        /// </summary>
        public double Max { get; }
        /// <summary>
        /// The per-pixel Euclidean distance deltas between the expected and actual textures.
        /// </summary>
        public float[] Deltas { get; set; }
        /// <summary>
        /// The width of the compared textures.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The height of the compared textures.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Initializes a new instance of the EuclideanDistanceResult struct with the specified average and maximum distances, and empty deltas and dimensions. The deltas and dimensions can be set later if needed for further analysis or debugging.
        /// </summary>
        /// <param name="average">The average Euclidean distance across all pixels.</param>
        /// <param name="max">The maximum Euclidean distance found between any single pair of corresponding pixels.</param>
        public EuclideanDistanceResult(double average, double max)
        {
            Average = average;
            Max = max;
            Deltas = Array.Empty<float>();
            Width = 0;
            Height = 0;
        }

        /// <summary>
        /// Clears the temporary data stored in the result, such as the per-pixel deltas and dimensions. This can be used to free up memory after the necessary information has been extracted from the result for reporting or analysis purposes.
         /// After calling this method, the Deltas array will be empty and the Width and Height will be reset to 0.
         /// Note that this does not affect the Average and Max properties, which will retain their values.
         /// Use this method when you no longer need the detailed per-pixel information and want to reduce memory usage.
         /// This is particularly useful in scenarios where the result is being stored or transmitted, and the detailed deltas and dimensions are not required for the final outcome of the test, but the average and maximum distances are still relevant for determining pass/fail status or for reporting purposes.
        /// </summary>
        public void ClearTemporary()
        {
            Deltas = null;
            Width = 0;
            Height = 0;
        }
    }
}
