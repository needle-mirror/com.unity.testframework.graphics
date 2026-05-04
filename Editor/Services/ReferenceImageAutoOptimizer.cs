using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Services
{
    [InitializeOnLoad]
    class ReferenceImageAutoOptimizer : AssetPostprocessor
    {
        static readonly ConcurrentBag<string> k_AssetBuffer = new();
        static ReferenceImageOptimizer s_ReferenceImageOptimizer;
        static DateTime s_LastAssetChange = DateTime.MinValue;
        static CancellationTokenSource s_DebounceTokenSource;

        static readonly TimeSpan k_DebounceDelay = TimeSpan.FromSeconds(1);

        static ReferenceImageAutoOptimizer()
        {
#if GRAPHICS_TEST_FRAMEWORK_DEBUG
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Debug.LogException(e.Exception);
                e.SetObserved();
            };
#endif
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            if (!GraphicsTestBuildSettings.LoadOrDefault().AutoOptimizeReferenceImages)
                return;

            var relevantAssets = new List<string>();
            foreach (var a in importedAssets)
            {
                if (a.Contains(PlatformSchema.k_DefaultReferenceImagesRoot) && (a.EndsWith(".png") || a.EndsWith(".exr")))
                    relevantAssets.Add(Path.GetFileNameWithoutExtension(a));
            }
            foreach (var a in deletedAssets)
            {
                if (a.Contains(PlatformSchema.k_DefaultReferenceImagesRoot) && (a.EndsWith(".png") || a.EndsWith(".exr")))
                    relevantAssets.Add(Path.GetFileNameWithoutExtension(a));
            }
            foreach (var a in movedAssets)
            {
                if (a.Contains(PlatformSchema.k_DefaultReferenceImagesRoot) && (a.EndsWith(".png") || a.EndsWith(".exr")))
                    relevantAssets.Add(Path.GetFileNameWithoutExtension(a));
            }

            foreach (var asset in relevantAssets)
            {
                k_AssetBuffer.Add(asset);
            }

            s_LastAssetChange = DateTime.UtcNow;

            // Cancel and dispose any existing debounce task
            if (s_DebounceTokenSource != null)
            {
                s_DebounceTokenSource.Cancel();
                s_DebounceTokenSource.Dispose();
            }
            s_DebounceTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                var token = s_DebounceTokenSource.Token;

                try
                {
                    await Task.Delay(k_DebounceDelay, token);
                    if (token.IsCancellationRequested)
                        return;

                    // If nothing changed since, trigger the file event
                    if ((DateTime.UtcNow - s_LastAssetChange) >= k_DebounceDelay)
                    {
                        TriggerOptimizationFromBuffer();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when a new event resets the debounce timer
                }
            });
        }

        static void TriggerOptimizationFromBuffer()
        {
            if (k_AssetBuffer.IsEmpty)
                return;

            // Grab all asset names and clear buffer
            var uniqueAssets = new HashSet<string>();
            while (k_AssetBuffer.TryTake(out var asset))
                uniqueAssets.Add(asset);

            var filter = "(" + string.Join("|", uniqueAssets) + ")";

            OnFileEventTriggered(filter);
        }

        static void OnFileEventTriggered(string filter)
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                ReferenceImageOptimizer.OnOptimizationComplete += OnOptimizationComplete;
                ReferenceImageOptimizer.OptimizeReferenceImages(
                    filter,
                    GraphicsTestBuildSettings.LoadOrDefault().MaxConcurrentImageOptimizations
                );
            });
        }

        static void OnOptimizationComplete(object sender, OptimizationResult r)
        {
            ReferenceImageOptimizer.OnOptimizationComplete -= OnOptimizationComplete;
            GraphicsTestLogger.Log(r.ToString());
        }
    }
}
