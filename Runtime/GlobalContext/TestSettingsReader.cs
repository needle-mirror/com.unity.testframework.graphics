using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace UnityEngine.TestTools.Graphics
{
    class TestSettingsReader
    {
        const string k_TestSettingsArg = "-testSettingsFile";
        readonly ITestSettingsProvider m_TestSettingsProvider;
        TestSettings m_TestSettingsCache;

        internal TestSettingsReader(ITestSettingsProvider provider)
        {
            m_TestSettingsProvider = provider;
        }

        internal TestSettingsReader()
        {
            var reader = new CommandLineReader();
            var fileName = reader.FindCommandLineArgument(k_TestSettingsArg);
            m_TestSettingsProvider = new FileSystemTestSettingsProvider(fileName);
        }

        internal TestSettingsReader(string path)
        {
            m_TestSettingsProvider = new FileSystemTestSettingsProvider(path);
        }

        internal TestSettings TryGetTestSettings()
        {
            return m_TestSettingsCache ??= m_TestSettingsProvider.GetTestSettings();
        }

        /// <summary>
        /// Gets a setting value from the test settings file by name, without requiring it to be defined in TestSettings.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the setting to.</typeparam>
        /// <param name="settingName">The name of the setting in the JSON file.</param>
        /// <param name="setting">A placeholder for the setting.</param>
        /// <returns>true if the setting was found and is set, false if not found.</returns>
        internal static bool TryGetTestSetting<T>(string settingName, out T setting)
        {
            var reader = new CommandLineReader();
            var fileName = reader.FindCommandLineArgument(k_TestSettingsArg);
            setting = default;

            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return false;

            try
            {
                var json = File.ReadAllText(fileName);
                var jObject = JObject.Parse(json);

                if (jObject.TryGetValue(settingName, out var token))
                {
                    setting = token.ToObject<T>();
                    return true;
                }
            }
            catch (Exception e)
            {
                GraphicsTestLogger.DebugWarning(e.Message);
                GraphicsTestLogger.DebugWarning("Returning default value for setting: " + settingName);
            }

            return false;
        }
    }
}
