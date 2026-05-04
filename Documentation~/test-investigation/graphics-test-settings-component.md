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
| **Per Pixel Correctness Threshold** | Makes the test fail if the perceptual difference between two pixels is greater than this value. Unity uses the DeltaE formula to measure the perceptual difference between two colors. |
| **Per Pixel Gamma Threshold** | Makes the test fail if the RGB gamma difference between two pixels is greater than this value. |
| **Per Pixel Alpha Threshold** | Makes the test fail if the alpha difference between two pixels is greater than this value. |
| **Average Correctness Threshold** | Makes the test fail if the average difference between the images is greater than this value. |
| **Incorrect Pixels Threshold** | Makes the test fail if the ratio of incorrect pixels to correct pixels is greater than this value. |
| **Active Image Tests** | Sets which method Unity uses to calculate the difference between the test image and the reference image. The options are: <ul><li>**None**: Doesn't compare the images.</li><li>**Everything**: Uses all the methods.</li><li>**Average Delta E**: Uses the perceptual difference.</li><li>**Incorrect Pixels Count**: Uses the ratio of incorrect pixels to correct pixels.</li><li>**RMSE**: Uses the root mean square error.</li></ul> |
| **Active Pixel Tests** | Sets which method Unity uses to calculate the difference between individual pixels in the test image and the reference image. The options are: <ul><li>**None**: Doesn't compare pixels.</li><li>**Everything**: Uses all the methods.</li><li>**Delta E**: Uses the perceptual difference.</li><li>**Delta Gamma**: Uses the RGB gamma difference.</li><li>**Delta Alpha**: Uses the alpha difference.</li></ul> |

## Additional resources

- [Customize tests](../build-customization/customize-a-test.md)
- [Graphics Tests window reference](the-graphics-tests-window.md)
- [Graphics Test Build Settings Inspector window reference](graphics-test-build-settings-inspector.md)
