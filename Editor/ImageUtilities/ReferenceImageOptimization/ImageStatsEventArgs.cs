using System;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Event arguments for the <see cref="ReferenceImageOptimizer.OnStatsReceived"/> event.
    /// Contains the test name and the metrics for the reference image optimization.
    /// This event is triggered when the optimization process provides statistics about the reference images.
    /// </summary>
    public class ImageStatsEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the test for which the reference image metrics are reported.
        /// This is typically the name of the test case that the reference images belong to.
        /// </summary>
        public string TestName { get; init; }

        /// <summary>
        /// The metrics associated with the reference images for the specified test.
        /// </summary>
        public ReferenceImageMetrics Metrics { get; init; }
    }
}
