using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        const int k_FilterDebounceMs = 300;
        const int k_ResultFontSize = 15;
        const int k_RegexTimeoutMs = 100;

        static readonly Color k_PassedColor = new(0.1333333f, 0.5450981f, 0.1333333f, 1f);
        static readonly Color k_FailedColor = new(0.8627452f, 0.1921569f, 0.1960784f, 1f);
        static readonly Color k_InconclusiveColor = new(1f, 0.6470588f, 0f, 1f);
        static readonly Color k_LinkColor = new(0.1176471f, 0.5647059f, 1f, 1f);

        ToolbarSearchField m_SearchField;
        MultiColumnTreeView m_TreeView;
        VisualElement m_EmptyStateOverlay;

        readonly Dictionary<string, Uri[]> m_Links = new();
        readonly TreeViewModel m_TreeViewModel = new();
        GraphicsTestCase m_SelectedTestCase;
        int m_TestCasesAtUpdate;
        Color m_DefaultTextColor;

        DateTime m_LastFilterChangeTime = DateTime.MinValue;
        bool m_FilterDirty;
        readonly TimeSpan m_FilterDebounceTime = TimeSpan.FromMilliseconds(k_FilterDebounceMs);

        public void Update()
        {
            if (
                m_TreeViewModel.FullTreeRootItems.Count == 0
                || GraphicsTestCaseCollector.Instance.TestCaseCount != m_TestCasesAtUpdate
            )
            {
                BuildFullTreeModel();
            }

            if (m_FilterDirty && (DateTime.Now - m_LastFilterChangeTime) > m_FilterDebounceTime)
            {
                m_FilterDirty = false;
                m_TreeViewModel.CurrentFilter = m_SearchField.value;
                ApplyFilterAndRefreshView();
            }

            m_DefaultTextColor = m_TreeView.resolvedStyle.color;
        }

        void SetupTreeView()
        {
            m_TreeView = m_Root.Q<MultiColumnTreeView>();
            m_SearchField = m_Root.Q<ToolbarSearchField>("Search");
            m_EmptyStateOverlay = m_Root.Q<VisualElement>("EmptyStateOverlay");

            m_SearchField.RegisterValueChangedCallback(_ =>
            {
                m_LastFilterChangeTime = DateTime.Now;
                m_FilterDirty = true;
            });

            m_TreeView.columnSortingChanged += OnSortingChanged;

            var createButton = m_Root.Q<Button>("CreateGraphicsTestsButton");
            createButton?.RegisterCallback<ClickEvent>(_ => GraphicsTestScaffolder.CreateDefault());

            SetupCellBindings();
        }

        void BuildFullTreeModel()
        {
            m_TestCasesAtUpdate = GraphicsTestCaseCollector.Instance.TestCaseCount;

            m_TreeViewModel.BuildModel(
                GraphicsTestCaseCollector.Instance.GetAllTestCases(),
                m_ReferenceImageMetrics,
                m_TestResults
            );

            ApplyFilterAndRefreshView();
        }

        void ApplyFilterAndRefreshView()
        {
            if (m_TreeView == null)
                return;

            var filteredItems = m_TreeViewModel.GetFilteredItems();
            m_TreeView.SetRootItems(filteredItems);
            m_TreeView.Rebuild();

            var hasNoTests = m_TestCasesAtUpdate == 0 && filteredItems.Count == 0;
            if (m_EmptyStateOverlay != null)
            {
                m_EmptyStateOverlay.style.display = hasNoTests ? DisplayStyle.Flex : DisplayStyle.None;
                m_TreeView.style.display = hasNoTests ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        void SetupCellBindings()
        {
            var testResultCol = m_TreeView.columns[ColumnNames.k_TestResult];
            var testNameCol = m_TreeView.columns[ColumnNames.k_TestName];
            var ignoredOnCol = m_TreeView.columns[ColumnNames.k_IgnoredOn];
            var imagesCol = m_TreeView.columns[ColumnNames.k_Images];
            var divergenceCol = m_TreeView.columns[ColumnNames.k_Divergence];

            testResultCol.makeCell = () => CreateLabel(WhiteSpace.Normal, Wrap.NoWrap, TextAnchor.MiddleCenter);
            testResultCol.bindCell = BindTestResultCell;

            testNameCol.makeCell = () => CreateLabel(WhiteSpace.Normal, Wrap.Wrap, TextAnchor.MiddleLeft);
            testNameCol.bindCell = BindTestNameCell;

            ignoredOnCol.makeCell = () => CreateLabel(WhiteSpace.Normal, Wrap.Wrap, TextAnchor.MiddleLeft);
            ignoredOnCol.bindCell = BindIgnoredOnCell;

            imagesCol.makeCell = () => CreateLabel(WhiteSpace.Normal, Wrap.Wrap, TextAnchor.MiddleCenter);
            imagesCol.bindCell = BindImagesCell;

            divergenceCol.makeCell = () => CreateLabel(WhiteSpace.Normal, Wrap.Wrap, TextAnchor.MiddleCenter);
            divergenceCol.bindCell = BindDivergenceCell;
        }

        void OnSortingChanged()
        {
            SortColumnDescription sortColumn = null;
            foreach (var desc in m_TreeView.sortColumnDescriptions)
            {
                sortColumn = desc;
                break;
            }
            if (sortColumn == null)
                return;

            m_TreeViewModel.Sort(sortColumn.columnName, sortColumn.direction);
            ApplyFilterAndRefreshView();
        }

        static Label CreateLabel(WhiteSpace whiteSpace, Wrap wrap, TextAnchor align)
        {
            return new Label
            {
                style =
                {
                    whiteSpace = whiteSpace,
                    flexWrap = wrap,
                    unityTextAlign = align,
                },
            };
        }

        void BindTestResultCell(VisualElement element, int index)
        {
            var label = (Label)element;
            var id = m_TreeView.GetIdForIndex(index);
            var result = m_TreeView.GetItemDataForId<GraphicsTestCaseGroup>(id).m_Result;
            label.style.fontSize = k_ResultFontSize;

            label.text = result switch
            {
                TestStatus.Passed => "✓",
                TestStatus.Failed => "✗",
                TestStatus.Skipped => "⋯",
                _ => "○",
            };

            label.style.color = result switch
            {
                TestStatus.Passed => k_PassedColor,
                TestStatus.Failed => k_FailedColor,
                TestStatus.Inconclusive => k_InconclusiveColor,
                _ => Color.grey,
            };

            label.tooltip = (int)result == -1 ? "No Status" : result.ToString();
        }

        void BindTestNameCell(VisualElement element, int index)
        {
            var label = (Label)element;
            var data = m_TreeView.GetItemDataForIndex<GraphicsTestCaseGroup>(index);

            label.text = data.m_Name;
            label.tooltip = data.m_TestCase?.FullName;

            label.AddManipulator(
                new ContextualMenuManipulator(evt =>
                {
                    evt.menu.ClearItems();
                    evt.menu.AppendAction("Copy Name", _ => GUIUtility.systemCopyBuffer = label.text);

                    evt.menu.AppendAction("Copy Full Name", _ => GUIUtility.systemCopyBuffer = label.tooltip);

                    evt.menu.AppendSeparator();

                    evt.menu.AppendAction(
                        "Ping Reference",
                        _ => EditorGUIUtility.PingObject(m_ReferenceImage),
                        m_ReferenceImage == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal
                    );

                    evt.menu.AppendAction(
                        "Ping Actual",
                        _ => EditorGUIUtility.PingObject(m_ActualImage),
                        m_ActualImage == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal
                    );

                    evt.menu.AppendSeparator();

                    evt.menu.AppendAction(
                        "Open Scene",
                        _ => OpenScene(label.tooltip),
                        _ =>
                        {
                            if (
                                m_TreeViewModel.TestNames.TryGetValue(label.tooltip, out var id)
                                && m_TreeView.GetItemDataForId<GraphicsTestCaseGroup>(id).m_TestCase
                                    is SceneGraphicsTestCase
                            )
                                return DropdownMenuAction.Status.Normal;
                            return DropdownMenuAction.Status.Disabled;
                        }
                    );
                })
            );
        }

        void BindIgnoredOnCell(VisualElement element, int index)
        {
            var label = (Label)element;
            var id = m_TreeView.GetIdForIndex(index);
            var data = m_TreeView.GetItemDataForId<GraphicsTestCaseGroup>(id);

            label.text = data.m_IgnoredOn != null ? string.Join<GraphicsTestPlatform>(", ", data.m_IgnoredOn) : "";
            label.tooltip = data.m_IgnoreReason;

            var links = Array.Empty<Uri>();
            if (!string.IsNullOrEmpty(label.tooltip))
            {
                var urlMatches = Regex.Matches(
                    label.tooltip,
                    @"https?://[^\s]+",
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(k_RegexTimeoutMs)
                );
                var linkList = new List<Uri>();
                foreach (Match m in urlMatches)
                {
                    if (Uri.TryCreate(m.Value, UriKind.Absolute, out var uri))
                        linkList.Add(uri);
                }
                links = linkList.ToArray();
            }

            var hasLinks = false;
            foreach (var _ in links)
            {
                hasLinks = true;
                break;
            }
            if (hasLinks)
            {
                m_Links[label.tooltip] = links;
                label.text += "↗";
                label.style.color = k_LinkColor;
            }
            else
            {
                label.style.color = m_DefaultTextColor;
            }

            label.AddManipulator(
                new ContextualMenuManipulator(evt =>
                {
                    evt.menu.ClearItems();
                    if (!m_Links.TryGetValue(label.tooltip, out var uris))
                    {
                        evt.menu.AppendAction("No Links Found", _ => { }, DropdownMenuAction.Status.Disabled);
                        return;
                    }

                    foreach (var link in uris)
                    {
                        var parts = link.PathAndQuery.Split('/');
                        var path = parts.Length > 0 ? parts[^1] : "";
                        evt.menu.AppendAction(
                            $"Open {link.Host} link: {path}",
                            _ =>
                            {
                                Application.OpenURL(link.ToString());
                            }
                        );
                    }
                })
            );
        }

        void BindImagesCell(VisualElement element, int index)
        {
            var label = (Label)element;
            var id = m_TreeView.GetIdForIndex(index);
            var data = m_TreeView.GetItemDataForId<GraphicsTestCaseGroup>(id);
            var metrics = data.m_ReferenceImageMetrics;

            label.text = metrics == null ? "" : $"{metrics.PlatformCount}";

            label.AddManipulator(
                new ContextualMenuManipulator(evt =>
                {
                    evt.menu.ClearItems();
                    evt.menu.AppendAction(
                        $"See All Images",
                        _ =>
                        {
                            var searchContext = UnityEditor.Search.SearchService.CreateContext(
                                $"name:{data.m_Name} t:Texture2D"
                            );
                            var viewArgs = new SearchViewState(
                                searchContext,
                                SearchViewFlags.GridView | SearchViewFlags.OpenInspectorPreview
                            );
                            UnityEditor.Search.SearchService.ShowWindow(viewArgs);
                        }
                    );
                })
            );
        }

        void BindDivergenceCell(VisualElement element, int index)
        {
            var label = (Label)element;
            var metrics = m_TreeView.GetItemDataForIndex<GraphicsTestCaseGroup>(index).m_ReferenceImageMetrics;

            const float k_PercentMultiplier = 100f;
            label.text = metrics == null ? "" : $"{metrics.AccumulatedDivergence * k_PercentMultiplier:0.000} %";
        }

        internal void SelectTest(string testName)
        {
            if (!m_TreeViewModel.TestNames.TryGetValue(testName, out var id))
            {
                GraphicsTestLogger.LogWarning("Test not found: ${testName}");
                return;
            }

            m_TabView.selectedTabIndex = 0;
            m_TreeView.SetSelectionById(id);
            m_TreeView.ScrollToItemById(id);
        }
    }
}
