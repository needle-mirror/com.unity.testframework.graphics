using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.UI
{
    class GraphicsTestCaseGroup
    {
        /// <summary>
        /// Sentinel value indicating no test result is available.
        /// </summary>
        internal const TestStatus k_NoStatus = (TestStatus)(-1);

        internal readonly string m_Name;
        internal readonly GraphicsTestCase m_TestCase;
        internal readonly GraphicsTestPlatform[] m_IgnoredOn;
        internal readonly string m_IgnoreReason;
        internal readonly ReferenceImageMetrics m_ReferenceImageMetrics;
        internal readonly TestStatus m_Result;

        public GraphicsTestCaseGroup(
            string name,
            GraphicsTestCase testCase = null,
            ReferenceImageMetrics referenceImageMetrics = null,
            TestStatus result = k_NoStatus
        )
        {
            m_Name = testCase?.Name ?? name;
            m_TestCase = testCase;
            if (testCase?.IgnoreData != null)
            {
                var platforms = new List<GraphicsTestPlatform>();
                foreach (var ignore in testCase.IgnoreData)
                {
                    if (ignore.m_Platforms != null)
                    {
                        foreach (var p in ignore.m_Platforms)
                            platforms.Add(p);
                    }
                }
                m_IgnoredOn = platforms.ToArray();
                var reasons = new List<string>();
                foreach (var ignore in testCase.IgnoreData)
                    reasons.Add(ignore.reason);
                m_IgnoreReason = string.Join("\n\n", reasons);
            }
            else
            {
                m_IgnoredOn = Array.Empty<GraphicsTestPlatform>();
                m_IgnoreReason = "";
            }
            m_ReferenceImageMetrics = referenceImageMetrics;
            m_Result = result;
        }
    }
}
