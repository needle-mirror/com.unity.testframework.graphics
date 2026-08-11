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
        static bool s_UnknownVendorWarned;
        public Type DataType { get; } = typeof(GraphicsVendor);

        public Enum Current => Detect(GraphicsDeviceInfo.VendorID, GraphicsDeviceInfo.Vendor);

        /// <summary>
        /// The PCI vendor id is authoritative: <see cref="GraphicsVendor"/> values ARE PCI ids, while
        /// the vendor string ("NVIDIA Corporation", "ATI Technologies Inc.") rarely matches an enum
        /// name exactly, so name parsing is only a fallback for ids not in the enum.
        /// </summary>
        internal static GraphicsVendor Detect(int vendorId, string vendorName)
        {
            if (Enum.IsDefined(typeof(GraphicsVendor), vendorId))
                return (GraphicsVendor)vendorId;

              try
              {
                  return Enum.Parse<GraphicsVendor>(vendorName, true);
              }
              catch (ArgumentException e)
              {
                  if (!s_UnknownVendorWarned)
                  {
                      GraphicsTestLogger.DebugWarning($"{e.Message} ({GraphicsDeviceInfo.VendorID:X})");
                      s_UnknownVendorWarned = true;
                  }
                  return GraphicsVendor.Unknown;
              }
        }
#if UNITY_EDITOR
        public Enum Build => TestSettingsReader.TryGetTestSettings()?.GraphicsVendor ?? Current;
#else
        public Enum Build => Current;
#endif

        /// <summary>
        /// AMD and ATI share PCI id 0x1002, so <c>ToString()</c> picks either name depending on the
        /// scripting runtime. Reference-image folders on disk use "ATI"; pin that segment so paths and
        /// bundle names stay deterministic. Non-aliased vendors keep their single name.
        /// </summary>
        public string GetPathSegment(Enum value) =>
            (GraphicsVendor)value == GraphicsVendor.ATI ? nameof(GraphicsVendor.ATI) : value.ToString();
    }
}
