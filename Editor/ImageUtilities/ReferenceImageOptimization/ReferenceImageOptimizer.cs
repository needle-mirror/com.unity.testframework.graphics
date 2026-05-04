using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// The ReferenceImageOptimizer class is responsible for optimizing reference images
    /// in Unity projects. It allows you to filter images, compare them, and remove duplicates.
    /// It uses a texture comparison algorithm to determine the similarity between images
    /// and can move images to a base folder for better organization.
    /// The optimizer runs asynchronously and provides progress updates and statistics
    /// about the optimization process.
    /// It also provides events to notify when statistics are gathered and when the optimization is complete.
    /// </summary>
    public sealed class ReferenceImageOptimizer : IDisposable
    {
        static readonly string k_AssetsRefImagePath = PlatformSchema.k_DefaultReferenceImagesRoot;
        static readonly string k_AssetsRefImageBasePath = PlatformSchema.k_DefaultReferenceImagesBaseRoot;
        const int k_ComparisonCacheMultiplier = 1000;
        const int k_TextureCacheMultiplier = 50;
        const int k_RegexTimeoutMs = 100;

        static ReferenceImageOptimizer s_Instance;
        static readonly AtomicInt k_IsRunning = new();

        readonly TaskManager m_TaskManager;
        readonly CancellationTokenSource m_CancellationTokenSource;
        readonly TextureComparisonAlgorithm m_TextureComparer;
        readonly int m_MaxConcurrency;
        readonly string m_Filter;

        readonly ComparisonLruCache m_ComparisonCache;
        readonly TextureLruCache m_TextureCache;

        readonly ConcurrentBag<string> m_RemovedImages;
        readonly ConcurrentBag<(string, string)> m_MovedImages;

        Guid m_ProgressId;
        readonly AtomicInt m_TotalComparisons;
        readonly AtomicInt m_UniqueComparisons;
        readonly AtomicInt m_TotalTextureLoads;
        readonly AtomicInt m_UniqueTextureLoads;
        readonly AtomicInt m_HasCreatedBaseFolder;

        internal IAssetService<Texture2D> AssetService { get; set; } = new AssetDatabaseAssetService<Texture2D>();

        internal bool WriteToDeltaStorage { get; set; } = true;

        internal ReferenceImageOptimizer(string filter, int maxConcurrency)
        {
            m_Filter = filter;
            m_MaxConcurrency = maxConcurrency;

            m_CancellationTokenSource = new CancellationTokenSource();
            m_TaskManager = new TaskManager();
            m_TextureComparer = new EuclideanDistance();
            m_TotalComparisons = new AtomicInt();
            m_UniqueComparisons = new AtomicInt();
            m_TotalTextureLoads = new AtomicInt();
            m_UniqueTextureLoads = new AtomicInt();
            m_HasCreatedBaseFolder = new AtomicInt();
            m_ComparisonCache = new ComparisonLruCache(maxConcurrency * k_ComparisonCacheMultiplier);
            m_TextureCache = new TextureLruCache(maxConcurrency * k_TextureCacheMultiplier, AssetService);
            m_RemovedImages = new ConcurrentBag<string>();
            m_MovedImages = new ConcurrentBag<(string, string)>();
        }

        /// <inheritdoc cref="IDisposable.Dispose()"/>
        public void Dispose()
        {
            m_CancellationTokenSource.Dispose();
            m_TaskManager.Dispose();
            m_ComparisonCache.Clear();
            m_RemovedImages.Clear();
            m_MovedImages.Clear();
            m_TextureCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        internal bool Cancel()
        {
            if (Status != OptimizationStatus.Running)
            {
                return true;
            }

            Status = OptimizationStatus.Cancelled;
            m_CancellationTokenSource.Cancel();
            m_TaskManager.CancelAll();
            Dispose();

            return true;
        }

        /// <summary>
        /// Event that is invoked when the optimizer has gathered statistics for a set of images.
        /// This event provides the test name and metrics for the images, such as platform count and
        /// accumulated divergence.
        /// </summary>
        public static event EventHandler<ImageStatsEventArgs> OnStatsReceived = delegate { };

        /// <summary>
        /// Event that is invoked when the optimization process has completed.
        /// This event provides the final optimization result, including elapsed time, total comparisons,
        /// deleted files, moved files, and the status of the optimization.
        /// </summary>
        public static event EventHandler<OptimizationResult> OnOptimizationComplete = delegate { };

        /// <summary>
        /// Status of the optimization process.
        /// </summary>
        public OptimizationStatus Status { get; internal set; }

        /// <summary>
        /// Starts the optimization process for reference images with a specified filter and maximum concurrency.
        /// This method allows you to specify a filter to match against the reference image names and
        /// the maximum number of concurrent tasks to run during the optimization.
        /// The optimization will run asynchronously, and the progress will be shown in the editor.
        /// </summary>
        /// <param name="filter">
        /// The filter to match against reference image names.
        /// This can be a regular expression that will be used to filter the reference images.
        /// For example, to match all images for a specific test, you can use "TestName".
        /// If you want to match all images, you can pass an empty string.
        /// </param>
        /// <param name="maxConcurrency">
        /// The maximum number of concurrent tasks to run during the optimization.
        /// This allows you to control how many images are processed at the same time.
        /// A higher value may speed up the optimization process, but it may also increase memory usage
        /// and CPU load, so it should be set according to your system's capabilities.
        /// The default value is 8, which is a good balance for most systems.
        /// </param>
        public static void OptimizeReferenceImages(string filter, int maxConcurrency)
        {
            if (!k_IsRunning.TrySet(0, 1))
            {
                GraphicsTestLogger.DebugLog("Reference Image Optimizer is already running");
                return;
            }

            s_Instance = new ReferenceImageOptimizer(filter, maxConcurrency);
            _ = s_Instance
                .RunOptimizer()
                .ContinueWith(
                    task =>
                    {
                        if (task.IsCanceled)
                        {
                            s_Instance.Status = OptimizationStatus.Cancelled;
                            GraphicsTestLogger.Log(LogType.Warning, "Optimization was cancelled.");
                            s_Instance.Dispose();
                            k_IsRunning.Value = 0;
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            s_Instance.Status = OptimizationStatus.Error;
                            if (task.Exception != null)
                                UnityEngine.Debug.LogException(task.Exception.Flatten().InnerException);
                            GraphicsTestLogger.Log(LogType.Error, "Optimization failed with an exception.");
                            s_Instance.Dispose();
                            k_IsRunning.Value = 0;
                            return;
                        }

                        var result = task.Result;
                        result.Status = s_Instance.Status;
                        GraphicsTestLogger.Log(LogType.Log, $"Optimization complete! {result}");
                        s_Instance.Dispose();
                        k_IsRunning.Value = 0;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext()
                );
        }

        internal async Task<OptimizationResult> RunOptimizer()
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            Status = OptimizationStatus.Running;
            m_ProgressId = await m_TaskManager.Register(
                "Reference Image Optimizer",
                m_CancellationTokenSource,
                "Initializing...",
                Guid.Empty,
                Cancel
            );
            await Task.WhenAll(await GenerateTasks());
            sw.Stop();

            await m_TaskManager.UpdateProgress(m_ProgressId, 1f, "Optimization Complete");

            Status = OptimizationStatus.Success;
            var deletedFiles = new List<string>();
            foreach (var path in m_RemovedImages)
                deletedFiles.Add(path);
            var movedFiles = new List<(string, string)>();
            foreach (var pair in m_MovedImages)
                movedFiles.Add(pair);
            var result = new OptimizationResult
            {
                ElapsedTime = sw.Elapsed,
                TotalComparisons = m_TotalComparisons.Value,
                UniqueComparisons = m_UniqueComparisons.Value,
                TotalTextureLoads = m_TotalTextureLoads.Value,
                UniqueTextureLoads = m_UniqueTextureLoads.Value,
                DeletedFiles = deletedFiles,
                MovedFiles = movedFiles,
                Status = Status,
            };

            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                OnOptimizationComplete.Invoke(this, result);
                m_TaskManager.Complete(m_ProgressId);
            });

            return result;
        }

        async Task<IEnumerable<Task>> GenerateTasks()
        {
            await m_TaskManager.UpdateProgress(m_ProgressId, 0.015f, "Looking for reference images...");
            var referenceImagePaths = await FindAssets(k_AssetsRefImagePath, m_Filter);

            await m_TaskManager.UpdateProgress(m_ProgressId, 0.025f, "Looking for base reference images...");
            var referenceImageBasePaths = await FindAssets(k_AssetsRefImageBasePath, m_Filter);

            await m_TaskManager.UpdateProgress(m_ProgressId, 0.075f, "Preparing optimization tasks...");
            var totalSteps = referenceImagePaths.Count;
            var completed = new AtomicInt();
            var semaphore = new SemaphoreSlim(m_MaxConcurrency);

            foreach (var kvp in referenceImageBasePaths)
            {
                if (referenceImagePaths.ContainsKey(kvp.Key))
                    continue;

                await MainThreadDispatcher.RunOnMainThread(() =>
                {
                    OnStatsReceived.Invoke(
                        this,
                        new ImageStatsEventArgs
                        {
                            TestName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(kvp.Value[0])),
                            Metrics = new ReferenceImageMetrics { PlatformCount = 1, AccumulatedDivergence = 0f },
                        }
                    );
                });
            }

            var tasks = new List<Task>();
            foreach (var kvp in referenceImagePaths)
            {
                var testName = kvp.Key;
                var images = kvp.Value;
                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(m_CancellationTokenSource.Token);
                    try
                    {
                        string baseImage = null;
                        if (referenceImageBasePaths.TryGetValue(testName, out var basePaths) && basePaths.Count > 0)
                            baseImage = basePaths[0];
                        await OptimizeTest(testName, images, baseImage);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        GraphicsTestLogger.Log(
                            LogType.Error,
                            $"Optimization failed for '{testName}': {ex.Message}\n{ex.StackTrace}"
                        );
                    }
                    finally
                    {
                        semaphore.Release();
                        var done = completed.Increment();
                        await m_TaskManager.UpdateProgress(m_ProgressId, done, totalSteps, $"Optimized {testName}");
                    }
                });
                tasks.Add(task);
            }
            return tasks;
        }

        async Task OptimizeTest(string testName, List<string> referenceImages, string baseImage)
        {
            var subProgressId = await m_TaskManager.Register(
                testName,
                new CancellationTokenSource(),
                "Starting task...",
                parentTask: m_ProgressId,
                null
            );
            await m_TaskManager.UpdateProgress(subProgressId, 0.1f, "Selecting most common image...");

            baseImage ??= await GetMostCommonImage(referenceImages, subProgressId);

            await m_TaskManager.UpdateProgress(subProgressId, 0.3f, "Comparing images...");

            var initialResults = await CompareImageSets(
                referenceImages,
                new List<string> { baseImage },
                subProgressId,
                writeStats: false
            );

            var excessImages = new List<string>();
            foreach (var r in initialResults)
            {
                if (r.Key != baseImage && r.Value.AccumulatedDivergence == 0)
                    excessImages.Add(r.Key);
            }

            await m_TaskManager.UpdateProgress(subProgressId, 0.6f, "Finalizing assets...");
            var removedImages = await FinalizeReferenceImageSet(baseImage, excessImages, subProgressId);
            var removedSet = new HashSet<string>(removedImages);
            var newReferenceImages = new List<string>();
            foreach (var path in referenceImages)
            {
                if (!removedSet.Contains(path))
                    newReferenceImages.Add(path);
            }
            referenceImages = newReferenceImages;

            var optimizedResults = await CompareImageSets(
                referenceImages,
                new List<string> { baseImage },
                subProgressId,
                writeStats: true
            );

            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                OnStatsReceived.Invoke(
                    this,
                    new ImageStatsEventArgs
                    {
                        TestName = testName,
                        Metrics = new ReferenceImageMetrics
                        {
                            PlatformCount = optimizedResults.Count + 1,
                            AccumulatedDivergence = SumAccumulatedDivergence(optimizedResults),
                        },
                    }
                );
            });

            await MainThreadDispatcher.RunOnMainThread(() => m_TaskManager.Complete(subProgressId));
        }

        async Task<string> GetMostCommonImage(List<string> images, Guid progressId)
        {
            var results = await CompareImageSets(images, images, progressId);
            var sorted = new List<KeyValuePair<string, ReferenceImageMetrics>>(results);
            sorted.Sort(
                (a, b) =>
                {
                    var cmp = a.Value.AccumulatedDivergence.CompareTo(b.Value.AccumulatedDivergence);
                    if (cmp != 0)
                        return cmp;
                    return string.CompareOrdinal(b.Key, a.Key);
                }
            );
            return sorted[0].Key;
        }

        static double SumAccumulatedDivergence(ConcurrentDictionary<string, ReferenceImageMetrics> results)
        {
            double sum = 0;
            foreach (var r in results)
                sum += r.Value.AccumulatedDivergence;
            return sum;
        }

        async Task<List<string>> FinalizeReferenceImageSet(
            string baseImagePath,
            List<string> excessImages,
            Guid subProgressId
        )
        {
            var removedImages = new List<string>();
            if (!baseImagePath.StartsWith(k_AssetsRefImageBasePath, StringComparison.OrdinalIgnoreCase))
            {
                await m_TaskManager.UpdateProgress(subProgressId, 0.7f, "Moving new base image...");
                if (m_HasCreatedBaseFolder.Value == 0)
                {
                    m_HasCreatedBaseFolder.Increment();
                    await AssetService.CreateFolderAsync("Assets", Path.GetFileName(k_AssetsRefImageBasePath));
                }

                var targetPath = Path.Combine(k_AssetsRefImageBasePath, Path.GetFileName(baseImagePath)).SanitizeBackslashes();
                var error = await AssetService.MoveAssetAsync(baseImagePath, targetPath);
                if (!string.IsNullOrEmpty(error))
                {
                    GraphicsTestLogger.Log(LogType.Error, $"Move failed: {error}");
                }
                else
                {
                    m_MovedImages.Add((baseImagePath, targetPath));
                }
            }

            foreach (var path in excessImages)
            {
                await m_TaskManager.UpdateProgress(subProgressId, 0.8f, "Deleting excess images...");
                var deleteSuccess = await AssetService.DeleteAssetAsync(path);
                if (deleteSuccess)
                {
                    removedImages.Add(path);
                    m_RemovedImages.Add(path);

                    await RemoveEmptyParentFoldersAsync(path, k_AssetsRefImagePath);
                }
                else
                {
                    GraphicsTestLogger.Log(LogType.Error, $"Could not delete: {path}");
                }
            }

            return removedImages;
        }

        async Task RemoveEmptyParentFoldersAsync(string startingPath, string root)
        {
            var currentDir = Path.GetDirectoryName(startingPath);
            while (!string.IsNullOrEmpty(currentDir) && currentDir.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                var assets = await AssetService.FindAssetsAsync(currentDir, String.Empty);
                var hasAssets = false;
                foreach (var _ in assets)
                {
                    hasAssets = true;
                    break;
                }
                if (hasAssets)
                    break;

                var success = await AssetService.DeleteAssetAsync(currentDir);
                if (!success)
                {
                    GraphicsTestLogger.Log(LogType.Warning, $"Failed to remove empty folder: {currentDir}");
                    break;
                }

                currentDir = Path.GetDirectoryName(currentDir);
            }
        }

        async Task<ConcurrentDictionary<string, ReferenceImageMetrics>> CompareImageSets(
            List<string> a,
            List<string> b,
            Guid progressId,
            bool writeStats = false
        )
        {
            var results = new ConcurrentDictionary<string, ReferenceImageMetrics>();
            var total = a.Count * b.Count;
            var done = new AtomicInt();
            var semaphore = new SemaphoreSlim(m_MaxConcurrency);

            var tasks = new List<Task>();
            foreach (var pathA in a)
            {
                var task = Task.Run(async () =>
                {
                    var metrics = new List<ITextureComparisonResult>();

                    foreach (var pathB in b)
                    {
                        await semaphore.WaitAsync(m_CancellationTokenSource.Token);
                        try
                        {
                            await m_TaskManager.UpdateProgress(
                                progressId,
                                done.Increment(),
                                total,
                                $"Comparing {pathA} to {pathB}..."
                            );

                            var first = string.CompareOrdinal(pathA, pathB) <= 0 ? pathA : pathB;
                            var second = ReferenceEquals(first, pathA) ? pathB : pathA;

                            var result = await CompareAndCache(first, second);
                            if (result != null)
                            {
                                metrics.Add(result);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }

                    var hasMetrics = false;
                    double avgSum = 0;
                    var avgCount = 0;
                    foreach (var m in metrics)
                    {
                        hasMetrics = true;
                        avgSum += ((EuclideanDistanceResult)m).Average;
                        avgCount++;
                    }
                    results[pathA] = hasMetrics
                        ? new ReferenceImageMetrics
                        {
                            PlatformCount = metrics.Count,
                            AccumulatedDivergence = avgCount > 0 ? avgSum / avgCount : 0,
                        }
                        : new ReferenceImageMetrics();

                    if (WriteToDeltaStorage && writeStats && hasMetrics)
                    {
                        await WriteToDeltaCache(metrics, Path.GetFileNameWithoutExtension(pathA));
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            return results;
        }

        async Task<ITextureComparisonResult> CompareAndCache(string a, string b)
        {
            if (a == b)
                return null;

            m_TotalComparisons.Increment();

            var key = string.Join(";", a, b);
            if (m_ComparisonCache.TryGet(key, out var result))
                return result;

            var aTex = await LoadAndCacheTexture(a);
            var bTex = await LoadAndCacheTexture(b);
            result = await m_TextureComparer.CompareAsync(aTex, bTex);

            if (m_ComparisonCache.TryAdd(key, result))
                m_UniqueComparisons.Increment();

            return result;
        }

        async Task<Texture2D> LoadAndCacheTexture(string path)
        {
            m_TotalTextureLoads.Increment();
            if (m_TextureCache.TryGet(path, out var tex))
                return tex;

            tex = await AssetService.LoadAssetAtPathAsync(path);
            if (m_TextureCache.TryAdd(path, tex))
            {
                m_UniqueTextureLoads.Increment();
            }

            return tex;
        }

        async Task<Dictionary<string, List<string>>> FindAssets(string searchPath, string filter)
        {
            if (!await AssetService.IsValidFolderAsync(searchPath))
                return new Dictionary<string, List<string>>();

            Dictionary<string, List<string>> referenceImagePaths = new();
            var assets = await AssetService.FindAssetsAsync(searchPath, string.Empty);

            foreach (var asset in assets)
            {
                if (!Regex.IsMatch(asset, filter, RegexOptions.None, TimeSpan.FromMilliseconds(k_RegexTimeoutMs)))
                    continue;

                var testName = Path.GetFileNameWithoutExtension(asset);
                if (referenceImagePaths.TryGetValue(testName, out var paths))
                {
                    paths.Add(asset);
                }
                else
                {
                    referenceImagePaths.Add(testName, new List<string> { asset });
                }
            }

            return referenceImagePaths;
        }

        static async Task WriteToDeltaCache(List<ITextureComparisonResult> metrics, string name)
        {
            await Task.Run(() =>
            {
                var deltaEMetrics = new List<EuclideanDistanceResult>();
                foreach (var s in metrics)
                {
                    var m = (EuclideanDistanceResult)s;
                    if (m.Deltas.Length != 0)
                        deltaEMetrics.Add(m);
                }
                if (deltaEMetrics.Count == 0)
                    return;
                var width = deltaEMetrics[0].Width;
                var height = deltaEMetrics[0].Height;
                var pixelCount = width * height;

                var summed = ArrayPool<float>.Shared.Rent(pixelCount);
                Array.Clear(summed, 0, pixelCount);

                foreach (var metric in deltaEMetrics)
                {
                    for (var i = 0; i < pixelCount; i++)
                        summed[i] += metric.Deltas[i];
                }

                var averaged = ArrayPool<float>.Shared.Rent(pixelCount);
                var count = deltaEMetrics.Count;
                for (var i = 0; i < pixelCount; i++)
                    averaged[i] = summed[i] / count;

                DeltaCache.EnqueueWrite(name, width, height, averaged);

                foreach (var metric in deltaEMetrics)
                {
                    metric.ClearTemporary();
                }

                ArrayPool<float>.Shared.Return(summed, clearArray: true);
            });
        }
    }
}
