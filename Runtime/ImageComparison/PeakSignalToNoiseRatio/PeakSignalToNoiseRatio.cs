using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Implements the Peak Signal-to-Noise Ratio (PSNR) metric for texture/image comparison. PSNR (in dB) is derived
    /// from the mean squared error (MSE) between a reference and a test image and depends on the peak pixel value
    /// (e.g., 255 for 8‑bit images). PSNR is commonly used to evaluate the fidelity of compressed images/video: higher
    /// values generally indicate better quality. Typical PSNR values for lossy compression often fall in the 30–50 dB
    /// range, though this depends on content, bit depth, resolution, and viewing conditions. Distortions are more
    /// likely to be perceptible below ~30–35 dB, while values above ~40 dB are often considered high quality.
    /// PSNR is a simple, non-perceptual metric; for perceptual assessment it should be complemented with other measures
    /// such as SSIM or color-difference metrics (e.g. DeltaE).
    /// </summary>
    public class PeakSignalToNoiseRatio : TextureComparisonAlgorithm
    {
        const int k_BatchSize = 1024;
        const float k_MaxPossibleImageValue = 255.0f;
        readonly PeakSignalToNoiseRatioSettings m_Settings;

        /// <summary>
        /// Initialized a new instance of the Peak Signal to Noise Ratio (PSNR) algorithm with the given threshold.
        /// </summary>
        /// <param name="settings">Ratio under which the test constraint fails</param>
        public PeakSignalToNoiseRatio(ITextureComparisonSettings settings)
            : base(settings)
        {
            m_Settings = settings as PeakSignalToNoiseRatioSettings;
            Description = $"Peak Signal-to-Noise Ratio above {((PeakSignalToNoiseRatioSettings)settings).Value}";
        }

        /// <summary>
        /// Compares two Texture2D and returns a Peak Signal to Noise Ratio.
        /// </summary>
        /// <param name="expected">>The reference texture</param>
        /// <param name="actual">The texture being evaluated</param>
        /// <returns>The Peak Signal To Noise Ratio. higher being better, 30 being the human perceptible threshold.</returns>
        public override ITextureComparisonResult Compare(Texture2D expected, Texture2D actual)
        {
            return Compare(new[] { expected }, new[] { actual });
        }

        /// <summary>
        /// Compares two arrays of textures using the Peak Signal to Noise Ratio (PSNR) algorithm. Both arrays need to have the
        /// same length and are compared by corresponding index.
        /// </summary>
        /// <param name="expectedTextures">The reference textures</param>
        /// <param name="actualTextures">The texture being evaluated</param>
        /// <returns>The Peak Signal To Noise Ratio. higher being better, 30 being the human perceptible threshold.</returns>
        public override ITextureComparisonResult Compare(Texture2D[] expectedTextures, Texture2D[] actualTextures)
        {
            var individualTexturePsnr = new List<float>();
            var individualMse = new List<float>();

            BasicTexturePropertiesValidation.ValidateTexturesBasicProperties(expectedTextures, actualTextures);

            var lumaCalculations = new List<LumaPipelineResult>(expectedTextures.Length);
            var ownsLumaResults = !(m_Settings.LumaCalculations is { Count: > 0 });

            if (!ownsLumaResults)
                lumaCalculations = m_Settings.LumaCalculations;
            else
            {
                for (var i = 0; i < expectedTextures.Length; i++)
                {
                    var res = LumaPipeline.Schedule(
                        expectedTextures[i],
                        actualTextures[i],
                        m_Settings.ColorSpaceHandling,
                        batchSize: k_BatchSize
                    );
                    lumaCalculations.Add(res);
                }
            }

            for (var i = 0; i < lumaCalculations.Count; ++i)
            {
                var lumaCalculation = lumaCalculations[i];
                var pixelCount = expectedTextures[i].width * expectedTextures[i].height;

                var mseResult = new NativeArray<float>(1, Allocator.TempJob);
                try
                {
                    new MeanSquaredErrorJob
                    {
                        deltaLuma = lumaCalculation.DeltaLuma,
                        result = mseResult,
                    }.Schedule(lumaCalculation.Handle).Complete();

                    var lumaMeanSquaredError = mseResult[0] / pixelCount;
                    individualMse.Add(lumaMeanSquaredError);
                    individualTexturePsnr.Add(Psnr(lumaMeanSquaredError, k_MaxPossibleImageValue));
                }
                finally
                {
                    mseResult.Dispose();
                    if (ownsLumaResults)
                        lumaCalculation.Dispose();
                }
            }

            var ratioArray = new float[individualTexturePsnr.Count];
            for (var k = 0; k < individualTexturePsnr.Count; k++)
                ratioArray[k] = individualTexturePsnr[k];

            // Derive overall PSNR from the mean MSE across all frames rather than
            // averaging per-frame PSNR values, which overflows when any frame has
            // MSE=0 (producing float.MaxValue that dominates the sum).
            var mseSum = 0f;
            for (var k = 0; k < individualMse.Count; k++)
                mseSum += individualMse[k];
            var overallPsnr = Psnr(mseSum / individualMse.Count, k_MaxPossibleImageValue);

            return new PeakSignalToNoiseRatioResult
            {
                perTextureRatio = ratioArray,
                overallRatio = overallPsnr,
            };
        }

        /// <summary>
        /// Compares asynchronously two textures using the Peak Signal to Noise Ratio (PSNR) algorithm.
        /// </summary>
        /// <param name="expected">The reference image</param>
        /// <param name="actual">The texture being evaluated against the reference</param>
        /// <returns>A task with a result</returns>
        /// <exception cref="NotSupportedException">Async comparison is not supported for this algorithm</exception>
        public override Task<ITextureComparisonResult> CompareAsync(Texture2D expected, Texture2D actual)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Evaluates the comparison result against the defined threshold.
        /// </summary>
        /// <param name="result">The PSNR comparison result</param>
        /// <returns>A tuple containing the result and whether it passes the evaluation</returns>
        public override (object, bool) Evaluate(ITextureComparisonResult result)
        {
            return
                ((PeakSignalToNoiseRatioResult)result).overallRatio > ((PeakSignalToNoiseRatioSettings)Settings).Value
                ? (result, true)
                : (result, false);
        }

        static float Psnr(float meanSquareError, float maxImageValue)
        {
            if (meanSquareError <= float.Epsilon)
                return float.MaxValue;

            return 10.0f * Mathf.Log10((maxImageValue * maxImageValue) / meanSquareError);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        struct MeanSquaredErrorJob : IJob
        {
            [ReadOnly] public NativeArray<float> deltaLuma;
            public NativeArray<float> result;

            public void Execute()
            {
                var sum = 0f;
                for (var i = 0; i < deltaLuma.Length; i++)
                {
                    var d = deltaLuma[i];
                    sum += d * d;
                }
                result[0] = sum;
            }
        }
    }
}
