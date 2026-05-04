using System;
using NUnit.Framework.Constraints;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Constraint that verifies whether a texture is equal to an expected texture using a specified comparison algorithm.
    /// </summary>
    public class EqualToTextureConstraint : Constraint
    {
        readonly Texture2D m_Expected;
        TextureComparisonAlgorithm m_ComparisonAlgorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualToTextureConstraint"/> class with the expected texture.
        /// </summary>
        /// <param name="expected">The texture expected in the comparison.</param>
        public EqualToTextureConstraint(Texture2D expected)
        {
            if (expected is not Texture2D)
                throw new ArgumentException("Expected value is not a Texture2D");

            m_Expected = expected;
        }

        /// <summary>
        /// Specifies the algorithm to use for texture comparison.
        /// </summary>
        /// <param name="algorithm">The <see cref="TextureComparisonAlgorithm"/> to use for comparison.</param>
        /// <returns>This constraint instance, allowing for fluent configuration.</returns>
        public EqualToTextureConstraint Using(TextureComparisonAlgorithm algorithm)
        {
            m_ComparisonAlgorithm = algorithm;
            return this;
        }

        /// <summary>
        /// Applies the constraint to the actual value and determines if it matches the expected texture.
        /// </summary>
        /// <param name="actual">The actual value to compare, which must be a <see cref="Texture2D"/>.</param>
        /// <returns>
        /// A <see cref="ConstraintResult"/> indicating whether the actual texture matches the expected texture.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the actual value is not a <see cref="Texture2D"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no comparison algorithm is specified.</exception>
        public override ConstraintResult ApplyTo(object actual)
        {
            if (actual is not Texture2D actualTexture)
                throw new ArgumentException("Actual value is not a Texture2D");

            if (m_ComparisonAlgorithm == null)
                throw new InvalidOperationException("No texture comparer was specified via Using(...)");

            var result = m_ComparisonAlgorithm.Compare(m_Expected, actualTexture);
            var (evaluation, passed) = m_ComparisonAlgorithm.Evaluate(result);
            Description = m_ComparisonAlgorithm.Description;

            return new ConstraintResult(this, evaluation, passed);
        }

        /// <summary>
        /// Gets the description of the constraint, based on the comparison algorithm used.
        /// </summary>
        public override string Description => m_ComparisonAlgorithm.Description;
    }
}
