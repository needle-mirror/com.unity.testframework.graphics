using UnityEditor.UIElements;
using UnityEngine.TestTools.Graphics;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        VisualElement m_SettingsInspector;
        ObjectField m_SettingsObjectField;
        Button m_SettingsSaveButton;

        void SetupSettingsInspector()
        {
            m_SettingsObjectField = m_Root.Q<ObjectField>("SettingsObject");
            m_SettingsInspector = m_Root.Q<VisualElement>("InspectorContainer");

            m_SettingsObjectField.value = GraphicsTestBuildSettings.LoadOrDefault();
            var inspector = new InspectorElement(m_SettingsObjectField.value);
            m_SettingsInspector.Add(inspector);

            EditorApplication.update += ReplaceSettingsIfDeleted;
        }

        void TearDownSettingsInspector()
        {
            EditorApplication.update -= ReplaceSettingsIfDeleted;
        }

        void ReplaceSettingsIfDeleted()
        {
            if (m_SettingsObjectField.value != null)
                return;

            m_SettingsObjectField.value = GraphicsTestBuildSettings.LoadOrDefault();
            var inspector = new InspectorElement(m_SettingsObjectField.value);
            m_SettingsInspector.Clear();
            m_SettingsInspector.Add(inspector);
        }
    }
}
