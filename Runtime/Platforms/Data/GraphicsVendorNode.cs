using System;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class GraphicsVendorNode : IPlatformNode
    {
        static TestSettingsReader TestSettingsReader { get; set; } = new();
        public Type DataType { get; } = typeof(GraphicsVendor);

        public Enum Current
        {
            get
            {
                try
                {
                    return Enum.Parse<GraphicsVendor>(GraphicsDeviceInfo.Vendor, true);
                }
                catch (ArgumentException e)
                {
                    GraphicsTestLogger.DebugWarning($"{e.Message} ({GraphicsDeviceInfo.VendorID:X})");
                    return GraphicsVendor.Unknown;
                }
            }
        }
#if UNITY_EDITOR
        public Enum Build => TestSettingsReader.TryGetTestSettings()?.GraphicsVendor ?? Current;
#else
        public Enum Build => Current;
#endif
    }
}
