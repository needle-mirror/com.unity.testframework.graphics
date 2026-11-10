using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    /// The algorithm that was initially used with ImageAssert.AreEqual
    /// </summary>
    public class LegacyColorDifferenceAlgorithm : TextureComparisonAlgorithm
    {
        readonly PixelEvaluationGuide m_EvaluationGuide;

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

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> DescribeExpected()
        {
            return new Dictionary<string, object>
            {
                ["maxAverageDeltaE"] = m_EvaluationGuide.AverageCorrectnessThreshold,
                ["maxBadPixelsRatio"] = m_EvaluationGuide.IncorrectPixelsThreshold,
                ["deltaEThresholdPerPixel"] = m_EvaluationGuide.DeltaEThreshold,
                ["gammaThresholdPerPixel"] = m_EvaluationGuide.GammaThreshold,
                ["alphaThresholdPerPixel"] = m_EvaluationGuide.AlphaThreshold,
                ["enabledTests"] = m_EvaluationGuide.EnabledModes.ToString(),
            };
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> DescribeActual(ITextureComparisonResult result)
        {
            if (result is not LegacyColorDifferenceAggregate aggregate)
            {
                return null;
            }

            var results = aggregate.ImageComparisonResults;
            return new Dictionary<string, object>
            {
                ["averageDeltaE"] = results.AverageDeltaE,
                ["badPixelsRatio"] = results.BadPixelsCount,
                ["averageDeltaEWithinThreshold"] = results.AverageDeltaEWithinThreshold,
                ["badPixelsRatioWithinThreshold"] = results.BadPixelsCountWithinThreshold,
            };
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

            var pass = PixelComparisonPass.Run(expected, actual, m_EvaluationGuide, capturePerPixelExcess: false);

            return new LegacyColorDifferenceAggregate(
                pass.DiffPixels,
                (float)pass.BadPixelCount / pass.PixelCount,
                pass.SumOverThreshold / pass.PixelCount,
                m_EvaluationGuide
            );
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
    }
}
