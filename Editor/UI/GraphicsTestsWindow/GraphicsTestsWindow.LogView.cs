using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        ListView m_LogListView;
        ToolbarSearchField m_LogSearchField;
        Button m_OpenFileButton;
        Button m_ScrollToBottomButton;
        Button m_ClearButton;
        VisualElement m_EmptyLogsOverlay;

        List<string> m_AllLogs;
        List<string> m_FilteredLogs;
        long m_LogsAdded;
        List<int> m_SelectedRows;
        int m_SelectionHash;

        const int k_HashSeed = 17;
        const int k_HashMultiplier = 31;
        const int k_SelectionPollMs = 10;
        const int k_FileExistsPollMs = 500;
        const int k_AutoScrollDelayMs = 100;

        bool m_ShouldAutoScroll = true;
        bool m_AutoScrollSubscribed;
        static readonly Color k_ErrorColor = new(0.85f, 0.20f, 0.20f);
        static readonly Color k_WarningColor = new(0.85f, 0.55f, 0f);
        static readonly Color k_InfoColor = new(0.725f, 0.725f, 0.725f);

        static readonly Color k_ErrorColorDark = new(0.75f, 0.10f, 0.10f);
        static readonly Color k_WarningColorDark = new(0.7f, 0.35f, 0f);
        static readonly Color k_InfoColorDark = new(0.275f, 0.275f, 0.2275f);

        void SetupLogView()
        {
            m_LogSearchField = m_Root.Q<ToolbarSearchField>("LogSearchField");
            m_LogListView = m_Root.Q<ListView>("LogListView");
            m_OpenFileButton = m_Root.Q<Button>("OpenFileButton");
            m_ScrollToBottomButton = m_Root.Q<Button>("ScrollToLatestButton");
            m_ClearButton = m_Root.Q<Button>("ClearLogsButton");
            m_EmptyLogsOverlay = m_Root.Q<VisualElement>("EmptyLogsOverlay");

            m_AllLogs = new List<string>(GraphicsTestLogger.GetLogBuffer());
            m_FilteredLogs = new List<string>(m_AllLogs);
            m_LogsAdded = GraphicsTestLogger.GetTotalLogsAdded();

            m_LogListView.itemsSource = m_FilteredLogs;
            m_LogListView.makeItem = () =>
            {
                var label = new Label
                {
                    enableRichText = true,
                    style =
                    {
                        whiteSpace = WhiteSpace.PreWrap,
                        flexGrow = 1,
                        textOverflow = TextOverflow.Clip,
                        unityTextAlign = TextAnchor.UpperLeft,
                        minHeight = StyleKeyword.Auto,
                        height = StyleKeyword.Auto,
                        maxHeight = StyleKeyword.None,
                        paddingTop = 5,
                        paddingBottom = 5,
                    },
                };
                label.AddManipulator(
                    new ContextualMenuManipulator(
                        (e) =>
                        {
                            e.menu.AppendAction(
                                "Copy Selected",
                                (_) =>
                                {
                                    var lines = new List<string>();
                                    foreach (var k in m_SelectedRows)
                                        lines.Add(m_FilteredLogs[k]);
                                    GUIUtility.systemCopyBuffer = string.Join("\n", lines);
                                }
                            );
                        }
                    )
                );
                return label;
            };
            m_LogListView.bindItem = (element, i) =>
            {
                var label = (Label)element;
                label.text = m_FilteredLogs[i].Replace("\r\n", "\n").Replace("\r", "\n");
                SetColor(element, i);
            };

            m_LogListView
                .schedule.Execute(() =>
                {
                    var newHash = k_HashSeed;
                    foreach (var id in m_LogListView.selectedIds)
                        newHash = newHash * k_HashMultiplier + id;

                    if (newHash == m_SelectionHash)
                        return;
                    m_SelectionHash = newHash;

                    var newSelected = new List<int>();
                    foreach (var id in m_LogListView.selectedIds)
                        newSelected.Add(id);

                    var prevRows = m_SelectedRows ?? new List<int>();
                    foreach (var p in prevRows)
                    {
                        SetColor(m_LogListView.GetRootElementForId(p), p);
                    }

                    m_SelectedRows = newSelected;

                    foreach (var i in m_SelectedRows)
                    {
                        var index = m_LogListView.GetRootElementForId(i);
                        if (index != null)
                            index.style.color = Color.white;
                    }
                })
                .Every(k_SelectionPollMs);

            m_LogSearchField.RegisterValueChangedCallback(_ => ApplyFilter());
            m_ClearButton.clicked += ClearLogs;
            m_ScrollToBottomButton.clicked += () =>
            {
                if (m_FilteredLogs.Count > 0)
                    m_LogListView.ScrollToItem(m_FilteredLogs.Count - 1);
            };
            m_OpenFileButton.clicked += () => EditorUtility.OpenWithDefaultApp(GraphicsTestLogger.MostRecentLogPath);

            m_OpenFileButton
                .schedule.Execute(() =>
                {
                    m_OpenFileButton.SetEnabled(File.Exists(GraphicsTestLogger.MostRecentLogPath));
                })
                .Every(k_FileExistsPollMs);

            SetupAutoScrollLogic();
            RefreshListView();
        }

        void SetColor(VisualElement element, int i)
        {
            if (i > m_FilteredLogs.Count - 1 || element == null)
                return;

            var line = m_FilteredLogs[i];
            var parts = line.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var firstLine = parts.Length > 0 ? parts[0] : "";

            if (firstLine.IndexOf("[error]", StringComparison.OrdinalIgnoreCase) >= 0)
                element.style.color = EditorGUIUtility.isProSkin ? k_ErrorColor : k_ErrorColorDark;
            else if (firstLine.IndexOf("[warn]", StringComparison.OrdinalIgnoreCase) >= 0)
                element.style.color = EditorGUIUtility.isProSkin ? k_WarningColor : k_WarningColorDark;
            else
                element.style.color = EditorGUIUtility.isProSkin ? k_InfoColor : k_InfoColorDark;
        }

        void SetupAutoScrollLogic()
        {
            m_LogListView
                .schedule.Execute(() =>
                {
                    if (m_AutoScrollSubscribed)
                        return;

                    var scrollView = m_LogListView.Q<ScrollView>();
                    if (scrollView == null)
                        return;

                    m_AutoScrollSubscribed = true;
                    scrollView.verticalScroller.valueChanged += value =>
                    {
                        var isAtBottom = value >= scrollView.verticalScroller.highValue;
                        m_ShouldAutoScroll = isAtBottom;
                        m_ScrollToBottomButton.SetEnabled(!isAtBottom);
                    };
                })
                .StartingIn(k_AutoScrollDelayMs);
        }

        void ApplyFilter()
        {
            var searchText = m_LogSearchField.value;

            if (string.IsNullOrEmpty(searchText))
            {
                m_FilteredLogs = new List<string>(m_AllLogs);
            }
            else
            {
                m_FilteredLogs = new List<string>();
                foreach (var log in m_AllLogs)
                {
                    if (log.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        m_FilteredLogs.Add(log);
                }
            }

            RefreshListView();
        }

        void RefreshListView()
        {
            m_LogListView.itemsSource = m_FilteredLogs;
            m_LogListView.RefreshItems();

            m_ClearButton.SetEnabled(m_AllLogs.Count > 0);

            if (m_EmptyLogsOverlay != null)
            {
                var hasLogs = m_FilteredLogs.Count > 0;
                m_EmptyLogsOverlay.style.display = hasLogs ? DisplayStyle.None : DisplayStyle.Flex;
                m_LogListView.style.display = hasLogs ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (m_ShouldAutoScroll && m_FilteredLogs.Count > 0)
            {
                m_LogListView.ScrollToItem(m_FilteredLogs.Count - 1);
            }
        }

        void RefreshLogIfChanged()
        {
            if (m_LogsAdded == GraphicsTestLogger.GetTotalLogsAdded())
                return;

            m_AllLogs = new List<string>(GraphicsTestLogger.GetLogBuffer());
            m_LogsAdded = GraphicsTestLogger.GetTotalLogsAdded();

            ApplyFilter();
        }

        void OnInspectorUpdate()
        {
            RefreshLogIfChanged();
        }

        void ClearLogs()
        {
            GraphicsTestLogger.ClearLogBuffer();
            m_AllLogs.Clear();
            m_LogSearchField.SetValueWithoutNotify("");

            ApplyFilter();
        }
    }
}
