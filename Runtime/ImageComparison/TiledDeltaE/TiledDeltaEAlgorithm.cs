using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.TestTools.Graphics.LegacyColorDifference;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Compares two images tile by tile. Each pixel's deltaE above
    /// <see cref="ImageComparisonSettings.PerPixelCorrectnessThreshold"/> is averaged within each tile
    /// of a <see cref="ImageComparisonSettings.TilesPerAxis"/> grid, and the comparison fails when the
    /// most different tile exceeds <see cref="ImageComparisonSettings.MaxTileDeltaEThreshold"/>, so a
    /// difference confined to a small area is not diluted by the rest of the image. It also fails when
    /// the same average over the whole image exceeds
    /// <see cref="ImageComparisonSettings.AverageCorrectnessThreshold"/>, which catches a difference
    /// spread evenly across the image that no single tile stands out for.
    /// </summary>
    public sealed class TiledDeltaEAlgorithm : TextureComparisonAlgorithm
    {
        readonly PixelEvaluationGuide m_EvaluationGuide;

        /// <summary>
        /// Instantiates the algorithm with the thresholds and grid size from the given settings.
        /// </summary>
        /// <param name="settings">
        /// The settings to read <see cref="ImageComparisonSettings.PerPixelCorrectnessThreshold"/>,
        /// <see cref="ImageComparisonSettings.AverageCorrectnessThreshold"/>,
        /// <see cref="ImageComparisonSettings.TilesPerAxis"/> and
        /// <see cref="ImageComparisonSettings.MaxTileDeltaEThreshold"/> from. No other setting affects the comparison.
        /// When null, the default settings are used.
        /// </param>
        public TiledDeltaEAlgorithm(ImageComparisonSettings settings)
        {
            settings ??= new ImageComparisonSettings();

            // Resolving through the guide applies the grid clamp and the unset-threshold fallback.
            var resolved = new PixelEvaluationGuide(settings);

            // The guide resolves the threshold without recording that it was unset, so read the raw value
            // with the guide's own condition (negative means unset; 0 is a real threshold).
            var tileThresholdUnset = settings.MaxTileDeltaEThreshold < 0f;

            // The per-pixel pass computes deltaE, gamma and alpha on demand, keyed by these modes.
            // TestAverageDeltaE requests deltaE alone, which is all this algorithm needs.
            // Measures with the corrected deltaE: this algorithm has no reference images made with the original.
            m_EvaluationGuide = new PixelEvaluationGuide
            {
                DeltaEThreshold = resolved.DeltaEThreshold,
                AverageCorrectnessThreshold = resolved.AverageCorrectnessThreshold,
                TilesPerAxis = resolved.TilesPerAxis,
                MaxTileDeltaEThreshold = resolved.MaxTileDeltaEThreshold,
                EnabledModes = PixelEvaluationModes.TestAverageDeltaE,
                DeltaEFormula = DeltaEFormula.Corrected,
            };

            Description = string.Join(
                Environment.NewLine,
                $"Max average deltaE deviation: '{m_EvaluationGuide.AverageCorrectnessThreshold:G}'",
                $"\tDeltaE threshold per-pixel: '{m_EvaluationGuide.DeltaEThreshold:G}'",
                $"Max deltaE deviation per tile: '{m_EvaluationGuide.MaxTileDeltaEThreshold:G}' over "
                    + $"{m_EvaluationGuide.TilesPerAxis} by {m_EvaluationGuide.TilesPerAxis} tiles"
                    + (tileThresholdUnset ? " (unset, uses the average threshold)" : string.Empty));
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> DescribeExpected()
        {
            return new Dictionary<string, object>
            {
                ["maxAverageDeltaE"] = m_EvaluationGuide.AverageCorrectnessThreshold,
                ["deltaEThresholdPerPixel"] = m_EvaluationGuide.DeltaEThreshold,
                ["tilesPerAxis"] = m_EvaluationGuide.TilesPerAxis,
                ["maxTileDeltaE"] = m_EvaluationGuide.MaxTileDeltaEThreshold,
                ["deltaEFormula"] = m_EvaluationGuide.DeltaEFormula.ToString(),
            };
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> DescribeActual(ITextureComparisonResult result)
        {
            if (result is not TiledDeltaEResult tiledResult)
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                ["averageDeltaE"] = tiledResult.AverageDeltaE,
                ["averageDeltaEWithinThreshold"] = tiledResult.AverageDeltaEWithinThreshold,
                ["maxTileDeltaE"] = tiledResult.MaxTileDeltaE,
                // Newtonsoft would also serialise Vector2Int's magnitude and sqrMagnitude; report the coordinate alone.
                ["worstTile"] = new Dictionary<string, int>
                {
                    ["x"] = tiledResult.WorstTile.x,
                    ["y"] = tiledResult.WorstTile.y,
                },
                ["maxTileDeltaEWithinThreshold"] = tiledResult.MaxTileDeltaEWithinThreshold,
            };
        }

        /// <summary>
        /// Compares the two given textures.
        /// </summary>
        /// <param name="expected">The expected texture. It must be readable.</param>
        /// <param name="actual">
        /// The actual texture, the same size and format as the expected one. It must be readable.
        /// </param>
        /// <returns>A <see cref="TiledDeltaEResult"/></returns>
        /// <exception cref="ArgumentNullException">Either texture is null.</exception>
        /// <exception cref="ArgumentException">The textures differ in size or format.</exception>
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

            // Same size, because the pass indexes both images by the expected image's; same format, as
            // ImageAssert requires.
            BasicTexturePropertiesValidation.ValidateTexturesBasicProperties(new[] { expected }, new[] { actual });

            var pass = PixelComparisonPass.Run(expected, actual, m_EvaluationGuide, capturePerPixelExcess: true);
            var tileScores = TileScoreReducer.Reduce(
                pass.PerPixelExcess, expected.width, expected.height, m_EvaluationGuide.TilesPerAxis);

            // The frame mean comes straight from the pass. It equals a one-tile reduction, which would
            // walk the per-pixel excess a second time to produce the same number.
            var result = new TiledDeltaEResult(
                pass.DiffPixels,
                pass.SumOverThreshold / pass.PixelCount,
                m_EvaluationGuide.AverageCorrectnessThreshold,
                tileScores,
                m_EvaluationGuide.MaxTileDeltaEThreshold);

            if (!result.MaxTileDeltaEWithinThreshold)
            {
                TileFrameAnnotator.Annotate(pass.DiffPixels, tileScores, m_EvaluationGuide.MaxTileDeltaEThreshold);
            }

            return result;
        }

        /// <summary>
        /// Comparing arrays of textures is not supported.
        /// </summary>
        /// <param name="expected">Expected textures</param>
        /// <param name="actual">Actual textures</param>
        /// <returns>Nothing: always throws.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override ITextureComparisonResult Compare(Texture2D[] expected, Texture2D[] actual)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Asynchronous comparison is not supported.
        /// </summary>
        /// <param name="expected">The expected texture</param>
        /// <param name="actual">The actual texture</param>
        /// <returns>Nothing: always throws.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override Task<ITextureComparisonResult> CompareAsync(Texture2D expected, Texture2D actual)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Evaluates a result of this algorithm, for use with the NUnit texture constraint.
        /// </summary>
        /// <param name="result">The result of the comparison</param>
        /// <returns>The result, and whether the comparison passed</returns>
        /// <exception cref="ArgumentException">The result was not produced by this algorithm.</exception>
        public override (object, bool) Evaluate(ITextureComparisonResult result)
        {
            if (result is not TiledDeltaEResult tiledResult)
            {
                throw new ArgumentException("Result to evaluate is not a TiledDeltaEResult");
            }

            return (result, tiledResult.Success);
        }
    }
}
