using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.Graphics.Platforms;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class RuntimePlatformNode : IPlatformNode
    {
        public Type DataType { get; } = typeof(RuntimePlatform);
        public Enum Current => Application.platform;
#if UNITY_EDITOR
        public Enum Build => EditorUserBuildSettings.activeBuildTarget.ToRuntimePlatform();
#else
        public Enum Build => Current;
#endif
    }
}
