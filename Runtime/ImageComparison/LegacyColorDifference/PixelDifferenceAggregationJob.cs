using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    /// Computes the per-pixel comparison of two images in parallel, one entry per pixel.
    /// </summary>
    struct PixelDifferenceAggregationJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Color32> expected;

        [ReadOnly]
        public NativeArray<Color32> actual;

        [ReadOnly]
        public PixelEvaluationGuide evaluationGuide;

        [WriteOnly]
        public NativeArray<LegacyColorDifferencePixelResult> m_results;

        public void Execute(int index)
        {
            var exp = expected[index];
            var act = actual[index];

            var pixelScoreCard = new LegacyColorDifferencePixelResult { PixelIsCorrect = true };

            var pixelResult = PixelProcessor.ProcessPixel(exp, act, evaluationGuide, pixelScoreCard);
            pixelResult.Index = index;

            m_results[index] = pixelResult;
        }
    }
}
