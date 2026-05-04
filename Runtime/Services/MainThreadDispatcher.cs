using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace UnityEngine.TestTools.Graphics
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class MainThreadDispatcher
    {
        const int k_MaxActionsPerFrame = 8;

        static SynchronizationContext s_UnityContext;
        static readonly ConcurrentQueue<Action> k_QueuedActions = new();
        static bool s_Initialized;

#if !UNITY_EDITOR
        class PlayerLoopUpdater : MonoBehaviour
        {
            void Awake()
            {
                // Ensure only one instance exists and it persists across scene loads
                if (FindObjectsByType<PlayerLoopUpdater>().Length > 1)
                {
                    Destroy(gameObject);
                    return;
                }
                DontDestroyOnLoad(gameObject);
            }

            void Update()
            {
                ProcessQueue();
            }
        }
#endif

        static MainThreadDispatcher()
        {
#if UNITY_EDITOR
            Initialize();
#endif
        }

        internal static void Initialize()
        {
            if (s_Initialized)
                return;
            s_Initialized = true;
            s_UnityContext = SynchronizationContext.Current;

#if UNITY_EDITOR
            EditorApplication.update += ProcessQueue;
#else
            var go = new GameObject("MainThreadDispatcherService");
            go.AddComponent<PlayerLoopUpdater>();
#endif
        }

        static void ProcessQueue()
        {
            var processed = 0;
            while (processed < k_MaxActionsPerFrame && k_QueuedActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    // Log exceptions from dispatched actions
                    Debug.LogError($"[MainThreadDispatcher] Exception executing action: {ex}");
                }
                processed++;
            }
        }

        internal static Task RunOnMainThread(Action action, TimeSpan? timeout = null)
        {
            if (action == null)
                return Task.CompletedTask;

            if (!s_Initialized)
            {
                Initialize();
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Observe faults so fire-and-forget callers don't trigger
            // TaskScheduler.UnobservedTaskException on GC finalization.
            tcs.Task.ContinueWith(
                static t => { _ = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted
            );

            void WrappedAction()
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            if (s_UnityContext != null && SynchronizationContext.Current == s_UnityContext)
            {
                WrappedAction();
            }
            else if (s_UnityContext != null)
            {
                try
                {
                    s_UnityContext.Post(_ => WrappedAction(), null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    k_QueuedActions.Enqueue(WrappedAction);
#if UNITY_EDITOR
                    EditorApplication.QueuePlayerLoopUpdate();
#endif
                }
            }
            else
            {
                k_QueuedActions.Enqueue(WrappedAction);
#if UNITY_EDITOR
                EditorApplication.QueuePlayerLoopUpdate();
#endif
            }

            if (!timeout.HasValue)
                return tcs.Task;

            var cts = new CancellationTokenSource(timeout.Value);
            var registration = cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            tcs.Task.ContinueWith(
                _ =>
                {
                    registration.Dispose();
                    cts.Dispose();
                },
                TaskScheduler.Default
            );

            return tcs.Task;
        }
    }
}
