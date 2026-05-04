namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provides constraints for texture assertions in tests.
    /// </summary>
    public static class IsTexture
    {
        /// <summary>
        /// Returns a constraint that checks if a texture is equal to the expected texture.
        /// </summary>
        /// <param name="expected">The expected texture to compare against.</param>
        /// <returns>An <see cref="EqualToTextureConstraint"/> for the expected texture.</returns>
        public static EqualToTextureConstraint EqualTo(Texture2D expected)
        {
            return new EqualToTextureConstraint(expected);
        }
    }
}
