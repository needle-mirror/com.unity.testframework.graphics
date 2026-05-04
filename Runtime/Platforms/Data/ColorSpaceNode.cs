using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class ColorSpaceNode : IPlatformNode
    {
        public Type DataType { get; } = typeof(ColorSpace);
        public Enum Current => QualitySettings.activeColorSpace;
#if UNITY_EDITOR
        public Enum Build => PlayerSettings.colorSpace;
#else
        public Enum Build => Current;
#endif
    }
}
