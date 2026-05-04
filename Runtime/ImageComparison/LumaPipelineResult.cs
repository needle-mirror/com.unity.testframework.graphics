using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Contains the luma values on an actual and expect image. This is an output of the LumePipeline and
    /// used both by the SSIM and PSNR algorithm.
    /// </summary>
    public struct LumaPipelineResult : IDisposable
    {
        /// <summary>
        /// Expected image pixels
        /// </summary>
        internal NativeArray<Color32> ExpectedPixels { get; init; }

        /// <summary>
        /// Actual image pixels (RGBA8).
        /// </summary>
        internal NativeArray<Color32> ActualPixels { get; init; }

        /// <summary>
        /// Per-pixel luma values computed from the expected image.
        /// </summary>
        public NativeArray<float> ExpectedLuma { get; init; }

        /// <summary>
        /// Per-pixel luma values computed from the actual image.
        /// </summary>
        public NativeArray<float> ActualLuma { get; init; }

        /// <summary>
        /// Per-pixel luma difference (e.g., Actual - Expected) or another delta definition used by the pipeline.
        /// </summary>
        public NativeArray<float> DeltaLuma { get; init; }

        /// <summary>
        /// Job handle representing the luma computation work. Call Complete() before accessing luma arrays.
        /// </summary>
        public JobHandle Handle { get; init; }

        /// <summary>
        /// Image width in pixels for the luma buffers.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Image height in pixels for the luma buffers.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Ensure that the luma job has completed.
        /// </summary>
        public void Complete()
        {
            Handle.Complete();
        }

        /// <summary>
        /// Disposes of the different fields of the luma job.
        /// </summary>
        public void Dispose()
        {
            if (ExpectedPixels.IsCreated)
                ExpectedPixels.Dispose();
            if (ActualPixels.IsCreated)
                ActualPixels.Dispose();
            if (ExpectedLuma.IsCreated)
                ExpectedLuma.Dispose();
            if (ActualLuma.IsCreated)
                ActualLuma.Dispose();
            if (DeltaLuma.IsCreated)
                DeltaLuma.Dispose();
        }
    }
}
