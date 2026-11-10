using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The result of a <see cref="TiledDeltaEAlgorithm"/> comparison: the average deltaE above the
    /// per-pixel threshold, over the whole image and within its most different tile, and whether each
    /// is within its threshold.
    /// </summary>
    public sealed class TiledDeltaEResult : ITextureComparisonResult, IColorDifferenceResult
    {
        readonly string m_DifferenceMessage;
        readonly ImageAssert.ImageComparisonResults m_ImageComparisonResults;

        /// <summary>
        /// Whether both <see cref="AverageDeltaEWithinThreshold"/> and <see cref="MaxTileDeltaEWithinThreshold"/>
        /// are true.
        /// </summary>
        public bool Success => m_ImageComparisonResults.Success;

        /// <summary>
        /// The average over every pixel of its deltaE above the per-pixel threshold.
        /// </summary>
        public float AverageDeltaE => m_ImageComparisonResults.AverageDeltaE;

        /// <summary>
        /// Whether <see cref="AverageDeltaE"/> is within
        /// <see cref="ImageComparisonSettings.AverageCorrectnessThreshold"/>.
        /// </summary>
        public bool AverageDeltaEWithinThreshold => m_ImageComparisonResults.AverageDeltaEWithinThreshold;

        /// <summary>
        /// The score of the most different tile: the average over its pixels of their deltaE above the per-pixel
        /// threshold.
        /// </summary>
        public float MaxTileDeltaE => m_ImageComparisonResults.MaxTileDeltaE;

        /// <summary>
        /// The tile that scored <see cref="MaxTileDeltaE"/>, as an x-y tile coordinate with the origin at the
        /// bottom-left tile. When several tiles share the highest score, this is the one in the lowest row, then
        /// the lowest column.
        /// </summary>
        public Vector2Int WorstTile => m_ImageComparisonResults.WorstTile;

        /// <summary>
        /// Whether <see cref="MaxTileDeltaE"/> is within <see cref="ImageComparisonSettings.MaxTileDeltaEThreshold"/>.
        /// When the tile threshold is unset, <see cref="ImageComparisonSettings.AverageCorrectnessThreshold"/> is used.
        /// </summary>
        public bool MaxTileDeltaEWithinThreshold => m_ImageComparisonResults.MaxTileDeltaEWithinThreshold;

        /// <summary>
        /// The difference image: grey, brighter where the deltaE between the images is higher, with a
        /// frame drawn around every tile over the tiled threshold.
        /// </summary>
        public Color32[] DifferencePixels { get; }

        // Implemented explicitly, so the public surface carries only what this algorithm produces. This is the
        // shape ImageAssert reports: bad pixels are not counted, so BadPixelsCount is zero and
        // BadPixelsCountWithinThreshold is true, and Evaluated names only the average and the tiles.
        ImageAssert.ImageComparisonResults IColorDifferenceResult.ImageComparisonResults => m_ImageComparisonResults;

        internal TiledDeltaEResult(
            Color32[] differencePixels,
            float averageDeltaE,
            float averageThreshold,
            TileScores tileScores,
            float tileThreshold)
        {
            DifferencePixels = differencePixels;

            // Written as "exceeded" rather than "within" so that a NaN score, which exceeds nothing, reads as
            // within its threshold.
            var averageExceeded = averageDeltaE > averageThreshold;
            var tileExceeded = tileScores.MaxScore > tileThreshold;

            m_ImageComparisonResults = new ImageAssert.ImageComparisonResults
            {
                Success = !averageExceeded && !tileExceeded,
                Evaluated = ImageAssert.ComparisonStatistics.AverageDeltaE
                    | ImageAssert.ComparisonStatistics.MaxTileDeltaE,
                AverageDeltaE = averageDeltaE,
                AverageDeltaEWithinThreshold = !averageExceeded,
                // Bad pixels are not counted. An unevaluated statistic reads as within threshold, because
                // callers read the flag without checking Evaluated.
                BadPixelsCount = 0f,
                BadPixelsCountWithinThreshold = true,
                MaxTileDeltaE = tileScores.MaxScore,
                WorstTile = tileScores.WorstTile,
                MaxTileDeltaEWithinThreshold = !tileExceeded,
            };

            var description = new List<string>();
            if (averageExceeded)
            {
                description.Add($"Average deltaE deviation: '{averageDeltaE:G}'");
            }

            if (tileExceeded)
            {
                description.Add(TileFailureDescriber.Describe(tileThreshold, tileScores));
            }

            m_DifferenceMessage = description.Count > 0
                ? string.Join(Environment.NewLine, description)
                : "Image has no difference based on color difference test criteria.";
        }

        /// <summary>
        /// Describes the comparison: each statistic over its threshold, or that the images matched.
        /// </summary>
        /// <returns>The failure message, or a line saying there was no difference.</returns>
        public override string ToString()
        {
            return m_DifferenceMessage;
        }
    }
}
