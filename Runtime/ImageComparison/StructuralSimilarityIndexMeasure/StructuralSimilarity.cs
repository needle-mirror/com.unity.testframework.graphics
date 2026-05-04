using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Implements the Structural Similarity Index Measure (SSIM) for texture/image comparison. SSIM is widely used in
    /// image quality assessment (including digital video). The SSIM index is computed over sliding Gaussian-weighted
    /// windows on the reference and test images, and the per-window results are averaged across the full image.
    /// The window size and Gaussian parameters can be configured via <see cref="StructuralSimilaritySettings"/>.
    /// A window size of 11 and a Gaussian σ of 1.0–1.5 are common defaults. The resulting value typically lies between
    /// 0 and 1, where 1 indicates identical images and values near 0 indicate very low structural similarity.
    /// Note: Extreme cases (e.g., constant black vs. constant white) usually yield SSIM near 0; exact 0 depends on
    /// implementation details and stabilizing constants.
    /// </summary>
    public class StructuralSimilarity : TextureComparisonAlgorithm
    {
        const int k_BatchSize = 1024;

        readonly StructuralSimilaritySettings m_Settings;

        /// <summary>
        /// Initialized a new instance of the Structural Similarity Index Measure (SSIM) algorithm with the given threshold.
        /// </summary>
        /// <param name="settings">Index measure under which the test constraint fails</param>
        public StructuralSimilarity(ITextureComparisonSettings settings)
            : base(settings)
        {
            m_Settings = (StructuralSimilaritySettings)settings;
            Description = $"Structural Similarity Index Measure equal or above {m_Settings.MinimumIndexMeasure}";
        }

        /// <summary>
        /// Compares two Texture2D and returns a Structural Similarity Index Measure.
        /// </summary>
        /// <param name="expected">>The reference texture</param>
        /// <param name="actual">The texture being evaluated</param>
        /// <returns>The Structural Similarity Index Measure. higher being better, 1 being the same texture.</returns>
        public override ITextureComparisonResult Compare(Texture2D expected, Texture2D actual)
        {
            return Compare(new[] { expected }, new[] { actual });
        }

        /// <summary>
        /// Compares two arrays of textures using the Structural Similarity Index Measure (SSIM) algorithm. Both arrays need to
        /// have the same length and are compared by corresponding index.
        /// </summary>
        /// <param name="expectedTextures">The reference textures</param>
        /// <param name="actualTextures">The textures being evaluated</param>
        /// <returns>The Structural Similarity Index Measure; higher being better, 30 being the human perceptible threshold.</returns>
        public override ITextureComparisonResult Compare(Texture2D[] expectedTextures, Texture2D[] actualTextures)
        {
            BasicTexturePropertiesValidation.ValidateTexturesBasicProperties(expectedTextures, actualTextures);

            var validSsimRegionWidth = actualTextures[0].width - m_Settings.WindowSize / 2 * 2;
            var validSsimRegionHeight = actualTextures[0].height - m_Settings.WindowSize / 2 * 2;

            var numSsimValues = validSsimRegionWidth * validSsimRegionHeight;

            var lumaResults = new List<LumaPipelineResult>(expectedTextures.Length);
            var ownsLumaResults = !(m_Settings.LumaCalculations is { Count: > 0 });

            if (!ownsLumaResults)
                lumaResults = m_Settings.LumaCalculations;
            else
                for (var i = 0; i < expectedTextures.Length; i++)
                {
                    var res = LumaPipeline.Schedule(
                        expectedTextures[i],
                        actualTextures[i],
                        m_Settings.ColorSpaceHandling
                    );
                    lumaResults.Add(res);
                }

            var SSIMValues = new float[expectedTextures.Length];
            using var ssimValues = new NativeArray<float>(numSsimValues, Allocator.TempJob);
            using var gaussianKernel = GenerateGaussianKernel2D(
                m_Settings.WindowSize,
                m_Settings.WindowSize,
                m_Settings.GaussianWeight,
                Allocator.TempJob
            );

            for (var i = 0; i < lumaResults.Count; i++)
            {
                var lumaResult = lumaResults[i];
                var computeSSIMJob = new ComputeSSIMJob
                {
                    actualLuma = lumaResult.ActualLuma,
                    expectedLuma = lumaResult.ExpectedLuma,
                    imageSizeX = expectedTextures[i].width,
                    imageSizeY = expectedTextures[i].height,
                    windowSizeX = m_Settings.WindowSize,
                    windowSizeY = m_Settings.WindowSize,
                    ssimWindowValues = ssimValues,
                    gaussianWeights = gaussianKernel,
                };
                var computeSSIMJobHandle = computeSSIMJob.Schedule(numSsimValues, k_BatchSize, lumaResult.Handle);

                computeSSIMJobHandle.Complete();
                var ssimSum = 0f;
                for (var j = 0; j < ssimValues.Length; j++)
                    ssimSum += ssimValues[j];
                SSIMValues[i] = ssimSum / ssimValues.Length;

                if (ownsLumaResults)
                    lumaResult.Dispose();
            }

            var totalSSIM = 0f;
            for (var k = 0; k < SSIMValues.Length; k++)
                totalSSIM += SSIMValues[k];

            return new StructuralSimilarityResult
            {
                PerTextureIndexMeasure = SSIMValues,
                AverageIndexMeasure = totalSSIM / SSIMValues.Length,
            };
        }

        /// <summary>
        /// Asynchronously compares two textures using the Structural Similarity Index Measure (SSIM) algorithm.
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
        /// <param name="result">The SSIM comparison result</param>
        /// <returns>A tuple containing the result and whether it passes the evaluation</returns>
        public override (object, bool) Evaluate(ITextureComparisonResult result)
        {
            return (result, ((StructuralSimilarityResult)result).AverageIndexMeasure >= m_Settings.MinimumIndexMeasure);
        }

        static NativeArray<float> GenerateGaussianKernel2D(int sizeX, int sizeY, float sigma, Allocator allocator)
        {
            var kernel = new NativeArray<float>(sizeX * sizeY, allocator);
            var halfX = sizeX / 2;
            var halfY = sizeY / 2;
            var twoSigmaSq = 2.0f * sigma * sigma;
            var sum = 0.0f;

            for (var y = 0; y < sizeY; y++)
            for (var x = 0; x < sizeX; x++)
            {
                float valX = x - halfX;
                float valY = y - halfY;
                var weight = Mathf.Exp(-(valX * valX + valY * valY) / twoSigmaSq);
                kernel[y * sizeX + x] = weight;
                sum += weight;
            }

            // Normalize the kernel so sum of weights is 1
            for (var i = 0; i < kernel.Length; i++)
                kernel[i] /= sum;

            return kernel;
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        struct ComputeSSIMJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float> expectedLuma;

            [ReadOnly]
            public NativeArray<float> actualLuma;

            [ReadOnly]
            public NativeArray<float> gaussianWeights;

            public int imageSizeX;
            public int imageSizeY;
            public int windowSizeX;
            public int windowSizeY;
            public bool useSampleCovariance; // True to match Python default, False for Paper

            [WriteOnly]
            public NativeArray<float> ssimWindowValues;

            public void Execute(int index)
            {
                // 1. Map index to FULL image coordinates (not just valid region)
                var centerX = index % imageSizeX;
                var centerY = index / imageSizeX;

                var halfWinX = windowSizeX / 2;
                var halfWinY = windowSizeY / 2;

                float sumX = 0,
                    sumY = 0,
                    sumX2 = 0,
                    sumY2 = 0,
                    sumXY = 0,
                    totalW = 0;
                var useGaussian = gaussianWeights.IsCreated;

                // 2. Local window loop with edge clamping
                for (var y = -halfWinY; y <= halfWinY; y++)
                {
                    // Clamp Y to image bounds
                    var pixelY = Mathf.Clamp(centerY + y, 0, imageSizeY - 1);
                    var rowOffset = pixelY * imageSizeX;
                    var weightRowOffset = (y + halfWinY) * windowSizeX;

                    for (var x = -halfWinX; x <= halfWinX; x++)
                    {
                        // Clamp X to image bounds
                        var pixelX = Mathf.Clamp(centerX + x, 0, imageSizeX - 1);
                        var weightIndex = weightRowOffset + x + halfWinX;

                        var valX = expectedLuma[rowOffset + pixelX];
                        var valY = actualLuma[rowOffset + pixelX];
                        var w = useGaussian ? gaussianWeights[weightIndex] : 1.0f;

                        sumX += valX * w;
                        sumY += valY * w;
                        sumX2 += valX * valX * w;
                        sumY2 += valY * valY * w;
                        sumXY += valX * valY * w;
                        totalW += w;
                    }
                }

                // 3. Normalization Factors
                var invW = 1.0f / totalW;

                // covNorm is 1.0 for Population (Wang paper), totalW/(totalW-1) for Sample (skimage)
                var covNorm = useSampleCovariance ? totalW / (totalW - 1.0f) : 1.0f;

                var muX = sumX * invW;
                var muY = sumY * invW;

                // sigma calculation using the chosen normalization
                var sigmaX2 = Mathf.Max(0f, (sumX2 * invW - muX * muX) * covNorm);
                var sigmaY2 = Mathf.Max(0f, (sumY2 * invW - muY * muY) * covNorm);
                var sigmaXY = (sumXY * invW - muX * muY) * covNorm;

                // 4. Constants
                const float MAX_LUMA = 255.0f;
                const float C1 = 0.01f * MAX_LUMA * (0.01f * MAX_LUMA);
                const float C2 = 0.03f * MAX_LUMA * (0.03f * MAX_LUMA);

                // 5. Combined SSIM Formula
                var num = (2f * muX * muY + C1) * (2f * sigmaXY + C2);
                var den = (muX * muX + muY * muY + C1) * (sigmaX2 + sigmaY2 + C2);

                ssimWindowValues[index] = num / den;
            }
        }
    }
}
