using System.Threading.Tasks;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Abstract base class for algorithms that compare two textures using a specified threshold.
    /// </summary>
    public abstract class TextureComparisonAlgorithm
    {
        /// <summary>
        /// Gets the threshold used by the algorithm to measure the difference between images.
        /// </summary>
        protected ITextureComparisonSettings Settings { get; }

        /// <summary>
        /// Gets or sets the description of the algorithm, used when displaying assertion results.
        /// </summary>
        public string Description { get; protected init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureComparisonAlgorithm"/> class with the specified threshold.
        /// </summary>
        /// <param name="settings">The threshold that determines how differences between images are evaluated.</param>
        protected TextureComparisonAlgorithm(ITextureComparisonSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureComparisonAlgorithm"/> class.
        /// </summary>
        protected TextureComparisonAlgorithm() { }

        /// <summary>
        /// Compares two series of textures synchronously
        /// </summary>
        /// <param name="expected">The first texture to compare.</param>
        /// <param name="actual">The second texture to compare.</param>
        /// <returns>A result object representing the outcome of the comparison.</returns>
        public abstract ITextureComparisonResult Compare(Texture2D[] expected, Texture2D[] actual);

        /// <summary>
        /// Compares two textures synchronously.
        /// </summary>
        /// <param name="expected">The first texture to compare.</param>
        /// <param name="actual">The second texture to compare.</param>
        /// <returns>A result object representing the outcome of the comparison.</returns>
        public abstract ITextureComparisonResult Compare(Texture2D expected, Texture2D actual);

        /// <summary>
        /// Compares two textures asynchronously.
        /// </summary>
        /// <param name="expected">The first texture to compare.</param>
        /// <param name="actual">The second texture to compare.</param>
        /// <returns>
        /// A task that represents the asynchronous comparison operation.
        /// The task result contains a <see cref="ITextureComparisonResult"/> representing the outcome of the comparison.
        /// </returns>
        public abstract Task<ITextureComparisonResult> CompareAsync(Texture2D expected, Texture2D actual);

        /// <summary>
        /// Evaluates the result of a texture comparison.
        /// </summary>
        /// <param name="result">The result of the comparison to evaluate.</param>
        /// <returns>
        /// A tuple containing an evaluation object (typically for reporting) and a boolean indicating whether the comparison passed.
        /// </returns>
        public abstract (object, bool) Evaluate(ITextureComparisonResult result);
    }
}
