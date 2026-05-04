# Shader Variant List Import Settings Inspector window reference

Explore the properties and settings in the **Inspector** window of a Shader Variant List asset. Use a shader variant list to control which shader variants Unity keeps or strips when you build graphics tests.

For more information, refer to [Strip shader variants from test builds](../performance-optimization/graphics-test-shader-stripping.md).

## Logs operations

| **Property** | **Description** |
|:---|:---|
| **Update Shader Variants From Player.log** | Replaces the current shader variant list with the shader variants from a `Player.log` file. For more information about `Player.log` files, refer to [Unity log files](https://docs.unity3d.com/Manual/LogFiles.html). |
| **Aggregate Shader Variants From Player.log** | Adds the shader variants from a `Player.log` file to the current shader variants. |

## Shader Variant Collection Operations

| **Property** | **Description** |
|:---|:---|
| **Update Shader Variants From SVC** | Replaces the current shader variant list with the shader variants from a shader variant collection asset. For more information, refer to [Shader Variant Collection](https://docs.unity3d.com/Manual/class-ShaderVariantCollection.html). |
| **Aggregate Shader Variants From SVC** | Adds the shader variants from a shader variant collection asset to the current shader variants. |

## Other settings

| **Property** | **Description** |
|:---|:---|
| **Clear All Data** | Removes all shader variant entries from the list. |
| **Log all duplicated variants** | Logs shader variants that have different keyword combinations but compile into identical code. Use this property to find unneeded keywords in your shader passes. Unity writes the results to a `DuplicatedVariants.log` file in the root folder of your project. |
| **Enabled** | Keeps the shader variants in the list during tests and removes other shader variants. |
| **Target Platform** | Sets the platform the shader variant list targets. Unity matches this value against the graphics API of the current build, to determine which shader variant list to use. |
| **XR** | Selects shader variants that support XR rendering. |

## Serialized Shader Variants

Displays the list of shader variants that Unity keeps during shader stripping. 

| **Property** | **Description** |
|:---|:---|
| **Shader Name** | The name of the shader. |
| **Pass Name** | The name of the shader pass. |
| **Stage** | The shader stage type, for example **Vertex**, **Fragment**, or **Geometry**. |
| **Keywords** | The shader keywords of the shader variant. |

## Serialized Compute Shader Variants

Displays the list of compute shader variants that Unity keeps during shader stripping.

| **Property** | **Description** |
|:---|:---|
| **Compute Shader Name** | The name of the compute shader. |
| **Kernel Name** | The name of the compute shader kernel. |
| **Keywords** | The shader keywords of the shader variant. |

## Additional resources

- [Strip shader variants from test builds](../performance-optimization/graphics-test-shader-stripping.md)
- [Graphics Test Build Settings Inspector window reference](../test-investigation/graphics-test-build-settings-inspector.md)
- [Customizing and optimizing tests](../build-customization/build-customization-landing.md)
