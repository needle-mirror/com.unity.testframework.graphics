namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// An enum that represents the different graphics vendors.
    /// </summary>
    /// <remarks>
    /// This enum is used to represent the different graphics vendors. It is used in the <see cref="GraphicsDeviceInfo"/> class to check the vendor of the current graphics device. The values of the enum are the vendor IDs of the different graphics vendors as defined in the <see cref="SystemInfo.graphicsDeviceVendorID"/> property.
    /// </remarks>
    public enum GraphicsVendor
    {
        /// <summary>
        /// Unknown graphics vendor
        /// </summary>
        Unknown = 0xffff,

        /// <summary>
        /// Nvidia graphics vendor
        /// </summary>
        Nvidia = 0x10de,

        /// <summary>
        /// AMD graphics vendor
        /// </summary>
        AMD = 0x1002,

        /// <summary>
        /// ATI graphics vendor
        /// </summary>
        ATI = AMD,

        /// <summary>
        /// Intel graphics vendor
        /// </summary>
        Intel = 0x8086,

        /// <summary>
        /// ARM graphics vendor
        /// </summary>
        ARM = 0x13b5,

        /// <summary>
        /// Qualcomm graphics vendor
        /// </summary>
        Qualcomm = 0x5143,

        /// <summary>
        /// Apple graphics vendor
        /// </summary>
        Apple = 0x106b,

        /// <summary>
        /// Microsoft graphics vendor
        /// </summary>
        Microsoft = 0x1414,

        /// <summary>
        /// Nintendo graphics vendor
        /// </summary>
        Nintendo = 0x12e1,

        /// <summary>
        /// Sony graphics vendor
        /// </summary>
        Sony = 0x104d,

        /// <summary>
        /// Samsung graphics vendor
        /// </summary>
        Samsung = 0x1099,

        /// <summary>
        /// MediaTek graphics vendor
        /// </summary>
        MediaTek = 0x14c3,

        /// <summary>
        /// Zhaoxin graphics vendor
        /// </summary>
        Zhaoxin = 0x1d17,

        /// <summary>
        /// NEC graphics vendor
        /// </summary>
        NEC = 0x1033,

        /// <summary>
        /// VeriSilicon graphics vendor
        /// </summary>
        VeriSilicon = 0x1eb1,
    }
}
