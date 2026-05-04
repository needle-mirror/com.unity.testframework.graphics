namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    struct PixelProcessor
    {
        /// <summary>
        /// Process a pixel and evaluate it based on the comparison settings (DeltaE, alpha, gamma)
        /// </summary>
        /// <param name="actual">Actual pixel</param>
        /// <param name="expected">expected </param>
        /// <param name="guide"></param>
        /// <param name="result"></param>
        /// <returns>The qualities of the pixel based on the evaluation</returns>
        public static LegacyColorDifferencePixelResult ProcessPixel(
            Color expected,
            Color actual,
            PixelEvaluationGuide guide,
            LegacyColorDifferencePixelResult result
        )
        {
            if (
                guide.EnabledModes.IsAnySet(PixelEvaluationModes.CountBadDeltaE, PixelEvaluationModes.TestAverageDeltaE)
            )
            {
                result = DeltaEEvaluator.EvaluatePixel(actual, expected, guide, result);
            }

            if (guide.EnabledModes.IsSet(PixelEvaluationModes.CountBadGamma))
            {
                result = GammaDeltaEvaluator.EvaluatePixel(actual, expected, guide, result);
            }

            if (guide.EnabledModes.IsSet(PixelEvaluationModes.CountBadAlpha))
            {
                result = AlphaDeltaEvaluator.EvaluatePixel(actual, expected, guide, result);
            }

            if (guide.EnabledModes > 0)
            {
                result = PixelMaxDelta.EvaluatePixel(actual, expected, guide, result);
            }

            return result;
        }
    }
}
