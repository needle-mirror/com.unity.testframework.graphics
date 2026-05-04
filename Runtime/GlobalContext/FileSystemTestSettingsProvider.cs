using System.IO;

namespace UnityEngine.TestTools.Graphics
{
    class FileSystemTestSettingsProvider : ITestSettingsProvider
    {
        readonly string m_FilePath;

        internal FileSystemTestSettingsProvider(string filePath)
        {
            m_FilePath = filePath;
        }

        public TestSettings GetTestSettings()
        {
            return !File.Exists(m_FilePath) ? null : JsonUtility.FromJson<TestSettings>(File.ReadAllText(m_FilePath));
        }
    }
}
