using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// How the run renders, as its graphics device reports it.
    /// </summary>
    readonly struct RunConfigurationSnapshot
    {
        // The Microsoft Basic Render Driver, which is how WARP reports itself. The engine's Direct3D 12
        // backend recognizes WARP by the same vendor and device.
        const int k_WarpDeviceId = 0x8c;

        internal RunConfigurationSnapshot(
            GraphicsDeviceType graphicsApi,
            RenderingThreadingMode threadingMode,
            string deviceName,
            int vendorId,
            int deviceId)
        {
            GraphicsApi = graphicsApi;
            ThreadingMode = threadingMode;
            DeviceName = deviceName;
            VendorId = vendorId;
            DeviceId = deviceId;
        }

        internal static RunConfigurationSnapshot Current => new(
            SystemInfo.graphicsDeviceType,
            SystemInfo.renderingThreadingMode,
            SystemInfo.graphicsDeviceName,
            SystemInfo.graphicsDeviceVendorID,
            SystemInfo.graphicsDeviceID);

        internal GraphicsDeviceType GraphicsApi { get; }

        internal RenderingThreadingMode ThreadingMode { get; }

        internal string DeviceName { get; }

        internal int VendorId { get; }

        internal int DeviceId { get; }

        /// <summary>
        /// Whether the device is WARP, the Direct3D software rasterizer.
        /// </summary>
        internal bool IsWarp => VendorId == (int)GraphicsVendor.Microsoft && DeviceId == k_WarpDeviceId;

        /// <summary>
        /// The device, with the ids that tell WARP from a GPU.
        /// </summary>
        internal string Device => $"{DeviceName} (vendor 0x{VendorId:X4}, device 0x{DeviceId:X4})";

        public override string ToString() => $"{GraphicsApi}, {ThreadingMode}, on {Device}";
    }
}
