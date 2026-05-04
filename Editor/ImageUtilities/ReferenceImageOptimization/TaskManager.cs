using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    sealed class TaskManager : IDisposable
    {
        readonly ConcurrentDictionary<Guid, ProgressStatus> m_Tasks = new();

        internal async Task<Guid> Register(
            string name,
            CancellationTokenSource source,
            string description,
            Guid parentTask,
            Func<bool> cancelCallback
        )
        {
            var guid = Guid.NewGuid();
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var parentId = m_Tasks.GetValueOrDefault(parentTask)?.ProgressId ?? -1;
                var progressId = Progress.Start(name, description, parentId: parentId);

                Progress.SetTimeDisplayMode(progressId, Progress.TimeDisplayMode.ShowRemainingTime);

                if (cancelCallback != null)
                {
                    Progress.RegisterCancelCallback(progressId, cancelCallback);
                }
                else
                {
                    Progress.RegisterCancelCallback(
                        progressId,
                        () =>
                        {
                            source.Cancel();
                            return true;
                        }
                    );
                }

                m_Tasks.TryAdd(
                    guid,
                    new ProgressStatus
                    {
                        Name = name,
                        ProgressId = progressId,
                        Source = source,
                    }
                );
            });
            return guid;
        }

        internal async Task UpdateProgress(Guid id, float progress, string description)
        {
            if (m_Tasks.TryGetValue(id, out var status))
            {
                await MainThreadDispatcher.RunOnMainThread(() =>
                {
                    Progress.Report(status.ProgressId, progress, description);
                });
            }
        }

        internal async Task UpdateProgress(Guid id, int currentStep, int totalSteps, string description)
        {
            if (m_Tasks.TryGetValue(id, out var status))
            {
                await MainThreadDispatcher.RunOnMainThread(() =>
                {
                    Progress.Report(status.ProgressId, currentStep, totalSteps, description);
                });
            }
        }

        internal void Complete(Guid id)
        {
            if (m_Tasks.TryRemove(id, out var status))
            {
                Progress.Finish(status.ProgressId);
                status.Dispose();
            }
        }

        internal void CancelAll()
        {
            foreach (var kvp in m_Tasks.ToArray())
            {
                Cancel(kvp.Key);
            }
        }

        void Cancel(Guid guid)
        {
            if (m_Tasks.TryGetValue(guid, out var status) && Progress.Exists(status.ProgressId))
            {
                Progress.Cancel(status.ProgressId);
            }
        }

        internal IEnumerable<KeyValuePair<Guid, ProgressStatus>> GetSnapshot()
        {
            return m_Tasks.ToArray();
        }

        public void Dispose()
        {
            foreach (var kvp in m_Tasks.ToArray())
            {
                kvp.Value.Dispose();
                m_Tasks.TryRemove(kvp.Key, out _);
            }
        }
    }

    sealed class ProgressStatus : IDisposable
    {
        public int ProgressId;
        public string Name;
        public CancellationTokenSource Source;

        public void Dispose()
        {
            Source?.Dispose();
        }
    }
}
