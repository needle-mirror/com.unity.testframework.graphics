#if UNITY_6000_6_OR_NEWER
using System;
using NUnit.Framework.Internal.Commands;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics.GraphicsApiValidation
{
    /// <summary>
    /// Validates D3D12 API usage during test execution.
    /// Detects validation layer errors and fails tests that trigger them.
    /// </summary>
    internal class D3D12ValidationCommand : GraphicsApiValidationCommand
    {
        const string k_CommandLineArg = "-force-d3d12-debug-as-errors";

        static bool? s_IsValidationActive;
        static bool s_ValidationCheckComplete;
        static bool s_ValidationUnavailableWarningLogged;

        internal static bool IsEnabled { get; } = Array.Exists(
            Environment.GetCommandLineArgs(),
            arg => arg == k_CommandLineArg);

        public D3D12ValidationCommand(TestCommand innerCommand) : base(innerCommand) { }

        protected override string ValidationPrefix => "D3D12";

        protected override bool IsValidationActive()
        {
            if (!IsEnabled)
                return false;

            if (s_ValidationCheckComplete && s_IsValidationActive == false)
            {
                if (!s_ValidationUnavailableWarningLogged)
                {
                    GraphicsTestLogger.LogWarning($"[{ValidationPrefix}] Skipping validation - debug layer unavailable");
                    s_ValidationUnavailableWarningLogged = true;
                }
                return false;
            }

            if (!s_IsValidationActive.HasValue)
            {
                var graphicsDeviceType = GraphicsTestPlatform.Current.GetValue<GraphicsDeviceType>();

                if (graphicsDeviceType == GraphicsDeviceType.Direct3D12)
                {
                    bool requested = D3D12Validation.IsValidationRequested();
                    bool active = D3D12Validation.IsValidationActive();

                    if (requested && !active)
                    {
                        s_ValidationCheckComplete = true;
                        s_IsValidationActive = false;
                        throw new InvalidOperationException(
                            $"{ValidationPrefix} validation was requested via {k_CommandLineArg} but is not active. " +
                            "Install Graphics Tools via Windows Settings -> System -> Optional Features.");
                    }

                    s_IsValidationActive = active;
                }
                else
                {
                    s_IsValidationActive = false;
                    s_ValidationUnavailableWarningLogged = true;
                }

                s_ValidationCheckComplete = true;
            }

            return s_IsValidationActive.Value;
        }

        protected override void ClearErrors()
        {
            try
            {
                D3D12Validation.ClearValidationErrors();
            }
            catch (Exception e)
            {
                GraphicsTestLogger.LogWarning($"[{ValidationPrefix}] Failed to clear errors: {e.Message}");
            }
        }

        protected override void SetLoggingSuppressed(bool suppressed)
        {
            try
            {
                D3D12Validation.SetValidationErrorLoggingSuppressed(suppressed);
            }
            catch (Exception e)
            {
                GraphicsTestLogger.LogWarning($"[{ValidationPrefix}] Failed to set logging suppression: {e.Message}");
            }
        }

        protected override int GetErrorCount()
        {
            return D3D12Validation.GetValidationErrorCount();
        }

        protected override string GetError(int index)
        {
            return D3D12Validation.GetValidationError(index);
        }

        protected override int GetDroppedErrorCount()
        {
            return D3D12Validation.GetValidationErrorsDroppedCount();
        }
    }
}
#endif
