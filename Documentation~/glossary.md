# Glossary

This glossary provides definitions for key terms the Graphics Test Framework package uses.

- **DeltaE**: A metric for perceptual color difference. DeltaE is calculated in a perceptually uniform color space and reflects how humans perceive differences between colors. The [`LegacyColorDifferenceAlgorithm`](xref:UnityEngine.TestTools.Graphics.LegacyColorDifference.LegacyColorDifferenceAlgorithm) uses DeltaE as its comparison metric. Lower values indicate less perceptible difference.

- **Euclidean distance**: A comparison algorithm that calculates the per-pixel color distance between two textures using the Pythagorean theorem.

- **Luma pipeline**: A luminance calculation used by the peak signal-to-noise ratio (PSNR) and structural similarity index (SSIM) algorithms when `LumaCalculations` is enabled. `LumaColorSpaceMode` controls how color space conversions are applied during luminance computation.

- **Peak signal-to-noise ratio (PSNR)**: A comparison algorithm that measures the ratio of signal power to noise power between images, expressed in decibels.

- **Reference image**: A stored image that serves as the expected output for graphics tests. Used as a benchmark to compare against the actual image generated during test execution.

- **Structural similarity index (SSIM)**: A comparison algorithm that evaluates structural similarity between images by considering luminance, contrast, and structure.

## Additional resources

- [Customize tests](build-customization/customize-a-test.md)
- [Graphics test workflow](test-authoring/graphics-tests-introduction.md)
- [Graphics Tests window reference](test-investigation/the-graphics-tests-window.md)
