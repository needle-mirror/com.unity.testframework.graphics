using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    class GraphicsTestShaderStripper : IPreprocessBuildWithReport
    {
        internal static IAssetService AssetService
        {
            get => s_AssetService;
            set
            {
                s_AssetService = value;
                s_VariantListsInitialized = false;
            }
        }

        static IAssetService s_AssetService = new AssetDatabaseService();
        static bool s_VariantListsInitialized;

        static readonly List<ShaderVariantList> k_AllVariantListAssets = new();

        [NonSerialized]
        static ShaderVariantList s_CurrentVariantListInUse;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_CurrentVariantListInUse = null;
        }

        static void EnsureVariantListsInitialized()
        {
            if (s_VariantListsInitialized)
                return;
            s_VariantListsInitialized = true;

            k_AllVariantListAssets.Clear();
            var shaderVariantListGUIDs = AssetService.FindAssets("t:ShaderVariantList", new[] { "Assets", "Packages" });
            foreach (var guid in shaderVariantListGUIDs)
            {
                var path = AssetService.GuidToAssetPath(guid);
                var svl = AssetService.LoadAssetAtPath<ShaderVariantList>(path);
                if (svl != null && svl.settings.enabled)
                    k_AllVariantListAssets.Add(svl);
            }
        }

        static ShaderVariantList GetCurrentShaderVariantList()
        {
            EnsureVariantListsInitialized();

            if (!GraphicsTestBuildSettings.LoadOrDefault().EnableShaderStripping)
            {
                GraphicsTestLogger.DebugLog("Shader stripping disabled in GraphicsTestBuildSettings");
                return null;
            }

            if (s_CurrentVariantListInUse == null)
            {
                var currentAPI = GetCurrentGraphicsAPI();
                var matchingVariant = k_AllVariantListAssets.Find(s =>
                    s.MatchSettings(currentAPI, RuntimeSettings.reuseTestsForXR)
                );

                if (matchingVariant == null)
                {
                    matchingVariant = k_AllVariantListAssets.Find(s =>
                        s.MatchSettings(ShaderCompilerPlatform.D3D, false)
                    );
                    GraphicsTestLogger.Log(
                        LogType.Log,
                        $"Couldn't find the Shader Variant List for the Graphics API {currentAPI}{(RuntimeSettings.reuseTestsForXR ? " in XR" : "")}. Falling back on the D3D platform file"
                    );
                }

                if (matchingVariant == null)
                {
                    GraphicsTestLogger.Log(
                        LogType.Log,
                        "Couldn't find any Shader Variant List for this config, disabling Graphics Test Stripper"
                    );
                }
                s_CurrentVariantListInUse = matchingVariant;
            }

            return s_CurrentVariantListInUse;
        }

        static ShaderCompilerPlatform GetCurrentGraphicsAPI()
        {
            GraphicsDeviceType currentAPI;
            if (BuildPipeline.isBuildingPlayer)
            {
                // During a build, use the first configured graphics API for the active target.
                // Fall back to the editor's current device if the list is unexpectedly empty.
                var apis = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);
                currentAPI = apis.Length > 0 ? apis[0] : SystemInfo.graphicsDeviceType;
            }
            else
            {
                currentAPI = SystemInfo.graphicsDeviceType;
            }

            return GraphicsDeviceTypeToShaderCompilerPlatform(currentAPI);
        }

        static ShaderCompilerPlatform GraphicsDeviceTypeToShaderCompilerPlatform(GraphicsDeviceType type)
        {
            return type switch
            {
                GraphicsDeviceType.Direct3D11 => ShaderCompilerPlatform.D3D,
                GraphicsDeviceType.OpenGLES3 => ShaderCompilerPlatform.GLES3x,
                (GraphicsDeviceType)13 => (ShaderCompilerPlatform)11,
                GraphicsDeviceType.XboxOne => ShaderCompilerPlatform.XboxOneD3D11,
                GraphicsDeviceType.Metal => ShaderCompilerPlatform.Metal,
                GraphicsDeviceType.OpenGLCore => ShaderCompilerPlatform.OpenGLCore,
                GraphicsDeviceType.Direct3D12 => ShaderCompilerPlatform.D3D,
                GraphicsDeviceType.Vulkan => ShaderCompilerPlatform.Vulkan,
                (GraphicsDeviceType)22 => (ShaderCompilerPlatform)19,
                GraphicsDeviceType.XboxOneD3D12 => ShaderCompilerPlatform.XboxOneD3D12,
                (GraphicsDeviceType)29 => (ShaderCompilerPlatform)27,
                GraphicsDeviceType.GameCoreXboxOne => ShaderCompilerPlatform.GameCoreXboxOne,
                GraphicsDeviceType.GameCoreXboxSeries => ShaderCompilerPlatform.GameCoreXboxSeries,
                (GraphicsDeviceType)26 => (ShaderCompilerPlatform)23,
                (GraphicsDeviceType)27 => (ShaderCompilerPlatform)24,
                (GraphicsDeviceType)28 => (ShaderCompilerPlatform)26,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        public class ShaderPreProcessor : IPreprocessShaders
        {
            public int callbackOrder => Int32.MinValue;

            public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
            {
                var currentVariantList = GetCurrentShaderVariantList();
                if (currentVariantList == null)
                    return;

                // When the shader is not in the list we can just remove the whole shader
                if (!currentVariantList.variantListPerShader.TryGetValue(shader.name, out var variantList))
                {
                    data.Clear();
                    return;
                }

                // If the pass and stage doesn't exist in the list we can also remove the whole shader snippet
                if (!variantList.TryGetValue((snippet.shaderType, snippet.passName), out var keywordSetList))
                {
                    data.Clear();
                    return;
                }

                for (var i = data.Count - 1; i >= 0; i--)
                {
                    if (!keywordSetList.Contains(data[i].shaderKeywordSet.ToString()))
                    {
                        data.RemoveAt(i);
                    }
                }
            }
        }

        class GraphicsTestComputeShaderStripper : IPreprocessComputeShaders
        {
            public int callbackOrder => Int32.MinValue;

            public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
            {
                var currentVariantList = GetCurrentShaderVariantList();

                // In case the compute shader variant list is 0, either the project doesn't have compute (so this code is not called)
                // Or there is compute but the shader variant list was generated from SVC, in this case we skip the compute stripping
                // To add back the compute variants you just need to aggregate the log result of a run.
                if (currentVariantList == null || currentVariantList.variantListPerComputeShader.Count == 0)
                    return;

                // When the shader is not in the list we can just remove the whole shader
                if (!currentVariantList.variantListPerComputeShader.TryGetValue(shader.name, out var variantList))
                {
                    data.Clear();
                    return;
                }

                // If the pass and stage doesn't exist in the list we can also remove the whole shader snippet
                if (!variantList.TryGetValue(kernelName, out var keywordSetList))
                {
                    data.Clear();
                    return;
                }

                for (var i = data.Count - 1; i >= 0; i--)
                {
                    if (!keywordSetList.Contains(data[i].shaderKeywordSet.ToString()))
                    {
                        data.RemoveAt(i);
                    }
                }
            }
        }
    }
}
