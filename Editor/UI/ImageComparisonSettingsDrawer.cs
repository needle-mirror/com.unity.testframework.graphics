using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    [CustomPropertyDrawer(typeof(ImageComparisonSettings))]
    class ImageComparisonSettingsDrawer : PropertyDrawer
    {
        bool m_ImageComparisonSettingFoldoutStatus = true;
        bool m_ComparisonSettingsFoldoutStatus;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            m_ImageComparisonSettingFoldoutStatus = EditorGUILayout.Foldout(
                m_ImageComparisonSettingFoldoutStatus,
                label
            );
            if (m_ImageComparisonSettingFoldoutStatus)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(property.FindPropertyRelative("UseHDR"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("UseBackBuffer"));

                if (property.FindPropertyRelative("UseBackBuffer").boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "When using the backbuffer\n• In Editor: Game View resolution is used.\n• In Standalone: Screen resolution is used.\n• Reference image resolution needs to match.",
                        MessageType.Info
                    );
                }
                else
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("TargetWidth"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("TargetHeight"));
                }

                m_ComparisonSettingsFoldoutStatus = EditorGUILayout.Foldout(
                    m_ComparisonSettingsFoldoutStatus,
                    "Comparison Settings"
                );
                if (m_ComparisonSettingsFoldoutStatus)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("PerPixelCorrectnessThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("PerPixelGammaThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("PerPixelAlphaThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("AverageCorrectnessThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("IncorrectPixelsThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("ActiveImageTests"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("ActivePixelTests"));
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Returns 0 to prevent the default property height from adding empty space above the drawer.
        /// All layout is handled by EditorGUILayout calls inside OnGUI, so the base height is unused.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
    }
}
