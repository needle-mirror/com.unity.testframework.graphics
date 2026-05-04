using System;

namespace UnityEngine.TestTools.Graphics
{
    static class BasicTexturePropertiesValidation
    {
        public static void ValidateTexturesBasicProperties(Texture2D[] expected, Texture2D[] actual)
        {
            if (expected is null or { Length: 0 })
            {
                throw new ArgumentNullException(nameof(expected), "The expected texture array is null or empty.");
            }

            if (actual == null)
            {
                throw new ArgumentNullException(nameof(actual), "The actual texture array is null.");
            }

            if (expected.Length != actual.Length)
            {
                throw new ArgumentException(
                    "The expected texture array length does not match the actual texture array length.",
                    nameof(expected)
                );
            }

            string argumentExceptionMessage = null;

            for (var i = 0; i < expected.Length; i++)
            {
                var currentExpected = expected[i];
                var currentActual = actual[i];

                if (currentExpected == null)
                {
                    throw new ArgumentNullException(nameof(expected), $"The expected texture at index {i} is null.");
                }

                if (currentActual == null)
                {
                    throw new ArgumentNullException(nameof(expected), $"The actual texture at index {i} is null.");
                }

                if (currentActual.width != currentExpected.width)
                {
                    argumentExceptionMessage =
                        $"The expected image had a width of {currentExpected.width}px, but the actual image had width of {currentActual.width}px.";
                }
                else if (currentActual.height != currentExpected.height)
                {
                    argumentExceptionMessage =
                        $"The expected image had a height of {currentExpected.height}px, but the actual image had a height of {currentActual.height}px.";
                }
                else if (currentActual.format != currentExpected.format)
                {
                    argumentExceptionMessage =
                        $"The expected image had format {currentExpected.format}, but the actual image had format {currentActual.format}.";
                }
            }

            if (argumentExceptionMessage != null)
            {
                throw new ArgumentException(argumentExceptionMessage);
            }
        }
    }
}
