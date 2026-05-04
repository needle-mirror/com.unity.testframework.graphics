using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    [ScriptedImporter(1, "shadervariantlist")]
    class ShaderVariantListImporter : ScriptedImporter
    {
        internal static IAssetService AssetService { get; set; } = new AssetDatabaseService();

        [MenuItem("Assets/Create/Graphics Test Framework/Shader Variant List", false, 1)]
        public static void CreateEmptyShaderVariantList()
        {
            AssetService.Refresh();
            var action = ScriptableObject.CreateInstance<CreateShaderVariantListAsset>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
#if UNITY_6000_4_OR_NEWER
                EntityId.None,
#else
                0,
#endif
                action,
                "variants.shadervariantlist",
                null,
                null
            );
        }

        class CreateShaderVariantListAsset
#if UNITY_6000_4_OR_NEWER
            : AssetCreationEndAction
#else
            : EndNameEditAction
#endif
        {
            public override void Action(
#if UNITY_6000_4_OR_NEWER
                EntityId entityId,
#else
                int entityId,
#endif
                string pathName, string resourceFile)
            {
                File.WriteAllText(pathName, JsonUtility.ToJson(new ShaderVariantList.Settings()));
                AssetService.ImportAsset(pathName);
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Example of the log we try to parse:
            // {k_CompiledShaderString}: Custom/MyTestShader, pass: MyTestShader/Pass, stage: vertex, keywords <no keywords>
            // Compiled compute shader: ProbeVolumeBlendStates, kernel: BlendScenarios, keywords
            // Original format: "{k_CompiledShaderString}: %s, pass: %s, stage: %s, keywords %s\n", shaderName, passName, stageDesc, keywordNames
            // "Compiled compute shader: %s, kernel: %s, keywords %s\n", Name, kernelName, keywordNames);

            var shaderVariantListAsset = ScriptableObject.CreateInstance<ShaderVariantList>();

            string[] compiledShaderLines;
            try
            {
                compiledShaderLines = File.ReadAllLines(ctx.assetPath);
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.DebugLog($"Could not read shader variant list at '{ctx.assetPath}' (likely newly created): {ex.Message}");
                compiledShaderLines = Array.Empty<string>();
            }

            // We use the first line to store file settings in JSON:
            var settings = new ShaderVariantList.Settings();
            if (compiledShaderLines.Length > 0)
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(compiledShaderLines[0], settings);
                }
                catch (Exception ex)
                {
                    GraphicsTestLogger.DebugLog($"Could not parse settings from first line of shader variant list: {ex.Message}");
                }
            }

            shaderVariantListAsset.settings = settings;

            shaderVariantListAsset.serializedShaderVariants.Clear();
            foreach (var line in compiledShaderLines)
            {
                var matchCompiledShader = GenerateShaderVariantList.s_CompiledShaderRegex.Match(line);
                if (!matchCompiledShader.Success)
                {
                    matchCompiledShader = GenerateShaderVariantList.s_CompiledSnippetRegex.Match(line);
                }

                if (matchCompiledShader.Success)
                {
                    var serializedVariant = new ShaderVariantList.SerializedShaderVariant();
                    try
                    {
                        serializedVariant.shaderName = matchCompiledShader.Groups["shaderName"].Value;
                        var passName = matchCompiledShader.Groups["passName"].Value;
                        if (passName.StartsWith("<Unnamed Pass"))
                            passName = "";
                        serializedVariant.passName = passName;
                        var keywords = matchCompiledShader.Groups["keywords"].Value;
                        List<string> keywordList;
                        keywordList =
                            keywords == GenerateShaderVariantList.s_NoKeywordText
                                ? new List<string>()
                                : new List<string>(keywords.Split(' '));

                        keywordList.Sort();
                        serializedVariant.keywords = string.Join(" ", keywordList);

                        var stage = matchCompiledShader.Groups["stage"].Value.ToLowerInvariant();
                        if (stage == "all")
                        {
                            shaderVariantListAsset.serializedShaderVariants.AddRange(
                                GenerateAllStageForVariant(serializedVariant)
                            );
                        }
                        else
                        {
                            serializedVariant.stage = ParseShaderType(stage);
                            shaderVariantListAsset.serializedShaderVariants.Add(serializedVariant);
                        }
                    }
                    catch (Exception e)
                    {
                        GraphicsTestLogger.Log(LogType.Log, $"Unable to parse line {line}:");
                        Debug.LogException(e);
                    }
                }

                var matchCompiledComputeShader = GenerateShaderVariantList.s_CompiledComputeShaderRegex.Match(line);
                if (!matchCompiledComputeShader.Success)
                {
                    matchCompiledComputeShader = GenerateShaderVariantList.s_CompiledComputeKernelRegex.Match(line);
                }

                if (matchCompiledComputeShader.Success)
                {
                    var serializedComputeVariant = new ShaderVariantList.SerializedComputeShaderVariant();
                    try
                    {
                        serializedComputeVariant.computeShaderName = matchCompiledComputeShader
                            .Groups["computeName"]
                            .Value;
                        serializedComputeVariant.kernelName = matchCompiledComputeShader.Groups["kernelName"].Value;
                        var keywords = matchCompiledComputeShader.Groups["keywords"].Value;
                        List<string> keywordList;
                        keywordList =
                            keywords == GenerateShaderVariantList.s_NoKeywordText
                                ? new List<string>()
                                : new List<string>(keywords.Split(' '));

                        keywordList.Sort();
                        serializedComputeVariant.keywords = string.Join(" ", keywordList);

                        shaderVariantListAsset.serializedComputeShaderVariants.Add(serializedComputeVariant);
                    }
                    catch (Exception e)
                    {
                        GraphicsTestLogger.Log(LogType.Error, $"Unable to parse line {line}:");
                        Debug.LogException(e);
                    }
                }
            }

            ShaderType ParseShaderType(string stage)
            {
                return stage switch
                {
                    "vertex" => ShaderType.Vertex,
                    "pixel" or "fragment" => ShaderType.Fragment,
                    "geometry" => ShaderType.Geometry,
                    "hull" => ShaderType.Hull,
                    "domain" => ShaderType.Domain,
                    "raytracing" => ShaderType.RayTracing,
                    _ => throw new Exception("Unhandled shader stage: " + stage),
                };
            }

            ctx.AddObjectToAsset("Variants List", shaderVariantListAsset);
            ctx.SetMainObject(shaderVariantListAsset);
        }

        IEnumerable<ShaderVariantList.SerializedShaderVariant> GenerateAllStageForVariant(
            ShaderVariantList.SerializedShaderVariant variant
        )
        {
            foreach (ShaderType stage in Enum.GetValues(typeof(ShaderType)))
            {
                variant.stage = stage;
                yield return variant;
            }
        }
    }
}
