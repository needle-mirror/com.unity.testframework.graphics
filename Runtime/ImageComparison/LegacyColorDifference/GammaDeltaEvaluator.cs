using System;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    struct GammaDeltaEvaluator
    {
        internal static Action TestHook { get; set; }

        internal static LegacyColorDifferencePixelResult EvaluatePixel(
            Color actual,
            Color expected,
            PixelEvaluationGuide evaluationGuide,
            LegacyColorDifferencePixelResult pixelResult
        )
        {
            TestHook?.Invoke();

            var deltaR = Mathf.Abs(expected.r - actual.r);
            var deltaG = Mathf.Abs(expected.g - actual.g);
            var deltaB = Mathf.Abs(expected.b - actual.b);

            pixelResult.DeltaGamma = Mathf.Max(Mathf.Max(deltaR, deltaG), deltaB);
            pixelResult.PixelIsCorrect &= pixelResult.DeltaGamma <= evaluationGuide.GammaThreshold;

            return pixelResult;
        }
    }
}
