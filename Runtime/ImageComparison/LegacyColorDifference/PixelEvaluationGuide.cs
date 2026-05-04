using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    /// <summary>
    /// The different evaluation in flags format, slightly optimized for bitwise operations that we run at scale when comparing images.
    /// </summary>
    [Flags]
    internal enum PixelEvaluationModes
    {
        None = 0,
        CountBadDeltaE = 1 << 0, // 0001
        TestAverageDeltaE = 1 << 1, // 0010
        CountBadGamma = 1 << 2, // 0100
        CountBadAlpha = 1 << 3, // 1000
    }

    /// <summary>
    /// A struct that contains the evaluation guide for pixel evaluation.
    /// </summary>
    public readonly struct PixelEvaluationGuide : ITextureComparisonSettings
    {
        /// <summary>
        /// Gets the per-pixel DeltaE Threshold
        /// </summary>
        public float DeltaEThreshold { get; init; }

        /// <summary>
        /// Gets the per-pixel GammaThreshold
        /// </summary>
        public float GammaThreshold { get; init; }

        /// <summary>
        /// Gets the per-pixel alpha threshold
        /// </summary>
        public float AlphaThreshold { get; init; }

        /// <summary>
        /// Gets the average correctness threshold, which is the average of all deltaE values exceeding the deltaE threshold
        /// </summary>
        public float AverageCorrectnessThreshold { get; init; }

        /// <summary>
        /// Gets the threshold of incorrect pixels, which is the ratio of pixels above one of the settings
        /// </summary>
        public float IncorrectPixelsThreshold { get; init; }

        /// <summary>
        /// Gets or sets the modes that are used for pixel evaluation.
        /// </summary>
        internal PixelEvaluationModes EnabledModes { get; init; }

        /// <summary>
        /// Initializes a new instance of the PixelEvaluationGuide by evaluating the different flags in the more general ImageComparisonSettings.
        /// </summary>
        /// <param name="settings">settings that need to be converted to evaluation guide.</param>
        public PixelEvaluationGuide(ImageComparisonSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            EnabledModes = PixelEvaluationModes.None;

            if (settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.AverageDeltaE))
            {
                EnabledModes |= PixelEvaluationModes.TestAverageDeltaE;
            }

            if (settings.ActiveImageTests.HasFlag(ImageComparisonSettings.ImageTests.IncorrectPixelsCount))
            {
                if (settings.ActivePixelTests.HasFlag(ImageComparisonSettings.PixelTests.DeltaE))
                {
                    EnabledModes |= PixelEvaluationModes.CountBadDeltaE;
                }

                if (settings.ActivePixelTests.HasFlag(ImageComparisonSettings.PixelTests.DeltaGamma))
                {
                    EnabledModes |= PixelEvaluationModes.CountBadGamma;
                }

                if (settings.ActivePixelTests.HasFlag(ImageComparisonSettings.PixelTests.DeltaAlpha))
                {
                    EnabledModes |= PixelEvaluationModes.CountBadAlpha;
                }
            }

            DeltaEThreshold = settings.PerPixelCorrectnessThreshold;
            GammaThreshold = settings.PerPixelGammaThreshold;
            AlphaThreshold = settings.PerPixelAlphaThreshold;
            AverageCorrectnessThreshold = settings.AverageCorrectnessThreshold;
            IncorrectPixelsThreshold = settings.IncorrectPixelsThreshold;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var description = new List<string>();

            if ((EnabledModes & (PixelEvaluationModes.CountBadDeltaE | PixelEvaluationModes.TestAverageDeltaE)) != 0)
            {
                description.Add($"Max average deltaE deviation: '{AverageCorrectnessThreshold:G}'");
                description.Add($"\tDeltaE threshold per-pixel: '{DeltaEThreshold:G}'");
            }

            if ((EnabledModes & (PixelEvaluationModes.CountBadAlpha | PixelEvaluationModes.CountBadGamma)) != 0)
            {
                description.Add($"Max bad pixels ratio: '{IncorrectPixelsThreshold}'");

                if (EnabledModes.IsSet(PixelEvaluationModes.CountBadDeltaE))
                {
                    description.Add($"\tDeltaE threshold per-pixel: '{DeltaEThreshold:G}'");
                }

                if ((EnabledModes.IsSet(PixelEvaluationModes.CountBadGamma)))
                {
                    description.Add($"\tGamma threshold per-pixel: '{GammaThreshold:G}'");
                }

                if (EnabledModes.IsSet(PixelEvaluationModes.CountBadAlpha))
                {
                    description.Add($"\tAlpha threshold per-pixel: '{AlphaThreshold:G}'");
                }
            }

            return string.Join(Environment.NewLine, description);
        }
    }
}
