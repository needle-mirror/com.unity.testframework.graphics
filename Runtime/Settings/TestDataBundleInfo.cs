using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Records that a content bundle produced by the player build carries test data declared with
    /// <see cref="RequireTestDataAttribute"/> rather than reference images. The runtime loader uses
    /// this to exclude the bundle from the global asset search and to resolve it by its logical
    /// name through <see cref="GraphicsTestData"/>.
    /// </summary>
    [Serializable]
    class TestDataBundleInfo
    {
        /// <summary>
        /// The bundle file name inside StreamingAssets, matching its entry in
        /// <see cref="GraphicsTestBuildSettings.TestContentBundlePaths"/>.
        /// </summary>
        public string bundleFileName;

        /// <summary>
        /// The logical name the test data was declared under (<see cref="ITestDataDescriptor.BundleName"/>).
        /// </summary>
        public string logicalName;
    }
}
