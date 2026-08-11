#if UNITY_6000_6_OR_NEWER
using System;
using NUnit.Framework.Internal.Commands;

namespace UnityEngine.TestTools.Graphics.GraphicsApiValidation
{
    /// <summary>
    /// Base command that handles GfxDevices which have managed Rendering.GraphicsApiValidation implemented
    /// </summary>
    internal class ManagedApiValidationCommand : GraphicsApiValidationCommand
    {
        protected override string ValidationPrefix => "Rendering.GraphicsApiValidation";

        // because the root command checks IsValidationActive before actually trying to check validation layers
        // we can mark ourselves enabled if the GfxDevice actually implements the API
        public static bool IsEnabled => Rendering.GraphicsApiValidation.IsValidationSupported();

        public ManagedApiValidationCommand(TestCommand innerCommand) : base(innerCommand) { }

        protected override bool IsValidationActive()
        {
            return Rendering.GraphicsApiValidation.IsValidationActive();
        }

        protected override void ClearErrors()
        {
            Rendering.GraphicsApiValidation.ClearValidationErrors();
        }

        protected override bool IsLoggingSuppressed()
        {
            return Rendering.GraphicsApiValidation.IsValidationErrorLoggingSuppressed();
        }

        protected override void SetLoggingSuppressed(bool suppressed)
        {
            Rendering.GraphicsApiValidation.SetValidationErrorLoggingSuppressed(suppressed);
        }

        protected override int GetErrorCount()
        {
            return Rendering.GraphicsApiValidation.GetValidationErrorCount();
        }

        protected override string GetError(int index)
        {
            return Rendering.GraphicsApiValidation.GetValidationError(index);
        }

        protected override int GetDroppedErrorCount()
        {
            return Rendering.GraphicsApiValidation.GetValidationErrorsDroppedCount();
        }

        protected override bool GraphicsLayersMisconfigured()
        {
            return Rendering.GraphicsApiValidation.IsValidationRequested() != Rendering.GraphicsApiValidation.IsValidationActive();
        }
    }
}
#endif
