using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// A list of shader variants.
    /// This is used to store the shader variants that are used in the graphics tests.
    /// </summary>
    public class ShaderVariantList : ScriptableObject
    {
        /// <summary>
        /// A list of shader variants for a specific shader.
        /// </summary>
        public class VariantList : Dictionary<(ShaderType shaderType, string passName), HashSet<string>> { }

        /// <summary>
        /// A list of shader variants for a specific compute shader.
        /// </summary>
        public class ComputeVariantList : Dictionary<string, HashSet<string>> { }

        /// <summary>
        /// A dictionary of shader variants for each shader.
        /// </summary>
        public readonly Dictionary<string, VariantList> variantListPerShader = new Dictionary<string, VariantList>();

        /// <summary>
        /// A dictionary of shader variants for each compute shader.
        /// </summary>
        public readonly Dictionary<string, ComputeVariantList> variantListPerComputeShader =
            new Dictionary<string, ComputeVariantList>();

        /// <summary>
        /// A serialized shader variant.
        /// </summary>
        [System.Serializable]
        public struct SerializedShaderVariant
        {
            /// <summary>
            /// The name of the shader.
            /// </summary>
            public string shaderName;

            /// <summary>
            /// The name of the pass.
            /// </summary>
            public string passName;

            /// <summary>
            /// The type of the shader.
            /// </summary>
            public ShaderType stage;

            /// <summary>
            /// The sorted keywords used in the shader variant.
            /// </summary>
            public string keywords;
        }

        /// <summary>
        /// A serialized compute shader variant.
        /// </summary>
        [System.Serializable]
        public struct SerializedComputeShaderVariant
        {
            /// <summary>
            /// The name of the compute shader.
            /// </summary>
            public string computeShaderName;

            /// <summary>
            /// The name of the kernel.
            /// </summary>
            public string kernelName;

            /// <summary>
            /// The sorted keywords used in the compute shader variant.
            /// </summary>
            public string keywords;
        }

        /// <summary>
        /// Settings for the shader variant list.
        /// </summary>
        [System.Serializable]
        public class Settings
        {
            /// <summary>
            /// The target platform for the shader variants.
            /// </summary>
            public ShaderCompilerPlatform targetPlatform;

            /// <summary>
            /// Whether the shader variants are for XR.
            /// </summary>
            public bool xr;

            /// <summary>
            /// Whether the shader variants are enabled.
            /// </summary>
            public bool enabled = true;
        }

        /// <summary>
        /// The settings for the shader variant list.
        /// </summary>
        [HideInInspector]
        public Settings settings = new Settings();

        /// <summary>
        /// A list of serialized shader variants.
        /// </summary>
        public List<SerializedShaderVariant> serializedShaderVariants = new List<SerializedShaderVariant>();

        /// <summary>
        /// A list of serialized compute shader variants.
        /// </summary>
        public List<SerializedComputeShaderVariant> serializedComputeShaderVariants =
            new List<SerializedComputeShaderVariant>();

        void BuildFastAccessStructures()
        {
            variantListPerShader.Clear();
            variantListPerComputeShader.Clear();

            // For Vulkan and Switch (when using HLSLcc), we combine all shader stages, so the stripper needs to have a list of keywords for all the stages combined,
            // otherwise some variants are going to be stripped as their keyword config don't exist across all stages.
            var apis = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);
            var fusedStageBuild = false;
            foreach (var t in apis)
            {
                if (t == GraphicsDeviceType.Vulkan || t == (GraphicsDeviceType)22 || t == (GraphicsDeviceType)29)
                {
                    fusedStageBuild = true;
                    break;
                }
            }
            var allStages = new List<ShaderType>
            {
                ShaderType.Vertex,
                ShaderType.Fragment,
                ShaderType.Geometry,
                ShaderType.Hull,
                ShaderType.Domain,
                ShaderType.RayTracing,
            };

            foreach (var variant in serializedShaderVariants)
            {
                if (!variantListPerShader.TryGetValue(variant.shaderName, out var variantList))
                    variantList = variantListPerShader[variant.shaderName] = new VariantList();
                var key = (variant.stage, variant.passName);
                if (!variantList.TryGetValue(key, out var keywordSetList))
                    keywordSetList = variantList[key] = new HashSet<string>();
                keywordSetList.Add(variant.keywords);

                // Generate a key for all the other stages
                if (fusedStageBuild)
                {
                    foreach (var stage in allStages)
                    {
                        var stageKey = (stage, variant.passName);
                        variantList[stageKey] = keywordSetList;
                    }
                }
            }

            foreach (var variant in serializedComputeShaderVariants)
            {
                if (!variantListPerComputeShader.TryGetValue(variant.computeShaderName, out var variantList))
                    variantList = variantListPerComputeShader[variant.computeShaderName] = new ComputeVariantList();
                if (!variantList.TryGetValue(variant.kernelName, out var keywordSetList))
                    keywordSetList = variantList[variant.kernelName] = new HashSet<string>();
                keywordSetList.Add(variant.keywords);
            }
        }

        void OnEnable()
        {
            BuildFastAccessStructures();
        }

        /// <summary>
        /// Check if the shader variant list matches the settings.
        /// This is used to check if the shader variant list is compatible with the current build settings.
        /// </summary>
        /// <param name="targetPlatform">The target platform for the shader variants.</param>
        /// <param name="xr">Whether the shader variants are for XR.</param>
        /// <returns>
        /// True if the shader variant list matches the settings, false otherwise.
        /// </returns>
        public bool MatchSettings(ShaderCompilerPlatform targetPlatform, bool xr) =>
            settings.xr == xr && settings.targetPlatform == targetPlatform;
    }
}
