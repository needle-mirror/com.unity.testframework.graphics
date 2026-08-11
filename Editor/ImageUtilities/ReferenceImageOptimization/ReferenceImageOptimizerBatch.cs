using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Headless entry point (invoked via <c>-batchmode -executeMethod</c>) that runs the
    /// <see cref="ReferenceImageOptimizer"/> across the currently open project's reference images to
    /// completion and then quits the editor. Used by the weekly "Import SRP Test Projects" Yamato job.
    /// </summary>
    /// <remarks>
    /// Do NOT combine this with <c>-quit</c>. The optimizer runs asynchronously and its main-thread
    /// continuations are pumped by <see cref="EditorApplication.update"/>, which only runs while the
    /// editor stays alive - so <c>-quit</c> would tear the editor down before it finishes. This method
    /// quits the editor itself via <see cref="EditorApplication.Exit"/> once optimization completes, and
    /// a watchdog guarantees a stalled run can never hang CI.
    /// </remarks>
    public static class ReferenceImageOptimizerBatch
    {
        // Empty regex matches every reference image, i.e. optimize the whole project.
        const string k_OptimizeAllFilter = "";

        // Safety net: quit even if the optimization never signals completion.
        static readonly TimeSpan k_Timeout = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Optimizes every reference image in the open project, then exits the editor (0 on success,
        /// 1 on failure or timeout).
        /// </summary>
        public static void Run()
        {
            var maxConcurrency = GraphicsTestBuildSettings.LoadOrDefault().MaxConcurrentImageOptimizations;
            GraphicsTestLogger.Log($"[ReferenceImageOptimizerBatch] Starting (maxConcurrency={maxConcurrency}).");

            var optimizer = new ReferenceImageOptimizer(k_OptimizeAllFilter, maxConcurrency)
            {
                // The delta heatmap cache only feeds the interactive GraphicsTestsWindow; CI just wants the
                // deduplicated/moved reference images, so don't spend time writing it.
                WriteToDeltaStorage = false,
            };
            var startUtc = DateTime.UtcNow;
            var exited = false;

            void Exit(int code)
            {
                if (exited)
                    return;
                exited = true;
                EditorApplication.update -= Watchdog;
                optimizer.Dispose();
                EditorApplication.Exit(code);
            }

            void Watchdog()
            {
                if (DateTime.UtcNow - startUtc <= k_Timeout)
                    return;
                GraphicsTestLogger.LogError($"[ReferenceImageOptimizerBatch] Timed out after {k_Timeout}; quitting.");
                Exit(1);
            }

            EditorApplication.update += Watchdog;

            optimizer.RunOptimizer().ContinueWith(
                task =>
                {
                    if (task.IsFaulted)
                    {
                        GraphicsTestLogger.LogError("[ReferenceImageOptimizerBatch] Optimization faulted.");
                        if (task.Exception != null)
                            Debug.LogException(task.Exception.Flatten().InnerException ?? task.Exception);
                        Exit(1);
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        GraphicsTestLogger.LogWarning("[ReferenceImageOptimizerBatch] Optimization was cancelled.");
                        Exit(1);
                        return;
                    }

                    GraphicsTestLogger.Log($"[ReferenceImageOptimizerBatch] Complete: {task.Result}");
                    WriteStatsFile(task.Result);
                    Exit(task.Result.Status == OptimizationStatus.Success ? 0 : 1);
                },
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        // Opt-in machine-readable summary for CI: when REFERENCE_IMAGE_OPTIMIZER_STATS_FILE is set, write the
        // removed/moved counts as flat JSON so the calling job can surface them (e.g. in a PR body). No-op
        // when the env var is unset, so this is safe for interactive/other callers.
        static void WriteStatsFile(OptimizationResult result)
        {
            var path = Environment.GetEnvironmentVariable("REFERENCE_IMAGE_OPTIMIZER_STATS_FILE");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var json = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{{\"status\":\"{0}\",\"deleted\":{1},\"moved\":{2},\"elapsedSeconds\":{3:0.###}}}",
                    result.Status, result.DeletedFiles.Count, result.MovedFiles.Count, result.ElapsedTime.TotalSeconds);
                System.IO.File.WriteAllText(path, json);
                GraphicsTestLogger.Log($"[ReferenceImageOptimizerBatch] Wrote stats to {path}: {json}");
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.LogWarning($"[ReferenceImageOptimizerBatch] Could not write stats file '{path}': {ex.Message}");
            }
        }
    }
}
