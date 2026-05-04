using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Settings to control how image comparison is performed by <c>ImageAssert.</c>
    /// </summary>
    [Serializable]
    public class ImageComparisonSettings
    {
        /// <summary>
        /// The width to use for the rendered image. If a reference image already exists for this
        /// test and has a different size the test will fail.
        /// </summary>
        [Tooltip("The width to use for the rendered image.")]
        public int TargetWidth = 512;

        /// <summary>
        /// The height to use for the rendered image. If a reference image already exists for this
        /// test and has a different size the test will fail.
        /// </summary>
        [Tooltip("The height to use for the rendered image.")]
        public int TargetHeight = 512;

        /// <summary>
        /// The sample count needed for the test scene to be compared
        /// </summary>
        public int TargetMSAASamples = 1;

        /// <summary>
        /// The permitted perceptual difference between individual pixels of the images.
        ///
        /// The deltaE for each pixel of the image is compared and any differences below this
        /// threshold are ignored.
        /// </summary>
        [Tooltip("The permitted perceptual difference between individual pixels of the images.")]
        public float PerPixelCorrectnessThreshold;

        /// <summary>
        /// The permitted difference between the RGB components (in gamma) of individual pixels of the images.
        /// </summary>
        [Tooltip("The permitted difference between the RGB components (in gamma) of individual pixels of the images.")]
        public float PerPixelGammaThreshold = 1f / 255;

        /// <summary>
        /// The permitted difference between the alpha component of individual pixels of the images.
        /// </summary>
        [Tooltip("The permitted difference between the alpha component of individual pixels of the images.")]
        public float PerPixelAlphaThreshold = 1f / 255;

        /// <summary>
        /// The maximum permitted root mean squared error value across the entire image. If the root mean squared
        /// per-pixel error across the image is above this value, the images are considered
        /// not to be equal.
        /// </summary>
        [Tooltip("The maximum permitted root mean squared error value across the entire image.")]
        public float RMSEThreshold;

        /// <summary>
        /// The maximum permitted average error value across the entire image. If the average
        /// per-pixel difference across the image is above this value, the images are considered
        /// not to be equal.
        /// </summary>
        [Tooltip("The maximum permitted average error value across the entire image.")]
        public float AverageCorrectnessThreshold;

        /// <summary>
        /// The maximum ratio of pixels allowed to be incorrect across the image. A pixel is
        /// incorrect if it exceeds the specified per-pixel thresholds.
        /// </summary>
        [Tooltip("The maximum ratio of pixels allowed to be incorrect across the image.")]
        public float IncorrectPixelsThreshold = 1f / 512 / 512;

        /// <summary>
        /// Use HDR rendering
        /// </summary>
        [Tooltip("If enabled, render textures will be created with DefaultHDR format.")]
        public bool UseHDR;

        /// <summary>
        /// Use back buffer capture
        /// </summary>
        [Tooltip("If enabled, tests will use the back buffer, as opposed to a render texture.")]
        public bool UseBackBuffer;

        /// <summary>
        /// Determines which tests are active when comparing the images.
        /// </summary>
        [Tooltip("Determines which tests are active when comparing the images.")]
        public ImageTests ActiveImageTests = ImageTests.AverageDeltaE;

        /// <summary>
        /// The filename to use for the Actual Image. Use this to override the image filename.
        /// </summary>
        [Tooltip("Overrides the actual image filename when it is written to disk.")]
        public string ActualImageFileName { get; set; }

        /// <summary>
        /// The image comparison tests that are available. These tests are used to determine
        /// whether the images are equal or not.
        /// </summary>
        [Flags]
        public enum ImageTests
        {
            /// <summary>
            /// No image tests are active.
            /// </summary>
            None = 0,

            /// <summary>
            /// The average deltaE test is active. This test compares the average deltaE
            /// value of the images. If the average deltaE value is above the specified
            /// threshold, the images are considered not to be equal.
            /// </summary>
            AverageDeltaE = 1 << 0,

            /// <summary>
            /// The incorrect pixels count test is active. This test counts the number of
            /// incorrect pixels in the images. If the number of incorrect pixels is above
            /// the specified threshold, the images are considered not to be equal.
            /// </summary>
            IncorrectPixelsCount = 1 << 1,

            /// <summary>
            /// The root mean squared error test is active. This test compares the root
            /// mean squared error value of the images. If the root mean squared error
            /// value is above the specified threshold, the images are considered not to
            /// be equal.
            /// </summary>
            RMSE = 1 << 2,
        }

        /// <summary>
        /// Determines which tests are active when determining whether an individual pixel is
        /// correct or not. An incorrect pixel will increase the counter associated with the
        /// IncorrectPixelsCount image test. This is only relevant when ActiveImageTests has
        /// the IncorrectPixelsCount flag set.
        /// </summary>
        [Tooltip("Determines which tests affect the counter used by the IncorrectPixelsCount image test.")]
        public PixelTests ActivePixelTests = PixelTests.DeltaE | PixelTests.DeltaAlpha | PixelTests.DeltaGamma;

        /// <summary>
        /// The image comparison pixel tests that are available. These tests are used to determine
        /// whether an individual pixel is correct or not. An incorrect pixel will increase the
        /// counter associated with the IncorrectPixelsCount image test. This is only relevant
        /// when ActiveImageTests has the IncorrectPixelsCount flag set.
        /// </summary>
        [Flags]
        public enum PixelTests
        {
            /// <summary>
            /// No pixel tests are active.
            /// </summary>
            None = 0,

            /// <summary>
            /// The deltaE test is active. This test compares the deltaE value of the
            /// individual pixels of the images. If the deltaE value is above the specified
            /// threshold, the pixel is considered incorrect.
            /// </summary>
            DeltaE = 1 << 0,

            /// <summary>
            /// The delta alpha test is active. This test compares the alpha value of the
            /// individual pixels of the images. If the alpha value is above the specified
            /// threshold, the pixel is considered incorrect.
            /// </summary>
            DeltaAlpha = 1 << 1,

            /// <summary>
            /// The delta gamma test is active. This test compares the gamma value of the
            /// individual pixels of the images. If the gamma value is above the specified
            /// threshold, the pixel is considered incorrect.
            /// </summary>
            DeltaGamma = 1 << 2,
        }
    }
}
