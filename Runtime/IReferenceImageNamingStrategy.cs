using UnityEngine;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Pluggable strategy for building a reference image file descriptor for parameterized graphics tests.
    /// </summary>
    /// <remarks>
    /// Implementations must be stateless, have a public parameterless constructor, and must not be expensive to construct.
    /// When assigned via <see cref="GraphicsTestAttributeBase.ReferenceImageNamingStrategyType"/>, this strategy takes
    /// precedence over <see cref="ReferenceImageRootSource"/>.
    /// </remarks>
    public interface IReferenceImageNamingStrategy
    {
        /// <summary>
        /// Creates a descriptor for the given parameterized test case.
        /// </summary>
        /// <param name="rawCase">The graphics test case before parameter expansion (e.g. includes scene path for scene tests).</param>
        /// <param name="parameterizedTestName">The NUnit display name fragment used as the default reference root.</param>
        /// <param name="extension">Reference image extension from the graphics test attribute.</param>
        /// <param name="format">Texture format from the graphics test attribute.</param>
        /// <returns>A descriptor, or <c>null</c> to fall back to <see cref="ParameterizedTestNameNamingStrategy"/>-based naming.</returns>
        IReferenceImageFileDescriptor CreateDescriptor(
            GraphicsTestCase rawCase,
            string parameterizedTestName,
            ImageExtension extension,
            TextureFormat format);
    }
}
