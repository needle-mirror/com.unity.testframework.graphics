using System;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class RenderingThreadingModeNode : IPlatformNode
    {
        public Type DataType { get; } = typeof(RenderingThreadingMode);
        public Enum Current => SystemInfo.renderingThreadingMode;
        public Enum Build => Current;
    }
}
