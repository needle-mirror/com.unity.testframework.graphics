using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    /// The per-pixel products of one comparison. Every field comes from a single walk over the job
    /// output, so a caller pays for the walk once no matter how many of these it reads.
    /// </summary>
    readonly struct PixelComparisonPassResult
    {
        /// <summary>The difference image, indexed by pixel index.</summary>
        public Color32[] DiffPixels { get; init; }

        /// <summary>
        /// Per-pixel deltaE above the per-pixel threshold, indexed by <c>y * width + x</c>. Null
        /// when the caller did not ask for it.
        /// </summary>
        public float[] PerPixelExcess { get; init; }

        /// <summary>The sum over every pixel of its deltaE above the per-pixel threshold.</summary>
        public float SumOverThreshold { get; init; }

        /// <summary>How many pixels failed at least one active per-pixel test.</summary>
        public int BadPixelCount { get; init; }

        /// <summary>How many pixels were compared.</summary>
        public int PixelCount { get; init; }
    }

    /// <summary>
    /// Runs the per-pixel comparison of two images. This is deliberately owned by no algorithm: it
    /// evaluates deltaE, gamma and alpha in one pass, and different algorithms reduce its output
    /// differently.
    /// </summary>
    static class PixelComparisonPass
    {
        const int k_BatchSize = 1024;

        /// <param name="capturePerPixelExcess">
        /// When true, the per-pixel excess is captured into an array for callers that reduce it
        /// spatially. It costs a float per pixel, so callers that only need the totals leave it off.
        /// </param>
        internal static PixelComparisonPassResult Run(
            Texture2D expected,
            Texture2D actual,
            PixelEvaluationGuide guide,
            bool capturePerPixelExcess)
        {
            using var results = new NativeArray<LegacyColorDifferencePixelResult>(
                expected.width * expected.height,
                Allocator.TempJob
            );
            using var expectedPixels = new NativeArray<Color32>(expected.GetPixels32(0), Allocator.TempJob);
            using var actualPixels = new NativeArray<Color32>(actual.GetPixels32(0), Allocator.TempJob);

            new PixelDifferenceAggregationJob
            {
                expected = expectedPixels,
                actual = actualPixels,
                evaluationGuide = guide,
                m_results = results,
            }
                .Schedule(expectedPixels.Length, k_BatchSize)
                .Complete();

            var sumOverThreshold = 0f;
            var badPixels = 0;
            var diffPixels = new Color32[expectedPixels.Length];
            var perPixelExcess = capturePerPixelExcess ? new float[expectedPixels.Length] : null;

            foreach (var pixelResult in results)
            {
                sumOverThreshold += pixelResult.DeltaEOverThreshold;
                badPixels += pixelResult.PixelIsCorrect ? 0 : 1;

                diffPixels[pixelResult.Index] = pixelResult.ColorDifference;

                if (perPixelExcess != null)
                {
                    perPixelExcess[pixelResult.Index] = pixelResult.DeltaEOverThreshold;
                }
            }

            return new PixelComparisonPassResult
            {
                DiffPixels = diffPixels,
                PerPixelExcess = perPixelExcess,
                SumOverThreshold = sumOverThreshold,
                BadPixelCount = badPixels,
                PixelCount = expectedPixels.Length,
            };
        }
    }
}
