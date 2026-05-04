using System;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    struct PixelMaxDelta
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

            var deltaAlpha = pixelResult.DeltaAlpha;
            var deltaGamma = pixelResult.DeltaGamma;

            pixelResult.DeltaE = Mathf.LinearToGammaSpace(pixelResult.DeltaE);
            var result = Mathf.Max(Mathf.Max(pixelResult.DeltaE, deltaAlpha), deltaGamma);
            pixelResult.ColorDifference = new Color(result, result, result, 1f);

            return pixelResult;
        }
    }
}
