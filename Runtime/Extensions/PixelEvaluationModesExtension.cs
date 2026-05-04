namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    static class PixelEvaluationModesExtension
    {
        // Slightly more efficient than using HasFlag, as it avoids the boxing/unboxing from the generic CLR code.
        // it's used in parallel on thousands of pixels, so it may matter.
        internal static bool IsSet(this PixelEvaluationModes self, PixelEvaluationModes flag)
        {
            return (self & flag) == flag;
        }

        internal static bool IsAnySet(
            this PixelEvaluationModes self,
            PixelEvaluationModes flag1,
            PixelEvaluationModes flag2
        )
        {
            return (self & (flag1 | flag2)) != 0;
        }

        internal static bool IsAnySet(
            this PixelEvaluationModes self,
            PixelEvaluationModes flag1,
            PixelEvaluationModes flag2,
            PixelEvaluationModes flag3
        )
        {
            return (self & (flag1 | flag2 | flag3)) != 0;
        }
    }
}
