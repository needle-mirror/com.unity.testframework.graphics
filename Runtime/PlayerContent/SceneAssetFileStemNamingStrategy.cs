using System.IO;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Naming strategy that uses the scene asset file stem as the reference image root.
    /// Falls back to parameterized test name for non-scene test cases.
    /// </summary>
    internal class SceneAssetFileStemNamingStrategy : IReferenceImageNamingStrategy
    {
        public static readonly SceneAssetFileStemNamingStrategy Instance = new();

        private SceneAssetFileStemNamingStrategy() { }

        public IReferenceImageFileDescriptor CreateDescriptor(
            GraphicsTestCase rawCase,
            string parameterizedTestName,
            ImageExtension extension,
            TextureFormat format)
        {
            if (rawCase is SceneGraphicsTestCase sceneCase && !string.IsNullOrEmpty(sceneCase.ScenePath))
            {
                var root = Path.GetFileNameWithoutExtension(sceneCase.ScenePath).ToValidPath();
                return new ReferenceImageFileDescriptor(root, extension, format);
            }

            GraphicsTestLogger.LogWarning(
                $"{nameof(ReferenceImageRootSource.SceneAssetFileStem)} requires a {nameof(SceneGraphicsTestCase)} with a scene path; using parameterized test name for reference image root.");

            return new ReferenceImageFileDescriptor(parameterizedTestName, extension, format);
        }
    }
}
