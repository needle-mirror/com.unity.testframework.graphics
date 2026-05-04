using System;
using System.Threading.Tasks;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Uses the Pythagorean theorem to measure the per-pixel color difference between two textures using a compute shader.
    /// </summary>
    public class EuclideanDistance : TextureComparisonAlgorithm
    {
        const string k_ComputeShaderAssetPath =
            "Packages/com.unity.testframework.graphics/Runtime/ImageComparison/EuclideanDistance/EuclideanDistance.compute";
        static readonly ComputeShader k_ComputeShader;
        static readonly int k_TexA = Shader.PropertyToID("_TexA");
        static readonly int k_TexB = Shader.PropertyToID("_TexB");
        static readonly int k_Result = Shader.PropertyToID("_Result");
        static ComputeBuffer s_CachedBuffer;
        static int s_CachedBufferSize;

        /// <summary>
        /// Initializes a new instance of the Euclidean distance algorithm with the given settings.
        /// The Euclidean distance is calculated as the square root of the sum of
        /// the squared differences in each color channel
        /// (R, G, B, and optionally A) between corresponding pixels in the two images.
        /// The average distance across all pixels is compared against the specified maximum distance threshold
        /// to determine if the images are considered a match.
        /// </summary>
        /// <param name="settings">Settings containing the maximum acceptable distance threshold.</param>
        public EuclideanDistance(ITextureComparisonSettings settings)
            : base(settings)
        {
            Description = $"Euclidean distance below {((EuclideanDistanceSettings)settings).MaximumDistance}";
        }

        internal EuclideanDistance() { }

        static EuclideanDistance()
        {
#if UNITY_EDITOR
            k_ComputeShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(k_ComputeShaderAssetPath);
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseBuffer;
#endif
        }

        static void ReleaseBuffer()
        {
            s_CachedBuffer?.Dispose();
            s_CachedBuffer = null;
            s_CachedBufferSize = 0;
        }

        static ComputeBuffer GetOrCreateBuffer(int pixelCount)
        {
            if (s_CachedBuffer != null && s_CachedBufferSize >= pixelCount)
                return s_CachedBuffer;

            s_CachedBuffer?.Dispose();
            s_CachedBuffer = new ComputeBuffer(pixelCount, sizeof(float));
            s_CachedBufferSize = pixelCount;
            return s_CachedBuffer;
        }

        /// <summary>
        /// Compare two texture together using the Euclidean distance algorithm
        /// </summary>
        /// <param name="expected">Expected texture</param>
        /// <param name="actual">Actual texture</param>
        /// <returns>The result of the comparison</returns>
        public override ITextureComparisonResult Compare(Texture2D expected, Texture2D actual)
        {
            if (k_ComputeShader == null)
                throw new InvalidOperationException(
                    "EuclideanDistance compute shader is not loaded. " +
                    "This algorithm requires a compute shader that is only available in the Editor. " +
                    "Use a different comparison algorithm for player builds.");

            if (expected == null || actual == null
                || expected.width != actual.width || expected.height != actual.height)
            {
                var w = expected?.width ?? 1;
                var h = expected?.height ?? 1;
                return new EuclideanDistanceResult(2, 2)
                {
                    Width = w,
                    Height = h,
                    Deltas = CreateFilledArray(2.0f, w * h),
                };
            }

            var width = expected.width;
            var height = expected.height;
            var pixelCount = width * height;
            var deltas = new float[pixelCount];

            var kernel = k_ComputeShader.FindKernel("CSMain");
            var resultBuffer = GetOrCreateBuffer(pixelCount);

            k_ComputeShader.SetTexture(kernel, k_TexA, expected);
            k_ComputeShader.SetTexture(kernel, k_TexB, actual);
            k_ComputeShader.SetBuffer(kernel, k_Result, resultBuffer);

            var threadGroupsX = Mathf.CeilToInt(width / 8f);
            var threadGroupsY = Mathf.CeilToInt(height / 8f);
            k_ComputeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

            resultBuffer.GetData(deltas, 0, 0, pixelCount);

            double total = 0;
            double max = 0;
            for (var i = 0; i < pixelCount; i++)
            {
                double val = deltas[i];
                total += val;
                if (val > max)
                    max = val;
            }

            return new EuclideanDistanceResult(total / pixelCount, max)
            {
                Deltas = deltas,
                Width = width,
                Height = height,
            };
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
        /// Compare two textures together asynchronously.
        /// </summary>
        /// <param name="expected">Expected texture</param>
        /// <param name="actual">Actual texture</param>
        /// <returns>The result of the comparison</returns>
        public override async Task<ITextureComparisonResult> CompareAsync(Texture2D expected, Texture2D actual)
        {
            var shouldReturnMax = false;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                if (expected == null || actual == null)
                    shouldReturnMax = true;
            });

            var width = expected?.width ?? 1;
            var height = expected?.height ?? 1;
            if (shouldReturnMax || expected?.width != actual?.width || expected?.height != actual?.height)
                return new EuclideanDistanceResult(2, 2)
                {
                    Width = width,
                    Height = height,
                    Deltas = CreateFilledArray(2.0f, width * height),
                };

            var pixelCount = width * height;
            var deltas = new float[pixelCount];

            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                if (k_ComputeShader == null)
                    throw new InvalidOperationException("Compute Shader missing...");

                var kernel = k_ComputeShader.FindKernel("CSMain");
                var resultBuffer = GetOrCreateBuffer(pixelCount);

                k_ComputeShader.SetTexture(kernel, k_TexA, expected);
                k_ComputeShader.SetTexture(kernel, k_TexB, actual);
                k_ComputeShader.SetBuffer(kernel, k_Result, resultBuffer);

                var threadGroupsX = Mathf.CeilToInt(width / 8f);
                var threadGroupsY = Mathf.CeilToInt(height / 8f);
                k_ComputeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

                resultBuffer.GetData(deltas, 0, 0, pixelCount);
            });

            double total = 0;
            double max = 0;
            for (var i = 0; i < pixelCount; i++)
            {
                double val = deltas[i];
                total += val;
                if (val > max)
                    max = val;
            }

            return new EuclideanDistanceResult(total / pixelCount, max)
            {
                Deltas = deltas,
                Width = width,
                Height = height,
            };
        }

        ///<inheritdoc />
        public override (object, bool) Evaluate(ITextureComparisonResult result)
        {
            var euclideanResult = (EuclideanDistanceResult)result;
            var settings = (EuclideanDistanceSettings)Settings;
            return euclideanResult.Average <= settings.MaximumDistance ? (result, true) : (result, false);
        }

        static float[] CreateFilledArray(float value, int length)
        {
            var array = new float[length];
            Array.Fill(array, value);
            return array;
        }
    }
}
