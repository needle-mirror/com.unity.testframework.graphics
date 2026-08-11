using System;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class GraphicsApiValidationNode : IPlatformNode
    {
        public Type DataType { get; } = typeof(GraphicsApiValidationMode);

        public Enum Current
        {
            get
            {
#if UNITY_6000_6_OR_NEWER
                return global::UnityEngine.Rendering.GraphicsApiValidation.IsValidationActive()
                    ? GraphicsApiValidationMode.Enabled
                    : GraphicsApiValidationMode.None;
#else
                // Rendering.GraphicsApiValidation is a 6000.6+ engine API.
                return GraphicsApiValidationMode.None;
#endif
            }
        }
    }
}
