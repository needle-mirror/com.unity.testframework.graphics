using System;
using System.Globalization;
using System.IO;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Default implementation that uses a pattern of "root.extension" or "rootinteger.extension".
    /// </summary>
    public class ReferenceImageFileDescriptor : IReferenceImageFileDescriptor
    {
        /// <inheritdoc />
        public string Root { get; }

        /// <inheritdoc />
        public ImageExtension Extension { get; }

        /// <inheritdoc />
        public string VariantBase { get; }

        /// <inheritdoc />
        public TextureFormat Format { get; }

        /// <summary>
        /// Initializes a new instance with a default integer variant base of "0".
        /// </summary>
        /// <param name="root">The root of the file name stem.</param>
        /// <param name="extension">The file extension.</param>
        /// <param name="format">The texture format.</param>
        public ReferenceImageFileDescriptor(string root, ImageExtension extension, TextureFormat format)
            : this(root, "0", extension, format) { }

        /// <summary>
        /// Initializes a new instance with an explicit variant base.
        /// </summary>
        /// <param name="root">The root of the file name stem.</param>
        /// <param name="variantBase">The base for variants (for example "0").</param>
        /// <param name="extension">The file extension.</param>
        /// <param name="format">The texture format.</param>
        /// <exception cref="ArgumentException">Thrown when root or extension are null or empty.</exception>
        public ReferenceImageFileDescriptor(string root, string variantBase, ImageExtension extension, TextureFormat format)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Root cannot be null or empty.", nameof(root));

            if (!Enum.IsDefined(typeof(ImageExtension), extension))
                throw new ArgumentException("Extension must be a valid ImageExtension value.", nameof(extension));

            Root = root.ToValidPath();
            Extension = extension;
            Format = format;

            int.TryParse(variantBase, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n);
            VariantBase = n.ToString();
        }

        /// <inheritdoc />
        public string BuildDefaultName()
        {
            return Root;
        }

        /// <inheritdoc />
        public string BuildVariant(int i)
        {
            var currentVariant = int.Parse(VariantBase) + i;
            var nextVariant = Root + currentVariant;

            return nextVariant;
        }

        /// <inheritdoc />
        public bool TryParse(string filename, string expectedRoot)
        {
            if (filename is null)
                throw new ArgumentNullException(nameof(filename));

            if (string.IsNullOrWhiteSpace(expectedRoot))
            {
                throw new ArgumentException("Expected root cannot be null or empty.", nameof(expectedRoot));
            }

            var stem = Path.GetFileNameWithoutExtension(filename);

            if (string.Equals(stem, expectedRoot, StringComparison.InvariantCulture))
            {
                return true;
            }

            if (stem.StartsWith(expectedRoot, StringComparison.InvariantCulture))
            {
                var rest = stem.AsSpan(expectedRoot.Length);
                if (rest.Length == 0)
                {
                    return true;
                }

                if (int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a string that describes the naming pattern used by this descriptor.
        /// </summary>
        /// <returns>A string describing the pattern.</returns>
        public override string ToString()
        {
            return "Builds or parses a file name with the pattern '<root>.<extension>' or '<root><integer>.<extension>'.";
        }
    }
}
