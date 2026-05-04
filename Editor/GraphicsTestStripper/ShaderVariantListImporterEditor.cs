using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    [CustomEditor(typeof(ShaderVariantList))]
    class ShaderVariantListImporterEditor : Editor
    {
        const int k_UpdateSvcControlIdOffset = 101;
        const int k_AggregateSvcControlIdOffset = 42;
        const int k_FnvPrime = 16777619;
        const uint k_FnvOffsetBasis = 2166136261;

        new ShaderVariantList target => base.target as ShaderVariantList;

        internal IAssetService AssetService { get; set; } = new AssetDatabaseService();

        int m_UpdateFromScvEventID = -1;
        int m_AggregateFromScvEventID = -1;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.EndDisabledGroup();

            using (new GUILayout.VerticalScope("HelpBox"))
            {
                EditorGUILayout.LabelField("Logs operations", EditorStyles.boldLabel);

                if (
                    GUILayout.Button(
                        new GUIContent(
                            "Update Shader Variants From Player.log",
                            "You can provide the full Player.log file in this text area to manually update the list of shader variants to strip."
                        )
                    )
                )
                {
                    var defaultPath =
                        Environment
                            .GetFolderPath(Environment.SpecialFolder.ApplicationData)
                            .Replace("Roaming", "LocalLow") + "/DefaultCompany/UnityTestFramework/";

                    var path = EditorUtility.OpenFilePanel(
                        "Select Player.log or shadervariantlist file",
                        defaultPath,
                        "log,txt,shadervariantlist"
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        var playerLog = File.ReadAllText(path);
                        UpdateVariantsFromLog(playerLog);
                    }
                }

                if (
                    GUILayout.Button(
                        new GUIContent(
                            "Aggregate Shader Variants From Player.log",
                            "You can provide the full Player.log file in this text area to manually update the list of shader variants to strip."
                        )
                    )
                )
                {
                    var defaultPath =
                        Environment
                            .GetFolderPath(Environment.SpecialFolder.ApplicationData)
                            .Replace("Roaming", "LocalLow") + "/DefaultCompany/UnityTestFramework/";

                    var path = EditorUtility.OpenFilePanel(
                        "Select Player.log or shadervariantlist file",
                        defaultPath,
                        "log,txt,shadervariantlist"
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        var playerLog = File.ReadAllText(path);
                        AggregateVariantsFromLog(playerLog);
                    }
                }
            }

            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope("HelpBox"))
            {
                EditorGUILayout.LabelField("Shader Variant Collection Operations", EditorStyles.boldLabel);

                if (
                    GUILayout.Button(
                        new GUIContent(
                            "Update Shader Variants From SVC",
                            "You can provide a full Player.log file in this text area to manually update the list of shader variants to strip."
                        )
                    )
                )
                {
                    m_UpdateFromScvEventID = GUIUtility.GetControlID(FocusType.Passive) + k_UpdateSvcControlIdOffset;
                    EditorGUIUtility.ShowObjectPicker<ShaderVariantCollection>(null, false, "", m_UpdateFromScvEventID);
                }
                if (
                    GUILayout.Button(
                        new GUIContent(
                            "Aggregate Shader Variants From SVC",
                            "You can provide a full Player.log file in this text area to manually update the list of shader variants to strip."
                        )
                    )
                )
                {
                    m_AggregateFromScvEventID = GUIUtility.GetControlID(FocusType.Passive) + k_AggregateSvcControlIdOffset;
                    EditorGUIUtility.ShowObjectPicker<ShaderVariantCollection>(
                        null,
                        false,
                        "",
                        m_AggregateFromScvEventID
                    );
                }
            }

            EditorGUILayout.Space();

            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            using (new GUILayout.VerticalScope("HelpBox"))
            {
                if (GUILayout.Button("Clear All Data"))
                    ClearAllData();
            }
            GUI.backgroundColor = oldColor;

            using (new GUILayout.VerticalScope("HelpBox"))
            {
                EditorGUILayout.LabelField("Debug / Optimization");
                if (
                    GUILayout.Button(
                        new GUIContent(
                            "Log all duplicated variants",
                            "Allow to find unneeded keywords in your shader passes. Removing them will improve object batching."
                        )
                    )
                )
                {
                    LogDuplicatedVariantKeywords();
                }
            }

            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                ShaderVariantCollection pickedCollection;

                if (EditorGUIUtility.GetObjectPickerControlID() == m_UpdateFromScvEventID)
                {
                    pickedCollection = EditorGUIUtility.GetObjectPickerObject() as ShaderVariantCollection;
                    m_UpdateFromScvEventID = -1;
                    UpdateVariantsFromSvc(pickedCollection);
                }
                if (EditorGUIUtility.GetObjectPickerControlID() == m_AggregateFromScvEventID)
                {
                    pickedCollection = EditorGUIUtility.GetObjectPickerObject() as ShaderVariantCollection;
                    m_AggregateFromScvEventID = -1;
                    AggregateVariantsFromSvc(pickedCollection);
                }
            }

            EditorGUI.BeginChangeCheck();
            target.settings.enabled = EditorGUILayout.Toggle("Enabled", target.settings.enabled);
            target.settings.targetPlatform = (ShaderCompilerPlatform)
                EditorGUILayout.EnumPopup("Target Platform", target.settings.targetPlatform);
            target.settings.xr = EditorGUILayout.Toggle("XR", target.settings.xr);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateVariantsSettingsInFile();
            }

            EditorGUI.BeginDisabledGroup(true);
            base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

        int ComputeHash(params byte[] data)
        {
            unchecked
            {
                const int p = k_FnvPrime;
                var hash = (int)k_FnvOffsetBasis;

                foreach (var t in data)
                    hash = (hash ^ t) * p;

                return hash;
            }
        }

        string KeywordListToString(List<string> keywords)
        {
            return keywords.Count == 0 ? "<no keywords>" : string.Join(" ", keywords);
        }

        void LogDuplicatedVariantKeywords()
        {
            try
            {
                Dictionary<
                    (ShaderType shaderType, string shaderName, string shaderPass, int compiledHash),
                    string
                > compiledSet = new();
                StringBuilder sb = new();

                var count = 0;
                foreach (var variant in target.serializedShaderVariants)
                {
                    if (variant.stage != ShaderType.Fragment)
                        continue;

                    var shader = Shader.Find(variant.shaderName);
                    if (shader == null)
                    {
                        Debug.Log("Shader " + variant.shaderName + " not found");
                        continue;
                    }

                    CompileAndCheckVariant(shader, variant, compiledSet, sb, count);
                    count++;
                }

                File.WriteAllText(Application.dataPath + "/../DuplicatedVariants.log", sb.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void CompileAndCheckVariant(
            Shader shader,
            ShaderVariantList.SerializedShaderVariant variant,
            Dictionary<(ShaderType, string, string, int), string> compiledSet,
            StringBuilder sb,
            int count
        )
        {
            var sd = ShaderUtil.GetShaderData(shader);
            var subShader = sd.GetSubshader(sd.ActiveSubshaderIndex);
            for (var i = 0; i < subShader.PassCount; i++)
            {
                var pass = subShader.GetPass(i);
                if (pass.Name != variant.passName || !pass.HasShaderStage(variant.stage))
                    continue;

                var compiledInfo = pass.CompileVariant(
                    variant.stage,
                    variant.keywords.Split(' '),
                    ShaderCompilerPlatform.D3D,
                    BuildTarget.StandaloneWindows
                );
                EditorUtility.DisplayProgressBar(
                    $"Compiling Variants {count}/{target.serializedShaderVariants.Count}",
                    $"{variant.shaderName} {variant.passName} {variant.stage} {variant.keywords}",
                    i / (float)subShader.PassCount
                );
                var k = (
                    variant.stage,
                    variant.shaderName,
                    variant.passName,
                    ComputeHash(compiledInfo.ShaderData)
                );
                if (compiledSet.TryGetValue(k, out var keywords))
                {
                    var duplicatedText =
                        $"Duplicated keywords for shader {variant.shaderName} {variant.passName} {variant.stage}\n"
                        + $"{keywords}\n{variant.keywords}\n";
                    Debug.Log(duplicatedText);
                    sb.AppendLine(duplicatedText);
                }
                else
                {
                    compiledSet.Add(k, variant.keywords);
                }
            }
        }

        void UpdateVariantsFromLog(string playerLogContent)
        {
            GenerateShaderVariantList.AppendAllShaderLines(out var finalFile, playerLogContent);
            WriteAllTextToSvl(AssetService.GetAssetPath(target), finalFile);
        }

        void UpdateVariantsFromSvc(ShaderVariantCollection svc)
        {
            var svcLog = TransformSvcToLog(svc);

            GenerateShaderVariantList.AppendAllShaderLines(out var finalFile, svcLog.ToString());
            WriteAllTextToSvl(AssetService.GetAssetPath(target), finalFile);
        }

        StringBuilder TransformSvcToLog(ShaderVariantCollection svc)
        {
            // Generate compiled shader lines form SVC
            var serializedSvc = new SerializedObject(svc);
            var shaders = serializedSvc.FindProperty("m_Shaders");
            var svcLog = new StringBuilder();

            for (var i = 0; i < shaders.arraySize; i++)
            {
                var shaderVariants = shaders.GetArrayElementAtIndex(i);
                var shader = (Shader)shaderVariants.FindPropertyRelative("first").objectReferenceValue;
                var shaderPassNames = GetAllPassNamesInShader(shader);

                // Shader name and button to remove it
                var variantsProp = shaderVariants.FindPropertyRelative("second.variants");
                for (var variantIndex = 0; variantIndex < variantsProp.arraySize; ++variantIndex)
                {
                    var prop = variantsProp.GetArrayElementAtIndex(variantIndex);
                    var keywords = prop.FindPropertyRelative("keywords").stringValue;
                    if (string.IsNullOrEmpty(keywords))
                        keywords = "<no keywords>";

                    // Ignore pass type as it's useless in SRPs and hardcode all stage instead because we don't have the info
                    foreach (var passName in shaderPassNames)
                        svcLog.AppendLine(
                            $"{GenerateShaderVariantList.k_CompiledShaderString}: {shader.name}, pass: {passName}, stage: all, keywords {keywords}"
                        );
                }
            }

            return svcLog;
        }

        List<string> GetAllPassNamesInShader(Shader shader)
        {
            var shaderData = ShaderUtil.GetShaderData(shader);
            var shaderPassNames = new List<string>();

            // Gather pass names for the current shader, filtered using the current render pipeline
            for (var subShaderIndex = 0; subShaderIndex < shaderData.SubshaderCount; subShaderIndex++)
            {
                var subShader = shaderData.GetSubshader(subShaderIndex);

                var renderPipeline = subShader.FindTagValue(new ShaderTagId("RenderPipeline")).name;
                if (
                    RenderPipelineManager.currentPipeline == null
                    || renderPipeline == RenderPipelineManager.currentPipeline.GetType().Name
                )
                {
                    for (var passIndex = 0; passIndex < subShader.PassCount; passIndex++)
                    {
                        var passName = subShader.GetPass(passIndex).Name;
                        if (String.IsNullOrEmpty(passName))
                            passName = "<Unnamed Pass " + passIndex + ">";
                        shaderPassNames.Add(passName);
                    }
                }
            }

            return shaderPassNames;
        }

        void AggregateVariantsFromLog(string playerLogContent)
        {
            var path = AssetService.GetAssetPath(target);
            var existingLines = new SortedSet<string>();

            if (File.Exists(path))
            {
                var lines = new List<string>(File.ReadAllLines(path));
                try
                {
                    var settingsLine = JsonUtility.FromJson<ShaderVariantList.Settings>(lines[0]);
                    if (settingsLine != null)
                        lines.RemoveAt(0);
                }
                catch
                {
                    // Don't care if it fails
                }

                // Deduplicate entries
                foreach (var line in lines)
                    existingLines.Add(line.Trim());
            }

            GenerateShaderVariantList.AppendAllShaderLines(out var finalFile, playerLogContent, existingLines);
            WriteAllTextToSvl(path, finalFile);
        }

        void WriteAllTextToSvl(string path, StringBuilder finalFile)
        {
            finalFile.Insert(0, JsonUtility.ToJson(target.settings) + "\n");
            File.WriteAllText(path, finalFile.ToString());
            AssetService.Refresh();
            AssetService.ImportAsset(path);
        }

        void AggregateVariantsFromSvc(ShaderVariantCollection svc)
        {
            var path = AssetService.GetAssetPath(target);
            var svcLog = TransformSvcToLog(svc);

            var existingLines = new SortedSet<string>();
            if (File.Exists(path))
            {
                // Deduplicate entries
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                    existingLines.Add(line.Trim());
            }

            GenerateShaderVariantList.AppendAllShaderLines(out var finalFile, svcLog.ToString(), existingLines);
            WriteAllTextToSvl(path, finalFile);
        }

        void UpdateVariantsSettingsInFile()
        {
            var path = AssetService.GetAssetPath(target);
            var fileLines = new List<string>(File.ReadAllLines(path));
            var settingsText = JsonUtility.ToJson(target.settings);

            try
            {
                JsonUtility.FromJson<ShaderVariantList.Settings>(fileLines[0]);
                fileLines[0] = settingsText;
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.DebugLog($"No existing settings line found, inserting new one: {ex.Message}");
                fileLines.Insert(0, settingsText);
            }

            File.WriteAllLines(path, fileLines);
            AssetService.Refresh();
            AssetService.ImportAsset(path);
        }

        void ClearAllData()
        {
            var path = AssetService.GetAssetPath(target);
            var settingsText = JsonUtility.ToJson(target.settings);
            File.WriteAllText(path, settingsText);
            AssetService.Refresh();
            AssetService.ImportAsset(path);
        }
    }
}
