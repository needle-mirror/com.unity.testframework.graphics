namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Enumeration of XR devices.
    /// </summary>
    public enum XrDevice
    {
        /// <summary>
        /// No XR device.
        /// </summary>
        None,

        /// <summary>
        /// Mock HMD device.
        /// This is used for testing purposes only.
        /// </summary>
        MockHMDLoader,

        /// <summary>
        /// OpenXR device.
        /// </summary>
        OpenXRLoader,

        /// <summary>
        /// Oculus device.
        /// </summary>
        OculusLoader,

        /// <summary>
        /// PlayStation VR 2 device.
        /// </summary>
        PSVR2Loader,

        /// <summary>
        /// Windows Mixed Reality device.
        /// </summary>
        WindowsMRLoader,

        /// <summary>
        /// PolySpatial XR device.
        /// </summary>
        PolySpatialXRLoader,
        
        /// <summary>
        /// VisionOSLoader device.
        /// </summary>
        VisionOSLoader,
    }
}
