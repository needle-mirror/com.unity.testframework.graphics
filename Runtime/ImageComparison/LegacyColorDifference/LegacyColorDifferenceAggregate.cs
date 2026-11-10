using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    ///  Represents aggregated results of all the pixels when comparing two images using the legacy color difference algorithm (delta E, alpha, gamma).
    /// </summary>
    public class LegacyColorDifferenceAggregate : ITextureComparisonResult, IColorDifferenceResult
    {
        readonly string m_DifferenceMessage;
        readonly PixelEvaluationGuide m_PixelEvaluationGuide;
        bool m_AverageDeltaEIsWithinThreshold;
        bool m_BadPixelsCountIsWithinThreshold;
        ImageAssert.ComparisonStatistics m_Evaluated;

        /// <summary>
        /// Gets a collection of pixels that represents the difference between two images
        /// by using the biggest delta as the value for the RGB channels
        /// </summary>
        public Color32[] DifferencePixels { get; }

        /// <summary>
        /// Also the comparison results. This is kept for backward compatibility with the old ImageAssert.ImageComparisonResults
        /// </summary>
        public ImageAssert.ImageComparisonResults ImageComparisonResults { get; }

        /// <summary>
        /// Instantiate a LegacyColorDifferenceAggregateResult with aggregation difference values.
        /// </summary>
        /// <param name="differencePixels">A collection of pixels representing the difference between two images</param>
        /// <param name="badPixelsAverage">Average of pixels that are considered bad based on threshold</param>
        /// <param name="sumOverThresholdAverage">How much each pixel are above threshold in average</param>
        /// <param name="evaluationGuide">The thresholds and tests used to measure the image differences</param>
        public LegacyColorDifferenceAggregate(
            Color32[] differencePixels,
            float badPixelsAverage,
            float sumOverThresholdAverage,
            PixelEvaluationGuide evaluationGuide
        )
        {
            DifferencePixels = differencePixels;
            m_PixelEvaluationGuide = evaluationGuide;
            var averageDeltaE = sumOverThresholdAverage;
            var badPixelsCount = badPixelsAverage;

            m_DifferenceMessage = EvaluateResultsAgainstThresholds(averageDeltaE, badPixelsCount);

            ImageComparisonResults = new ImageAssert.ImageComparisonResults
            {
                Success = m_AverageDeltaEIsWithinThreshold && m_BadPixelsCountIsWithinThreshold,
                Evaluated = m_Evaluated,
                AverageDeltaE = averageDeltaE,
                BadPixelsCount = badPixelsCount,
                AverageDeltaEWithinThreshold = m_AverageDeltaEIsWithinThreshold,
                BadPixelsCountWithinThreshold = m_BadPixelsCountIsWithinThreshold,
                // MaxTileDeltaE and WorstTile take the values ImageComparisonResults documents when no tile was scored;
                // the flag reads as within threshold, as every unevaluated statistic does here.
                MaxTileDeltaE = 0f,
                WorstTile = new Vector2Int(-1, -1),
                MaxTileDeltaEWithinThreshold = true,
            };
        }

        string EvaluateResultsAgainstThresholds(float averageDeltaE, float badPixelsCount)
        {
            var description = new List<string>();

            m_Evaluated = ImageAssert.ComparisonStatistics.None;
            // An unevaluated statistic reads as within threshold: Success ANDs these flags, and
            // callers that read a flag without checking Evaluated rely on that.
            m_AverageDeltaEIsWithinThreshold = true;
            m_BadPixelsCountIsWithinThreshold = true;

            if (m_PixelEvaluationGuide.EnabledModes.IsSet(PixelEvaluationModes.TestAverageDeltaE))
            {
                m_Evaluated |= ImageAssert.ComparisonStatistics.AverageDeltaE;

                if (averageDeltaE > m_PixelEvaluationGuide.AverageCorrectnessThreshold)
                {
                    description.Add($"Average deltaE deviation: '{averageDeltaE:G}'");
                    m_AverageDeltaEIsWithinThreshold = false;
                }
            }

            if (
                m_PixelEvaluationGuide.EnabledModes.IsAnySet(
                    PixelEvaluationModes.CountBadDeltaE,
                    PixelEvaluationModes.CountBadGamma,
                    PixelEvaluationModes.CountBadAlpha
                )
            )
            {
                m_Evaluated |= ImageAssert.ComparisonStatistics.BadPixelsCount;

                if (badPixelsCount > m_PixelEvaluationGuide.IncorrectPixelsThreshold)
                {
                    description.Add($"Bad pixels ratio: '{badPixelsCount}'");
                    m_BadPixelsCountIsWithinThreshold = false;
                }
            }

            return description.Count > 0
                ? string.Join(Environment.NewLine, description)
                : "Image has no difference based on color difference test criteria.";
        }

        /// <summary>
        /// Texts that describe the comparison result
        /// </summary>
        /// <returns>Text with a describing the result</returns>
        public override string ToString()
        {
            return m_DifferenceMessage;
        }
    }
}
