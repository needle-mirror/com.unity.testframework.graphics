using UnityEngine;

namespace UnityEditor.TestTools.Graphics.Filtering
{
    [CustomEditor(typeof(TestFilters))]
    class TestFiltersEditor : Editor
    {
        SerializedProperty m_Filters;
        string m_Attributes = string.Empty;

        public void OnEnable()
        {
            m_Filters = serializedObject.FindProperty("filters");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "The Test Filters Asset workflow is deprecated. Please use the IgnoreGraphicsTest attribute instead.\nYou can convert each filter in this asset to an attribute by clicking the button below.",
                MessageType.Warning
            );

            if (GUILayout.Button("Convert Filters to Attributes"))
            {
                var testFilters = (TestFilters)target;
                m_Attributes = ConvertTestFiltersToIgnoreAttribute.ConvertFiltersToIgnore(testFilters);
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(m_Attributes))
            {
                EditorGUILayout.HelpBox(
                    "Copy the following attributes and paste them into your test script(s).",
                    MessageType.Info
                );
                EditorGUILayout.TextArea(m_Attributes);
            }
        }
    }
}
