using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Describes an image extension.
    /// </summary>
    [Serializable]
    public enum ImageExtension
    {
        /// <summary>
        /// Portable Network Graphics image extension
        /// </summary>
        PNG,

        /// <summary>
        /// OpenEXR image extension
        /// </summary>
        EXR
    }

    static class ImageNames
    {
        public static string ToLowerCase(this ImageExtension extension) => extension.ToString().ToLowerInvariant();
    }
}
