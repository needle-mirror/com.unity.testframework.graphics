using System;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class GraphicsDeviceNode : IPlatformNode
    {
        public Type DataType { get; } = typeof(GraphicsDeviceType);
        public Enum Current => GraphicsDeviceInfo.Type;
#if UNITY_EDITOR
        public Enum Build
        {
            get
            {
                var apis = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);
                return apis.Length > 0 ? apis[0] : Current;
            }
        }
#else
        public Enum Build => Current;
#endif
    }
}
