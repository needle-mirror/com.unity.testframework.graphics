# Graphics Tests window Test Results tab reference

Explore the properties and settings you can use to check the images from tests, and create reference images.

## Test case list

### Toolbar

| **Control** | **Description** |
|:---|:---|
| **Filter test cases by name** | Filters the tests in the test list. Filters both the **Name** and **Ignored On** columns of the test list. |
| **Optimize Images** | Replaces duplicate reference images with a single image, to reduce storage space. For more information, refer to [Deduplicate reference images](../performance-optimization/reference-image-optimization.md). |
| **Export Ignore Data** | Exports a .csv file of the attributes Unity used to ignore tests. This button is available when you select the dropdown on the right of the toolbar. |

### Test cases

The test list displays all the graphics tests in your project in a tree view.

**Note:** If no graphics tests exist in your project, the test list displays a **Create Graphics Tests** button. For more information, refer to [Get started](../get-started.md).

| Column | Description |
|---|---|
| **TestResult** | The result of the test. The values are:<ul><li>**◯**: Test not run yet or inconclusive result.</li><li>**✓**: Test passed.</li><li>**✗**: Test failed.</li><li>**⋯**: Test skipped or ignored.</li></ul> |
| **Name** | The name of the test. Right-click the name for more options. |
| **Ignored On** | The platforms where Unity ignores the result of this test. This field is empty if Unity doesn't ignore any platforms. |
| **Images** | The number of platform-specific reference images. This column is hidden by default. |
| **Divergence** | The total accumulated image divergence percentage across platforms. This column is hidden by default. |

To sort the list by a column, select the column header. To show or hide columns, right-click a column header.

## Image comparison panel

### Comparison tabs

| **Property** | **Description** |
|:---|:---|
| **Add a new comparison tab** (**+**) | Opens the **New Comparison** window to add a new tab to the comparison tabs panel. |

### New Comparison window

The New Comparison window adds a new tab that lets you compare two platforms or two textures. 

| **Control** | **Description** |
|:---|:---|
| **Mode** | Sets the type of comparison for the new tab. The options are:<ul><li>**Load from XML**: Creates a tab by importing test results from an NUnit XML file.</li><li>**Cross-Platform**: Creates a tab that compares images from two platforms.</li><li>**Adhoc**: Creates a tab that compares any two textures.</li></ul> |

#### Load From XML

These properties are only available if you set **Mode** to **Load from XML**.

| **Control** | **Description** |
|:---|:---|
| **Schema** | The folder hierarchy to use to read the XML file and generate a folder structure in the **Project** window. For more information about the folder structure, refer to [Graphics Test Build Settings Inspector window reference](graphics-test-build-settings-inspector.md#platform-schemata). |
| **Select XML File...** | Opens a file picker to select a test results XML file. |

#### Cross-Platform

These properties are only available if you set **Mode** to **Cross-Platform**.

| **Control** | **Description** |
|:---|:---|
| **Schema** | The folder hierarchy to use to generate folders in the **Project** window from the XML file. For more information about the folder structure, refer to [Graphics Test Build Settings Inspector window reference](graphics-test-build-settings-inspector.md#platform-schemata).  |
| **Reference Images A** | The hierarchy of folders for the first platform. |
| **Reference Images B** | The hierarchy of folders for the second platform. |
| **Compare** | Creates the comparison tab. |

#### Adhoc

These properties are only available if you set **Mode** to **Adhoc**.

| **Control** | **Description** |
|:---|:---|
| **Image A** | The first texture to compare. Drag a texture into the property, or select the picker (**⊙**). |
| **Image B** | The second texture to compare. Drag a texture into the property, or select the picker (**⊙**). |
| **Compare** | Creates the comparison tab. |

## Image comparison viewport

Move the slider left to right to reveal the reference image and the test image.

To use some of the following parameters, select the dropdown at the bottom of the image comparison panel.

| **Control** | **Description** |
|:---|:---|
| **Quick Switch** | Alternates between displaying the reference image and the test image. |
| **Prev / Next Variant** | Alternates between variants of the reference image. If no variants exist, the control is labelled **No Variants** and is disabled. |
| **Difference** | Displays an image of the difference between the two files. |
| **Heatmap** | Displays a heatmap where each pixel is a color that represents the difference between the two images. By default, a brighter color indicates a larger difference. |
| **Accept Result** | Makes the test image the reference image for future tests. For more information, refer to [Run a graphics test for the first time](../test-authoring/running-graphics-tests-first-time.md). |
| **Clear Result** | Deletes the test image. |
| **Reset View** | Resets the slider and the zoom to their defaults. |
| **Zoom slider** | Sets the zoom level. This slider appears only when you zoom in, for example using the mouse scroll wheel. Click and drag the image to scroll. | 
| **Delete Reference Image** | Deletes the reference image. |
| **Clear All Results** | Deletes the test images for all tests. |
| **Clear Test Result** | Resets the test result status for the selected test. |

## More (⋮) menu

| **Property** | **Description** |
|:---|:---|
| **Open Documentation** | Opens the documentation for the Graphics Test Framework in a browser window. |
| **Clear Test Results** | Clears all test results and deletes the test results file. |

## Additional resources

- [Write a scene test](../test-authoring/writing-graphics-tests.md)
- [Deduplicate reference images](../performance-optimization/reference-image-optimization.md)
- [Graphics Test Build Settings Inspector window reference](graphics-test-build-settings-inspector.md)
