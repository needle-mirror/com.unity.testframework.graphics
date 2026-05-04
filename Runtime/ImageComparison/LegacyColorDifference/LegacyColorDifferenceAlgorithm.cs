using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    /// The algorithm that was initially used with ImageAssert.AreEqual
    /// </summary>
    public class LegacyColorDifferenceAlgorithm : TextureComparisonAlgorithm
    {
        readonly PixelEvaluationGuide m_EvaluationGuide;
        const int k_BatchSize = 1024;

        /// <summary>
        /// Instantiates the algorithm with the right thresholds, based on the legacy settings
        /// </summary>
        /// <param name="settings">Settings including the thresholds and enabled tests</param>
        public LegacyColorDifferenceAlgorithm(ImageComparisonSettings settings)
        {
            var settingsOrDefault = settings ?? new ImageComparisonSettings();
            m_EvaluationGuide = new PixelEvaluationGuide(settingsOrDefault);
            Description = m_EvaluationGuide.ToString();
        }

        /// <summary>
        /// Compares the two given texture using the algorithm
        /// </summary>
        /// <param name="expected">The expected texture</param>
        /// <param name="actual">The actual texture</param>
        /// <returns>A comparison result</returns>
        public override ITextureComparisonResult Compare(Texture2D expected, Texture2D actual)
        {
            if (!expected)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (!actual)
            {
                throw new ArgumentNullException(nameof(actual));
            }

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
                evaluationGuide = m_EvaluationGuide,
                m_results = results,
            }
                .Schedule(expectedPixels.Length, k_BatchSize)
                .Complete();

            var sumOverThreshold = 0f;
            var badPixels = 0;
            var diffPixels = new Color32[expectedPixels.Length];

            foreach (var pixelResult in results)
            {
                sumOverThreshold += pixelResult.DeltaEOverThreshold;
                badPixels += pixelResult.PixelIsCorrect ? 0 : 1;

                diffPixels[pixelResult.Index] = pixelResult.ColorDifference;
            }

            var colorDifferenceReport = new LegacyColorDifferenceAggregate(
                diffPixels,
                (float)badPixels / expectedPixels.Length,
                sumOverThreshold / expectedPixels.Length,
                m_EvaluationGuide
            );

            return colorDifferenceReport;
        }

        /// <summary>
        /// Compare two arrays of texture
        /// </summary>
        /// <param name="expected">Expected textures</param>
        /// <param name="actual">Actual textures</param>
        /// <returns>The result of the comparison</returns>
        /// <exception cref="NotSupportedException">Not Supported for this algorithm</exception>
        public override ITextureComparisonResult Compare(Texture2D[] expected, Texture2D[] actual)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This is not supported yet.
        /// </summary>
        /// <param name="expected">expected</param>
        /// <param name="actual">actual</param>
        /// <returns>An exception</returns>
        /// <exception cref="NotSupportedException">There is no working implementation for this method right now</exception>
        public override Task<ITextureComparisonResult> CompareAsync(Texture2D expected, Texture2D actual)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This is used in conjunction with the NUnit constraint so that the algorithm can be outside the ImageAssert class
        /// with a nice syntax.
        /// </summary>
        /// <param name="result">The result of the comparison</param>
        /// <returns>A tuple with the result and whether the assertion passed or failed</returns>
        public override (object, bool) Evaluate(ITextureComparisonResult result)
        {
            var aggregate = result as LegacyColorDifferenceAggregate;
            if (aggregate == null)
            {
                throw new ArgumentException("Result to evaluate is not a LegacyColorDifferenceAggregate");
            }

            return (result, aggregate.ImageComparisonResults.Success);
        }

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
}
