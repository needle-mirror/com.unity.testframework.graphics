using System;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class StereoRenderingPathsNodeData : IPlatformNode
    {
        public Type DataType { get; } = typeof(StereoRenderingPaths);
        public Enum Current { get; }
        public Enum Build { get; }
    }
}
