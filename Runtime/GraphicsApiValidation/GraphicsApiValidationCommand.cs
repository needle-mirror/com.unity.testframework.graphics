#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEngine.Rendering;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools.Graphics.GraphicsApiValidation
{
    /// <summary>
    /// Abstract base class for graphics API validation commands.
    /// Wraps test execution to detect validation errors from graphics APIs (D3D12, Metal, Vulkan, etc.)
    /// Before adding a custom implementation for this, consider instead if you should implement Rendering.GraphicsApiValidation on the relevant GfxDevice
    /// Doing so will allow you to use ManagedApiValidationCommand
    /// </summary>
    internal abstract class GraphicsApiValidationCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        static bool? s_SupportsFences;

        protected GraphicsApiValidationCommand(TestCommand innerCommand) : base(innerCommand) { }

        /// <summary>
        /// Maximum number of errors to display in the failure message.
        /// </summary>
        protected virtual int MaxErrorsToDisplay => 10;

        /// <summary>
        /// Timeout in milliseconds when waiting for GPU completion using a fence.
        /// </summary>
        protected virtual int GPUSyncTimeoutMs => 5000;

        /// <summary>
        /// Fallback sleep time in milliseconds when fences are not supported.
        /// </summary>
        protected virtual int FallbackSleepMs => 500;

        /// <summary>
        /// Returns true if validation is currently active and errors should be checked.
        /// </summary>
        protected abstract bool IsValidationActive();

        /// <summary>
        /// Clears any accumulated validation errors before test execution.
        /// </summary>
        protected abstract void ClearErrors();

        /// <summary>
        /// Returns if validation error logging to the console is currently suppressed.
        /// </summary>
        protected abstract bool IsLoggingSuppressed();

        /// <summary>
        /// Sets whether validation error logging to the console is suppressed.
        /// </summary>
        protected abstract void SetLoggingSuppressed(bool suppressed);

        /// <summary>
        /// Returns the number of validation errors accumulated.
        /// </summary>
        protected abstract int GetErrorCount();

        /// <summary>
        /// Returns the validation error message at the specified index.
        /// </summary>
        protected abstract string GetError(int index);

        /// <summary>
        /// Returns the number of errors that were dropped due to buffer overflow.
        /// </summary>
        protected abstract int GetDroppedErrorCount();

        /// <summary>
        /// Returns a prefix string used in error messages (e.g., "D3D12", "Metal").
        /// </summary>
        protected abstract string ValidationPrefix { get; }

        /// <summary>
        /// Returns true if validation layers have been requested but for some reason are not active
        /// Returns false if validation layers are not requested, or if they are requested & active
        /// </summary>
        protected abstract bool GraphicsLayersMisconfigured();

        /// <summary>
        /// Throws if GraphicsLayersMisconfigured returns true
        /// </summary>
        private void ThrowIfMisconfigured()
        {
            if (GraphicsLayersMisconfigured())
                throw new InvalidOperationException(
                    "Validation has been requested but is not active. Double check this project has been setup correctly for validation layers. For example, Vulkan validation layers require libVkLayer_khronos_validation.so or the Vulkan SDK.");
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            ThrowIfMisconfigured();
            if (!IsValidationActive())
            {
                innerCommand.Execute(context);
                return context.CurrentResult;
            }

            using var _ = new WithLogSuppression(this);
            try
            {
                ClearErrors();
                innerCommand.Execute(context);
            }
            finally
            {
                if (!ShouldSkipValidation(context.CurrentResult))
                    CheckValidationErrors(context.CurrentResult);
            }

            return context.CurrentResult;
        }

        private struct WithLogSuppression: IDisposable
        {
            GraphicsApiValidationCommand m_Command;
            bool m_SuppressState;

            public WithLogSuppression(GraphicsApiValidationCommand commmand)
            {
                m_SuppressState = commmand.IsLoggingSuppressed();
                m_Command = commmand;
                m_Command.SetLoggingSuppressed(true);
            }

            public void Dispose()
            {
                m_Command.SetLoggingSuppressed(m_SuppressState);
            }
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            ThrowIfMisconfigured();
            if (!(innerCommand is IEnumerableTestMethodCommand enumerableCommand))
            {
                try
                {
                    Execute(context);
                }
                catch (Exception e)
                {
                    context.CurrentResult.RecordException(e);
                }
                yield break;
            }

            if (!IsValidationActive())
            {
                foreach (var item in enumerableCommand.ExecuteEnumerable(context))
                    yield return item;
                yield break;
            }

            using var _ = new WithLogSuppression(this);
            try
            {
                ClearErrors();

                foreach (var item in enumerableCommand.ExecuteEnumerable(context))
                    yield return item;
            }
            finally
            {
                if (!ShouldSkipValidation(context.CurrentResult))
                    CheckValidationErrors(context.CurrentResult);
            }
        }

        static bool ShouldSkipValidation(TestResult result)
        {
            return result.ResultState == ResultState.Ignored ||
                   result.ResultState == ResultState.Skipped ||
                   result.ResultState.Status == TestStatus.Skipped ||
                   result.ResultState.Status == TestStatus.Inconclusive;
        }

        void CheckValidationErrors(TestResult result)
        {
            try
            {
                FlushGPU();
                WaitForGPUCompletion();

                int errorCount = GetErrorCount();
                if (errorCount > 0)
                    result.RecordException(new AssertionException(BuildErrorMessage(errorCount)));
            }
            catch (Exception e)
            {
                GraphicsTestLogger.LogWarning($"[{ValidationPrefix}] Failed to check errors: {e.Message}");
            }
        }

        /// <summary>
        /// Flushes pending GPU commands. Override for API-specific flush behavior.
        /// </summary>
        protected virtual void FlushGPU()
        {
            using (var cmd = new CommandBuffer())
                UnityEngine.Graphics.ExecuteCommandBuffer(cmd);

            GL.Flush();
        }

        /// <summary>
        /// Waits for GPU operations to complete. Override for API-specific synchronization.
        /// </summary>
        protected virtual void WaitForGPUCompletion()
        {
            s_SupportsFences ??= SystemInfo.supportsGraphicsFence;

            if (s_SupportsFences.Value)
                WaitWithFence();
            else
                System.Threading.Thread.Sleep(FallbackSleepMs);
        }

        void WaitWithFence()
        {
            try
            {
                var fence = UnityEngine.Graphics.CreateGraphicsFence(
                    GraphicsFenceType.CPUSynchronisation,
                    SynchronisationStageFlags.AllGPUOperations);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (!fence.passed && sw.ElapsedMilliseconds < GPUSyncTimeoutMs)
                    System.Threading.Thread.Sleep(1);
            }
            catch
            {
                System.Threading.Thread.Sleep(FallbackSleepMs);
            }
        }

        string BuildErrorMessage(int errorCount)
        {
            int droppedCount = GetDroppedErrorCount();
            var sb = new StringBuilder();
            sb.AppendLine($"{ValidationPrefix} validation detected {errorCount + droppedCount} error(s):");

            int displayCount = Math.Min(errorCount, MaxErrorsToDisplay);
            for (int i = 0; i < displayCount; i++)
            {
                string msg = GetError(i);
                if (!string.IsNullOrEmpty(msg))
                    sb.AppendLine($"  [{i + 1}] {msg}");
            }

            if (errorCount > MaxErrorsToDisplay)
                sb.AppendLine($"  ... and {errorCount - MaxErrorsToDisplay} more");

            if (droppedCount > 0)
                sb.AppendLine($"  ... and {droppedCount} dropped (buffer overflow)");

            return sb.ToString();
        }
    }
}
#endif
