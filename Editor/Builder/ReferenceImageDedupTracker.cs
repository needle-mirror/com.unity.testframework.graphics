using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Decides which reference images each platform contributes to the player content bundles.
    /// Two rules apply, in order:
    /// <list type="number">
    /// <item><description>
    /// A test case whose reference image was already resolved by a platform of an earlier schema is
    /// skipped entirely — schemata are resolved in sequence and later schemata only fill the gaps.
    /// Platforms of the SAME schema (e.g. per-vendor variants of one build) are siblings and each get
    /// to resolve their own image for the same test case. The exception is the universal-fallback
    /// schema — the last schema configured for the build — whose platforms always collect.
    /// </description></item>
    /// <item><description>
    /// An asset path is only ever assigned to one bundle (Unity fails the bundle build otherwise).
    /// When two sibling platforms resolve a test case to the same file — both fell back to a shared,
    /// less-specific folder — only the first occurrence is bundled. Images are grouped into bundles
    /// by the folder they resolved from, so the shared file lands in that folder's bundle either way
    /// and serves every variant the folder is valid for.
    /// </description></item>
    /// </list>
    /// </summary>
    sealed class ReferenceImageDedupTracker
    {
        readonly HashSet<string> m_ResolvedInEarlierSchemata = new();
        readonly HashSet<string> m_ResolvedInCurrentSchema = new();
        readonly Dictionary<string, string> m_ImageNamesByAssignedPath = new();
        string m_CurrentSchemaIdentity;
        bool m_HasCurrentSchema;
        bool m_CurrentSchemaIsUniversalFallback;

        /// <summary>
        /// Marks the start of a platform's collection pass. Platforms sharing a schema form one group;
        /// moving to a new group promotes the previous group's resolutions to "earlier schema". Schemata
        /// are identified by name AND resolution (root path + node sequence), since two same-named
        /// schemata may legally resolve to different reference images and then are distinct schemata.
        /// Callers must pass platforms grouped by schema; the resolver emits them that way (schemata
        /// are resolved in the given order, so each schema's platforms stay adjacent).
        /// </summary>
        /// <param name="platform">The platform about to collect reference images.</param>
        /// <param name="isUniversalFallback">
        /// True for platforms of the universal-fallback schema, whose collection is never suppressed.
        /// The caller decides which schema plays that role (see <see cref="AssetBundleBuilder"/>).
        /// </param>
        internal void BeginPlatform(GraphicsTestPlatform platform, bool isUniversalFallback)
        {
            var schema = platform?.Schema;
            var identity = schema == null
                ? string.Empty
                : $"{schema.name}\n{schema.rootPath}\n{string.Join(",", schema.nodes ?? Array.Empty<string>())}";
            if (m_HasCurrentSchema && identity == m_CurrentSchemaIdentity)
                return;

            m_ResolvedInEarlierSchemata.UnionWith(m_ResolvedInCurrentSchema);
            m_ResolvedInCurrentSchema.Clear();
            m_CurrentSchemaIdentity = identity;
            m_HasCurrentSchema = true;
            m_CurrentSchemaIsUniversalFallback = isUniversalFallback;
        }

        /// <summary>
        /// Whether the current platform should search for this test case's reference image at all.
        /// False when an earlier schema already resolved it.
        /// </summary>
        internal bool ShouldCollect(GraphicsTestCase testCase)
        {
            if (m_CurrentSchemaIsUniversalFallback)
                return true;

            var descriptor = testCase.ReferenceImageDescriptor;
            return !m_ResolvedInEarlierSchemata.Contains(descriptor.BuildDefaultName())
                && !m_ResolvedInEarlierSchemata.Contains(descriptor.BuildVariant(0));
        }

        /// <summary>
        /// Records the images the current platform resolved and returns only those whose asset path has
        /// not been assigned to a bundle yet.
        /// </summary>
        internal Dictionary<string, string> FilterNewImages(Dictionary<string, string> images)
        {
            var newImages = new Dictionary<string, string>();
            foreach (var pair in images)
            {
                m_ResolvedInCurrentSchema.Add(pair.Key);

                if (m_ImageNamesByAssignedPath.TryGetValue(pair.Value, out var assignedTo))
                {
                    if (assignedTo != pair.Key)
                        GraphicsTestLogger.Log(
                            LogType.Warning,
                            $"Reference image '{pair.Value}' resolves for both '{assignedTo}' and "
                                + $"'{pair.Key}' but is already assigned to a bundle; skipping the duplicate."
                        );
                    continue;
                }

                m_ImageNamesByAssignedPath.Add(pair.Value, pair.Key);
                newImages.Add(pair.Key, pair.Value);
            }

            return newImages;
        }
    }
}
