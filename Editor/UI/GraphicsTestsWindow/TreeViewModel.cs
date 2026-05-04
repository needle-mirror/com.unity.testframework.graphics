using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    /// <summary>
    /// Pure data model for the graphics test tree view.
    /// Handles building, filtering, and sorting the tree model independent of any UI elements.
    /// </summary>
    class TreeViewModel
    {
        readonly List<TreeViewItemData<GraphicsTestCaseGroup>> m_FullTreeRootItems = new();
        readonly Dictionary<string, int> m_TestNames = new();

        string m_CurrentFilter = string.Empty;
        int m_NextId;

        /// <summary>
        /// Maps test full names to their tree view IDs for fast lookup.
        /// </summary>
        internal IReadOnlyDictionary<string, int> TestNames => m_TestNames;

        /// <summary>
        /// The full unfiltered root items of the tree model.
        /// </summary>
        internal IReadOnlyList<TreeViewItemData<GraphicsTestCaseGroup>> FullTreeRootItems => m_FullTreeRootItems;

        /// <summary>
        /// The current search filter string.
        /// </summary>
        internal string CurrentFilter
        {
            get => m_CurrentFilter;
            set => m_CurrentFilter = value ?? string.Empty;
        }

        /// <summary>
        /// Builds the full tree model from the given test cases, metrics, and results.
        /// Returns the number of test cases processed.
        /// </summary>
        internal int BuildModel(
            IEnumerable<GraphicsTestCase> testCases,
            ConcurrentDictionary<string, ReferenceImageMetrics> metrics,
            ConcurrentDictionary<string, TestStatus> testResults
        )
        {
            m_FullTreeRootItems.Clear();
            m_TestNames.Clear();
            m_NextId = 0;

            var groupDict = new Dictionary<string, List<GraphicsTestCase>>();
            foreach (var tc in testCases)
            {
                var groupKey = GetGroupKey(tc.FullName);
                if (!groupDict.TryGetValue(groupKey, out var list))
                {
                    list = new List<GraphicsTestCase>();
                    groupDict[groupKey] = list;
                }
                list.Add(tc);
            }

            var groupKeys = new List<string>(groupDict.Keys);
            groupKeys.Sort(StringComparer.Ordinal);

            foreach (var groupKey in groupKeys)
            {
                var group = groupDict[groupKey];
                var groupLabel = GetGroupLabel(groupKey);
                var children = new List<TreeViewItemData<GraphicsTestCaseGroup>>();

                foreach (var tc in group)
                {
                    var id = m_NextId++;
                    m_TestNames.TryAdd(tc.FullName, id);
                    metrics.TryGetValue(tc.FileName, out var itemMetrics);
                    var testResult = testResults.GetValueOrDefault(tc.FullName, GraphicsTestCaseGroup.k_NoStatus);

                    var testCaseData = new GraphicsTestCaseGroup(tc.FullName, tc, itemMetrics, testResult);

                    children.Add(new TreeViewItemData<GraphicsTestCaseGroup>(id, testCaseData));
                }

                var parentResult = ComputeGroupResult(children);
                var parentGroupData = new GraphicsTestCaseGroup(groupLabel, result: parentResult);
                var parentId = m_NextId++;

                m_FullTreeRootItems.Add(
                    new TreeViewItemData<GraphicsTestCaseGroup>(parentId, parentGroupData, children)
                );
            }

            return m_TestNames.Count;
        }

        /// <summary>
        /// Returns the filtered root items based on the current filter string.
        /// </summary>
        internal List<TreeViewItemData<GraphicsTestCaseGroup>> GetFilteredItems()
        {
            return GetFilteredItems(m_CurrentFilter);
        }

        /// <summary>
        /// Returns the filtered root items based on the given filter string.
        /// </summary>
        internal List<TreeViewItemData<GraphicsTestCaseGroup>> GetFilteredItems(string filter)
        {
            var filteredRootItems = new List<TreeViewItemData<GraphicsTestCaseGroup>>();
            var hasFilter = !string.IsNullOrEmpty(filter);

            foreach (var group in m_FullTreeRootItems)
            {
                if (hasFilter)
                {
                    var filteredChildren = new List<TreeViewItemData<GraphicsTestCaseGroup>>();
                    foreach (var child in group.children)
                    {
                        if (MatchesFilter(child.data, filter))
                            filteredChildren.Add(child);
                    }

                    if (filteredChildren.Count > 0)
                    {
                        filteredRootItems.Add(
                            new TreeViewItemData<GraphicsTestCaseGroup>(group.id, group.data, filteredChildren)
                        );
                    }
                }
                else
                {
                    filteredRootItems.Add(group);
                }
            }

            return filteredRootItems;
        }

        /// <summary>
        /// Sorts the full tree model by the specified column and direction.
        /// </summary>
        internal void Sort(string columnName, SortDirection direction)
        {
            var ascending = direction == SortDirection.Ascending;
            var groupComparer = GetGroupComparer(columnName, ascending);
            var childKeySelector = GetChildKeySelector(columnName);

            for (var i = 0; i < m_FullTreeRootItems.Count; i++)
            {
                var group = m_FullTreeRootItems[i];
                var hasChildren = false;
                foreach (var _ in group.children)
                {
                    hasChildren = true;
                    break;
                }
                if (childKeySelector != null && hasChildren)
                {
                    var orderedChildren = new List<TreeViewItemData<GraphicsTestCaseGroup>>(group.children);
                    orderedChildren.Sort(
                        (a, b) =>
                        {
                            var keyA = childKeySelector(a);
                            var keyB = childKeySelector(b);
                            if (keyA == null && keyB == null)
                                return 0;
                            if (keyA == null)
                                return 1;
                            if (keyB == null)
                                return -1;
                            var cmp = keyA.CompareTo(keyB);
                            return ascending ? cmp : -cmp;
                        }
                    );

                    m_FullTreeRootItems[i] = new TreeViewItemData<GraphicsTestCaseGroup>(
                        group.id,
                        group.data,
                        orderedChildren
                    );
                }
            }

            if (groupComparer != null)
                m_FullTreeRootItems.Sort(groupComparer);
        }

        // ── Private helpers ────────────────────────────────────────────

        static string GetGroupKey(string fullName)
        {
            var lastDot = fullName.LastIndexOf('.');
            return lastDot >= 0 ? fullName[..lastDot] : fullName;
        }

        static string GetGroupLabel(string groupKey)
        {
            var lastDot = groupKey.LastIndexOf('.');
            return lastDot >= 0 ? groupKey[(lastDot + 1)..] : groupKey;
        }

        static bool MatchesFilter(GraphicsTestCaseGroup data, string filter)
        {
            if (data.m_TestCase == null)
                return false;

            if (data.m_TestCase.FullName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (data.m_IgnoreReason.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (data.m_IgnoredOn != null)
            {
                foreach (var r in data.m_IgnoredOn)
                {
                    if (r.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        static TestStatus ComputeGroupResult(List<TreeViewItemData<GraphicsTestCaseGroup>> children)
        {
            foreach (var c in children)
            {
                if (c.data.m_Result == TestStatus.Failed)
                    return TestStatus.Failed;
            }
            var allPassed = true;
            foreach (var c in children)
            {
                if (c.data.m_Result != TestStatus.Passed)
                {
                    allPassed = false;
                    break;
                }
            }
            if (allPassed)
                return TestStatus.Passed;
            var allSkipped = true;
            foreach (var c in children)
            {
                if (c.data.m_Result != TestStatus.Skipped)
                {
                    allSkipped = false;
                    break;
                }
            }
            if (allSkipped)
                return TestStatus.Skipped;
            return GraphicsTestCaseGroup.k_NoStatus;
        }

        static Comparison<TreeViewItemData<GraphicsTestCaseGroup>> GetGroupComparer(string columnName, bool ascending)
        {
            Comparison<TreeViewItemData<GraphicsTestCaseGroup>> comparer = columnName switch
            {
                ColumnNames.k_TestResult => (a, b) => a.data.m_Result.CompareTo(b.data.m_Result),
                ColumnNames.k_TestName => (a, b) =>
                    string.Compare(a.data.m_Name, b.data.m_Name, StringComparison.Ordinal),
                ColumnNames.k_IgnoredOn => (a, b) =>
                    string.Compare(
                        GetFirstIgnoredOnString(a.data.m_IgnoredOn),
                        GetFirstIgnoredOnString(b.data.m_IgnoredOn),
                        StringComparison.Ordinal
                    ),
                ColumnNames.k_Images => (a, b) => SumPlatformCount(a.children).CompareTo(SumPlatformCount(b.children)),
                ColumnNames.k_Divergence => (a, b) => SumDivergence(a.children).CompareTo(SumDivergence(b.children)),
                _ => null,
            };

            if (comparer != null && !ascending)
                return (a, b) => comparer(b, a);

            return comparer;
        }

        static Func<TreeViewItemData<GraphicsTestCaseGroup>, IComparable> GetChildKeySelector(string columnName)
        {
            return columnName switch
            {
                ColumnNames.k_TestName => x => x.data.m_Name,
                ColumnNames.k_IgnoredOn => x => GetFirstIgnoredOnString(x.data.m_IgnoredOn) ?? string.Empty,
                ColumnNames.k_Images => x => x.data.m_ReferenceImageMetrics?.PlatformCount ?? 0,
                ColumnNames.k_Divergence => x => x.data.m_ReferenceImageMetrics?.AccumulatedDivergence ?? 0f,
                ColumnNames.k_TestResult => x => x.data.m_Result,
                _ => null,
            };
        }

        static string GetFirstIgnoredOnString(GraphicsTestPlatform[] ignoredOn)
        {
            if (ignoredOn == null || ignoredOn.Length == 0)
                return null;
            return ignoredOn[0].Name;
        }

        static int SumPlatformCount(IEnumerable<TreeViewItemData<GraphicsTestCaseGroup>> children)
        {
            var sum = 0;
            foreach (var child in children)
                sum += child.data.m_ReferenceImageMetrics?.PlatformCount ?? 0;
            return sum;
        }

        static double SumDivergence(IEnumerable<TreeViewItemData<GraphicsTestCaseGroup>> children)
        {
            double sum = 0;
            foreach (var child in children)
                sum += child.data.m_ReferenceImageMetrics?.AccumulatedDivergence ?? 0;
            return sum;
        }
    }
}
