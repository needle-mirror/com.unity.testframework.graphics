using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        const string k_GraphicsTestsWindowTab = "GraphicsTestsWindowTab";
        const string k_GraphicsTestTreeExpanded = "GraphicsTestTree_Expanded";
        const string k_GraphicsTestTreeSelected = "GraphicsTestTree_Selected";
        const string k_GraphicsTestTreeFilter = "GraphicsTestTree_Filter";
        const string k_GraphicsTestTreeMetrics = "GraphicsTestTree_Metrics";
        const string k_GraphicsTestTreeTestResults = "GraphicsTestTree_TestResults";
        const string k_ComparisonTabs = "GraphicsTests_ComparisonTabs";

        const string k_EditorPrefsCorruptMessage = "EditorPrefs data was corrupt; Graphics Test Window was reinitialized with empty metrics.";

        /// <summary>
        /// Removes all persisted window state from EditorPrefs.
        /// Useful for tests that need a clean window without leaked session data.
        /// </summary>
        internal static void ClearPersistedState()
        {
            EditorPrefs.DeleteKey(k_GraphicsTestsWindowTab);
            EditorPrefs.DeleteKey(k_GraphicsTestTreeExpanded);
            EditorPrefs.DeleteKey(k_GraphicsTestTreeSelected);
            EditorPrefs.DeleteKey(k_GraphicsTestTreeFilter);
            EditorPrefs.DeleteKey(k_GraphicsTestTreeMetrics);
            EditorPrefs.DeleteKey(k_GraphicsTestTreeTestResults);
            EditorPrefs.DeleteKey(k_ComparisonTabs);
        }

        void SaveWindowState()
        {
            if (m_TreeView == null)
                return;

            // Store current tab
            var tab = m_TabView.selectedTabIndex;
            EditorPrefs.SetInt(k_GraphicsTestsWindowTab, tab);

            var expanded = new List<int>();
            foreach (var id in m_TreeView.viewController.GetAllItemIds())
            {
                if (m_TreeView.viewController.IsExpanded(id))
                    expanded.Add(id);
            }
            EditorPrefs.SetString(
                k_GraphicsTestTreeExpanded,
                JsonUtility.ToJson(new IntListWrapper { list = expanded })
            );

            // Store selection
            var selected = m_TreeView.selectedIds;
            EditorPrefs.SetString(
                k_GraphicsTestTreeSelected,
                JsonUtility.ToJson(new IntListWrapper { list = new List<int>(selected) })
            );

            // Store filter
            EditorPrefs.SetString(k_GraphicsTestTreeFilter, m_TreeViewModel.CurrentFilter);

            // Store metrics
            var serializedDictionary = JsonConvert.SerializeObject(
                ReferenceImageMetrics.ToSerializedDictionary(m_ReferenceImageMetrics)
            );
            EditorPrefs.SetString(k_GraphicsTestTreeMetrics, serializedDictionary);

            // Store comparison tabs
            var serializedComparisonTabs = JsonConvert.SerializeObject(m_ImageComparisonTabs);
            EditorPrefs.SetString(k_ComparisonTabs, serializedComparisonTabs);
        }

        void RestoreTreeState()
        {
            if (m_TreeView == null)
                return;

            var selectedTab = EditorPrefs.GetInt(k_GraphicsTestsWindowTab, 0);
            var expandedJson = EditorPrefs.GetString(k_GraphicsTestTreeExpanded, "");
            var selectedJson = EditorPrefs.GetString(k_GraphicsTestTreeSelected, "");
            m_SearchField.value = EditorPrefs.GetString(k_GraphicsTestTreeFilter, "");
            m_TabView.selectedTabIndex = selectedTab;

            if (!string.IsNullOrEmpty(expandedJson))
            {
                var expanded = JsonUtility.FromJson<IntListWrapper>(expandedJson)?.list;
                m_TreeView
                    .schedule.Execute(() =>
                    {
                        expanded?.ForEach(i => m_TreeView.viewController.ExpandItemByIndex(i, true));
                    })
                    .ExecuteLater(10);
            }

            if (string.IsNullOrEmpty(selectedJson))
                return;
            var selected = JsonUtility.FromJson<IntListWrapper>(selectedJson)?.list;
            m_TreeView
                .schedule.Execute(() =>
                {
                    if (selected is not { Count: > 0 })
                        return;

                    m_TreeView.SetSelectionById(selected);
                    m_TreeView.ScrollToItemById(selected[0]);
                    m_TreeViewModel.CurrentFilter = m_SearchField.value;
                    ApplyFilterAndRefreshView();
                })
                .Until(() =>
                {
                    if (selected is null || selected is { Count: 0 })
                        return true;
                    foreach (var id in m_TreeView.selectedIds)
                    {
                        if (id == selected[0])
                            return true;
                    }
                    return false;
                });

            // Restore comparison tabs
            Dictionary<string, ImageComparisonTab> comparisonTabs;
            try
            {
                comparisonTabs = JsonConvert.DeserializeObject<Dictionary<string, ImageComparisonTab>>(
                    EditorPrefs.GetString(k_ComparisonTabs)
                ) ?? new Dictionary<string, ImageComparisonTab>();
            }
            catch (JsonException)
            {
                GraphicsTestLogger.LogError(k_EditorPrefsCorruptMessage);
                comparisonTabs = new Dictionary<string, ImageComparisonTab>();
            }
            m_ComparisonTabView.schedule.Execute(() =>
            {
                foreach (var t in comparisonTabs)
                {
                    if (m_ImageComparisonTabs.ContainsKey(t.Key))
                        continue;

                    var tab = new Tab(t.Key)
                    {
                        closeable = true,
                        name = string.IsNullOrWhiteSpace(t.Key) ? "Base" : t.Key,
                    };
                    m_ComparisonTabView.Add(tab);
                    m_ImageComparisonTabs.Add(t.Key, t.Value);
                    tab.closed += tab1 =>
                    {
                        m_ImageComparisonTabs.Remove(tab1.name);
                    };
                }
            });
        }

        void LoadReferenceImageMetrics()
        {
            var storedMetrics = EditorPrefs.GetString(k_GraphicsTestTreeMetrics, "");
            try
            {
                var deserializedDictionary = ReferenceImageMetrics.FromSerializedDictionary(
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(storedMetrics)
                );

                if (deserializedDictionary != null)
                {
                    foreach (var kvp in deserializedDictionary)
                    {
                        m_ReferenceImageMetrics.TryAdd(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (JsonException)
            {
                // Corrupt EditorPrefs data; start with empty metrics
                GraphicsTestLogger.LogError("EditorPrefs data was corrupt; Graphics Test Window was reinitialized with empty metrics.");
            }
        }

        static string TestResultsFilePath =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "GraphicsTestResults.json");

        void LoadTestResults()
        {
            var json = File.Exists(TestResultsFilePath) ? File.ReadAllText(TestResultsFilePath) : "";
            try
            {
                var results = JsonConvert.DeserializeObject<Dictionary<string, TestStatus>>(json);

                if (results != null)
                {
                    foreach (var kvp in results)
                    {
                        m_TestResults[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (JsonException)
            {
                GraphicsTestLogger.LogError("EditorPrefs data was corrupt; Graphics Test Window was reinitialized with empty metrics.");
                // Corrupt test results file; start with empty results
            }
        }

        void SaveTestResults()
        {
            var json = JsonConvert.SerializeObject(m_TestResults, Formatting.Indented);
            File.WriteAllText(TestResultsFilePath, json);
        }

        void ClearTestResults()
        {
            m_TestResults.Clear();

            if (File.Exists(TestResultsFilePath))
                File.Delete(TestResultsFilePath);

            BuildFullTreeModel();
        }

        [Serializable]
        class IntListWrapper
        {
            [FormerlySerializedAs("m_List")]
            [SerializeField]
            internal List<int> list = new();
        }
    }
}
