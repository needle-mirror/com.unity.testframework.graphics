using System;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    struct AlphaDeltaEvaluator
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

            pixelResult.DeltaAlpha = Mathf.Abs(expected.a - actual.a);
            pixelResult.PixelIsCorrect &= pixelResult.DeltaAlpha <= evaluationGuide.AlphaThreshold;

            return pixelResult;
        }
    }
}
