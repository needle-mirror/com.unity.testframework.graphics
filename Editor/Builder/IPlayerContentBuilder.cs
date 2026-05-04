using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Interface for building player content for graphics tests.
    /// </summary>
    public interface IPlayerContentBuilder
    {
        /// <summary>
        /// Builds the content for the test cases and nodes specified.
        /// </summary>
        /// <param name="testCases">The test cases to be built</param>
        /// <param name="searchPlatforms">The nodes for which to search reference images</param>
        /// <param name="buildTarget">The build target for the player</param>
        /// <returns>
        /// The names of the built content bundles.
        /// </returns>
        IEnumerable<string> BuildContent(
            IList<GraphicsTestCase> testCases,
            IEnumerable<GraphicsTestPlatform> searchPlatforms,
            BuildTarget buildTarget
        );

        /// <summary>
        /// Cleans up the content built by this builder.
        /// </summary>
        void CleanUp();
    }
}
