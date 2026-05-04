namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Describes and builds reference image file names and validates them against an expected pattern.
    /// </summary>
    public interface IReferenceImageFileDescriptor
    {
        /// <summary>
        /// Gets the root part of the file name (stem without variant or extension).
        /// </summary>
        string Root { get; }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        ImageExtension Extension { get; }

        /// <summary>
        /// Gets the configured base for the variant (for example "0" for integers).
        /// </summary>
        string VariantBase { get; }

        /// <summary>
        /// Gets the texture format associated with the reference image.
        /// </summary>
        TextureFormat Format { get; }

        /// <summary>
        /// Builds the default name stem without extension.
        /// </summary>
        /// <returns>The default stem.</returns>
        string BuildDefaultName();

        /// <summary>
        /// Build the variant, using the variant at the specified index, starting from the provided base.
        /// </summary>
        /// <param name="i">Index of the variant, starting from the base</param>
        /// <returns>A variant name</returns>
        string BuildVariant(int i);

        /// <summary>
        /// Tries to validate and parse a filename against the expected root and extension.
        /// </summary>
        /// <param name="filename">The filename to validate.</param>
        /// <param name="expectedRoot">The expected root to match.</param>
        /// <returns>True if the filename matches the pattern and expected root; otherwise, false.</returns>
        bool TryParse(string filename, string expectedRoot);
    }
}
