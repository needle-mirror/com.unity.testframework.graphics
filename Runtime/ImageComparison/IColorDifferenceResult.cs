namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A comparison result that <see cref="ImageAssert"/> can report as
    /// <see cref="ImageAssert.ImageComparisonResults"/> and export as a difference image.
    /// </summary>
    interface IColorDifferenceResult : ITextureComparisonResult
    {
        ImageAssert.ImageComparisonResults ImageComparisonResults { get; }

        Color32[] DifferencePixels { get; }
    }
}
