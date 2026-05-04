namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    /// Contains difference data for an individual pixel of an image.
    /// </summary>
    struct LegacyColorDifferencePixelResult
    {
        public int Index { get; set; }
        public float DeltaE { get; set; }
        public float DeltaEOverThreshold { get; set; }
        public float DeltaGamma { get; set; }
        public bool PixelIsCorrect { get; set; }
        public float DeltaAlpha { get; set; }
        public Color32 ColorDifference { get; set; }
    }
}
