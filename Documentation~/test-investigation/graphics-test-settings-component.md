# Graphics Test Settings component reference

Explore the settings you can use to customize the graphics tests in a scene. For more information, refer to [Customize tests](../build-customization/customize-a-test.md).

| **Property** | **Description** |
|:---|:---|
| **Use HDR** | Generates images using high-definition rendering (HDR). |
| **Use back buffer** | Captures images from the back buffer instead of a render texture. |
| **Target width** | Sets the width of the captured image in pixels. |
| **Target height** | Sets the height of the captured image in pixels. |
| **Comparison settings** | Sets the sensitivity of the comparison between the test image and the reference image. |

### Comparison settings

| **Property** | **Description** |
|:---|:--|
| **Comparison Algorithm** | Sets the algorithm that compares the test image with the reference image. The options are: <ul><li>**Legacy Color Difference**: Evaluates the tests selected in **Active Image Tests**. This is the default. It measures deltaE with the original formula. For more information, refer to [DeltaE formulas](#deltae-formulas).</li><li>**Tiled Delta E**: Evaluates the average difference over the whole image and inside each tile of a **Tiles Per Axis** grid. It measures deltaE with the corrected formula. For more information, refer to [DeltaE formulas](#deltae-formulas).</li></ul>The Inspector shows only the settings the selected algorithm reads. |
| **Per Pixel Correctness Threshold** | Makes the test fail if the perceptual difference between two pixels is greater than this value. Unity uses the DeltaE formula to measure the perceptual difference between two colors. |
| **Per Pixel Gamma Threshold** | Makes the test fail if the RGB gamma difference between two pixels is greater than this value. Legacy Color Difference only. |
| **Per Pixel Alpha Threshold** | Makes the test fail if the alpha difference between two pixels is greater than this value. Legacy Color Difference only. |
| **Average Correctness Threshold** | Makes the test fail if the average difference between the images is greater than this value. |
| **Tiles Per Axis** | Sets how many tiles Unity divides the image into along each axis, so the grid holds this value squared tiles. The default is 16, which produces a 16 by 16 grid of 256 tiles. Unity clamps the value to at least 1, and to no more than the smaller image dimension so that no tile is empty. Tiled Delta E only. |
| **Max Tile Delta E Threshold** | Makes the test fail if the average difference inside any single tile is greater than this value. A negative value means the threshold is unset, in which case the test uses **Average Correctness Threshold** instead. The default is -1, which is unset. Tiled Delta E only. For more information, refer to [Choose thresholds for Tiled Delta E](#choose-thresholds-for-tiled-delta-e). |
| **Incorrect Pixels Threshold** | Makes the test fail if the ratio of incorrect pixels to correct pixels is greater than this value. Legacy Color Difference only. |
| **Active Image Tests** | Sets which method Unity uses to calculate the difference between the test image and the reference image. The options are: <ul><li>**None**: Doesn't compare the images.</li><li>**Everything**: Uses all the methods.</li><li>**Average Delta E**: Uses the perceptual difference.</li><li>**Incorrect Pixels Count**: Uses the ratio of incorrect pixels to correct pixels.</li><li>**RMSE**: Uses the root mean square error.</li></ul> Legacy Color Difference only. |
| **Active Pixel Tests** | Sets which method Unity uses to calculate the difference between individual pixels in the test image and the reference image. The options are: <ul><li>**None**: Doesn't compare pixels.</li><li>**Everything**: Uses all the methods.</li><li>**Delta E**: Uses the perceptual difference.</li><li>**Delta Gamma**: Uses the RGB gamma difference.</li><li>**Delta Alpha**: Uses the alpha difference.</li></ul> Legacy Color Difference only. |

### Choose thresholds for Tiled Delta E

**Tiled Delta E** checks two thresholds, and they measure the same quantity over different areas:

- **Average Correctness Threshold** bounds the average difference over the whole image. Only the part of each pixel's
  difference above **Per Pixel Correctness Threshold** counts.
- **Max Tile Delta E Threshold** bounds the same average inside any one tile.

The worst tile scores at least the whole-image average when **Tiles Per Axis** divides the image's width and height.
Otherwise the last row and column of tiles overlap their neighbours, and the worst tile can score slightly below it: by
less than 1% at 1920 × 1080 with 16 tiles. A difference confined to one tile scores about **Tiles Per Axis** × **Tiles
Per Axis** times higher in its tile than in the whole-image average, which is 256 times at the default of 16. A
threshold that suits the whole image is therefore too strict for a tile.

When you leave **Max Tile Delta E Threshold** unset, every tile must meet the whole-image threshold. This is strict, and
a reference image that is noisy in places can fail it. When it applies, the Inspector shows a note below the comparison
settings, and the failure message marks the tile line of the **Expected** block with `(unset, uses the average
threshold)`.

To choose the thresholds:

1. Set **Average Correctness Threshold** as you would for the Legacy Color Difference algorithm.
2. Run the test on each platform it targets.
3. Read the measured `maxTileDeltaE` from one of the following:
    - The `Max tile deltaE deviation` line of the failure message.
    - The `MaxTileDeltaE` property of the `ImageAssert.ImageComparisonResults` that `ImageAssert.AreEqual` returns
      through its `out` parameter.
    - The comparison result that Unity reports to a connected host for every comparison, whether it passes or fails. Its
      measured values include `maxTileDeltaE`.
4. Set **Max Tile Delta E Threshold** above the highest value you measured across the platforms, with some margin.

When a comparison fails because of a tile, the failure message names each tile over the threshold, worst first, with its
column, row, score and pixel range. The message lists up to 10 tiles and counts the rest. The diff image frames the same
tiles with a dashed outline, so you can see where the image differs.

### DeltaE formulas

DeltaE measures how different two colors look. It combines the difference in lightness, chroma (how far a color is
from a gray of the same lightness) and hue, in the JzAzBz color space. The original formula has two bugs:

- Its hue angle covers only half the circle, so colors whose red-green component changes sign, such as near-neutral
  grays, score too high, and opposite hues score too low.
- One coefficient of its color-space matrix differs from the one in the paper that defines JzAzBz.

The corrected formula fixes both.

**Tiled Delta E** measures with the corrected formula. **Legacy Color Difference** measures with the original formula,
which existing reference images and thresholds were made with.

## Additional resources

- [Customize tests](../build-customization/customize-a-test.md)
- [Graphics Tests window reference](the-graphics-tests-window.md)
- [Graphics Test Build Settings Inspector window reference](graphics-test-build-settings-inspector.md)
