using System.Collections.Concurrent;
using UnityEngine.TestTools.Graphics;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        Button m_OptimizeImagesButton;

        readonly ConcurrentDictionary<string, ReferenceImageMetrics> m_ReferenceImageMetrics = new();

        ReferenceImageOptimizer m_ImageOptimizer;

        void SetupOptimization()
        {
            m_OptimizeImagesButton = m_Root.Q<Button>("OptimizeImagesButton");
            m_OptimizeImagesButton.clickable.clickedWithEventInfo += (_) =>
            {
                ReferenceImageOptimizer.OnOptimizationComplete += OnOptimizationComplete;
                ReferenceImageOptimizer.OptimizeReferenceImages(
                    "",
                    GraphicsTestBuildSettings.LoadOrDefault().MaxConcurrentImageOptimizations
                );
                Progress.ShowDetails();
            };

            // Receive stats from optimizations no matter the source
            ReferenceImageOptimizer.OnStatsReceived += OnStatsReceived;
        }

        void OnStatsReceived(object sender, ImageStatsEventArgs e)
        {
            m_ReferenceImageMetrics[e.TestName] = e.Metrics;
            BuildFullTreeModel();
        }

        void OnOptimizationComplete(object sender, OptimizationResult r)
        {
            ReferenceImageOptimizer.OnOptimizationComplete -= OnOptimizationComplete;
            GraphicsTestLogger.DebugLog(r.ToString());
        }
    }
}
