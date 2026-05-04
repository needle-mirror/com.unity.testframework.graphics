using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Measure the luma of two Texture2D using the Job System. This is used by the PSNR and SSIM algorithms.
    /// </summary>
    public static class LumaPipeline
    {
        const float k_SrgbLumaWeightR = 0.2126f;
        const float k_SrgbLumaWeightG = 0.7152f;
        const float k_SrgbLumaWeightB = 0.0722f;
        const float k_GammaExponent = 2.2f;
        const float k_ByteNormalization = 255f;

        /// <summary>
        /// Schedule the job to compare the luma of an actual and expected image. Assumes images are in sRGB (gamma) space.
        /// </summary>
        /// <param name="expected">Expected image</param>
        /// <param name="actual">Actual image</param>
        /// <param name="lumaColorSpaceMode">Preferred handling of linear space images</param>
        /// <param name="batchSize">The number of pixels per batch</param>
        /// <param name="allocator">Allocation type for the native arrays used by the job</param>
        /// <returns>Results of the pipeline</returns>
        public static LumaPipelineResult Schedule(
            Texture2D expected,
            Texture2D actual,
            LumaColorSpaceMode lumaColorSpaceMode = LumaColorSpaceMode.RejectLinearImages,
            int batchSize = 1024,
            Allocator allocator = Allocator.TempJob
        )
        {
            var convertPixelsToGamma = false;

            if (!GraphicsFormatUtility.IsSRGBFormat(expected.graphicsFormat))
            {
                if (lumaColorSpaceMode == LumaColorSpaceMode.RejectLinearImages)
                {
                    throw new ArgumentException(
                        "Linear color space texture provided when LumaColorSpaceMode.RejectLinearImages is specified. "
                            + "The texture '"
                            + expected.name
                            + "' uses a linear format ("
                            + expected.graphicsFormat
                            + ") "
                            + "which cannot be processed directly. Either:\n"
                            + "1. Convert your texture to sRGB format before comparison.\n"
                            + "2. Change LumaColorSpaceMode to ConvertLinearImagesToGamma to use converted colors for measurement.\n"
                            + nameof(expected)
                    );
                }

                if (lumaColorSpaceMode == LumaColorSpaceMode.ConvertLinearToGamma)
                {
                    convertPixelsToGamma = true;
                }
            }

            var expectedPixels = new NativeArray<Color32>(expected.GetPixels32(0), allocator);
            var actualPixels = new NativeArray<Color32>(actual.GetPixels32(0), allocator);

            var expectedLuma = new NativeArray<float>(expectedPixels.Length, allocator);
            var actualLuma = new NativeArray<float>(expectedPixels.Length, allocator);
            var deltaLuma = new NativeArray<float>(expectedPixels.Length, allocator);

            var lumaJob = new LumaMeasurementJob
            {
                expected = expectedPixels,
                actual = actualPixels,
                expectedLuma = expectedLuma,
                actualLuma = actualLuma,
                deltaLuma = deltaLuma,
                convertPixelsToGamma = convertPixelsToGamma,
            }.Schedule(expectedPixels.Length, batchSize);

            return new LumaPipelineResult
            {
                ExpectedPixels = expectedPixels,
                ActualPixels = actualPixels,
                ExpectedLuma = expectedLuma,
                ActualLuma = actualLuma,
                DeltaLuma = deltaLuma,
                Handle = lumaJob,
                Width = expected.width,
                Height = expected.height,
            };
        }

        static float MeasureLuma(Color32 srgbColor)
        {
            return srgbColor.r * k_SrgbLumaWeightR + srgbColor.g * k_SrgbLumaWeightG + srgbColor.b * k_SrgbLumaWeightB;
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        struct LumaMeasurementJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Color32> expected;

            [ReadOnly]
            public NativeArray<Color32> actual;

            [WriteOnly]
            public NativeArray<float> expectedLuma;

            [WriteOnly]
            public NativeArray<float> actualLuma;

            [WriteOnly]
            public NativeArray<float> deltaLuma;

            public bool convertPixelsToGamma;
            public bool convertResultToGamma;

            public void Execute(int index)
            {
                var expPixel = expected[index];
                var actPixel = actual[index];

                if (convertPixelsToGamma)
                {
                    expPixel = LinearToGamma(expPixel);
                    actPixel = LinearToGamma(actPixel);
                }

                var expectedLumaVal = MeasureLuma(expPixel);
                var actualLumaVal = MeasureLuma(actPixel);

                if (convertResultToGamma)
                {
                    expectedLumaVal = LinearToGamma(expectedLumaVal);
                    actualLumaVal = LinearToGamma(actualLumaVal);
                }

                expectedLuma[index] = expectedLumaVal;
                actualLuma[index] = actualLumaVal;
                deltaLuma[index] = Mathf.Abs(expectedLumaVal - actualLumaVal);
            }

            static Color32 LinearToGamma(Color32 linearColor)
            {
                var convertedColor = new Color32(
                    (byte)(LinearToGamma(linearColor.r / k_ByteNormalization) * k_ByteNormalization),
                    (byte)(LinearToGamma(linearColor.g / k_ByteNormalization) * k_ByteNormalization),
                    (byte)(LinearToGamma(linearColor.b / k_ByteNormalization) * k_ByteNormalization),
                    linearColor.a
                );

                return convertedColor;
            }

            static float LinearToGamma(float linear)
            {
                return math.pow(linear, 1.0f / k_GammaExponent);
            }
        }
    }
}
