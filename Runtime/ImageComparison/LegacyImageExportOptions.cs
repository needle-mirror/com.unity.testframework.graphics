namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Image comparison settings that have been extracted because specific to image exports.
    /// </summary>
    public class LegacyImageExportOptions
    {
        /// <summary>
        /// Logs the comparison message when set to true. This is here for legacy reason only
        /// </summary>
        public bool LogMessages { get; set; }

        /// <summary>
        /// Saves the Actual Image even if the image comparison does not return a difference.
        /// </summary>
        public bool SaveActualImageOnSuccess { get; set; }

        /// <summary>
        /// Whether the actual, expected and diff are all saved on failure or just the actual.
        /// </summary>
        public bool SaveImagesOnFailure { get; set; }

        /// <summary>
        /// Whether the actual, expected and diff are all saved on failure or just the actual.
        /// </summary>
        public bool SaveImagesToDiskOnFailure { get; set; }

        /// <summary>
        /// Whether the file is saved as png or exr. Defaults to png. Must be lower-case.
        /// </summary>
        public string FileExtension { get; set; } = "png";

        /// <summary>
        /// The file name of the actual image to be exported.
        /// </summary>
        public string ActualImageFileName { get; set; }
    }
}
