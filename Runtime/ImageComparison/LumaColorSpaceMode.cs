namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Specifies how luminance (luma) values should be computed with respect to
    /// gamma and linear color spaces.
    /// </summary>
    public enum LumaColorSpaceMode
    {
        /// <summary>
        /// Rejects linear space images for luma computation. An argument exception will be thrown.
        /// </summary>
        RejectLinearImages,

        /// <summary>
        /// Converts linear space images to gamma space before computing luma values.
        /// </summary>
        ConvertLinearToGamma,
    }
}
