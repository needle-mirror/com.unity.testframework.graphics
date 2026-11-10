using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    [CustomPropertyDrawer(typeof(ImageComparisonSettings))]
    class ImageComparisonSettingsDrawer : PropertyDrawer
    {
        static readonly string[] k_LegacyColorDifferenceSettingNames =
        {
            nameof(ImageComparisonSettings.PerPixelCorrectnessThreshold),
            nameof(ImageComparisonSettings.PerPixelGammaThreshold),
            nameof(ImageComparisonSettings.PerPixelAlphaThreshold),
            nameof(ImageComparisonSettings.AverageCorrectnessThreshold),
            nameof(ImageComparisonSettings.IncorrectPixelsThreshold),
            nameof(ImageComparisonSettings.ActiveImageTests),
            nameof(ImageComparisonSettings.ActivePixelTests),
        };

        // Includes the per-pixel deltaE threshold: the frame average and each tile average only the deltaE above it.
        static readonly string[] k_TiledDeltaESettingNames =
        {
            nameof(ImageComparisonSettings.PerPixelCorrectnessThreshold),
            nameof(ImageComparisonSettings.AverageCorrectnessThreshold),
            nameof(ImageComparisonSettings.TilesPerAxis),
            nameof(ImageComparisonSettings.MaxTileDeltaEThreshold),
        };

        /// <summary>
        /// The settings the drawer shows for the given algorithm, in the order it shows them. A setting that
        /// cannot affect the algorithm's comparison is not shown.
        /// </summary>
        internal static IReadOnlyList<string> ComparisonSettingNames(ComparisonAlgorithm algorithm)
        {
            return algorithm switch
            {
                ComparisonAlgorithm.LegacyColorDifference => k_LegacyColorDifferenceSettingNames,
                ComparisonAlgorithm.TiledDeltaE => k_TiledDeltaESettingNames,
                _ => Array.Empty<string>(),
            };
        }

        /// <summary>
        /// The properties the comparison section draws, in order: the selector, whatever its value, then the
        /// settings of the algorithm it selects. Nothing follows the selector when the selected objects use
        /// different algorithms, since no one algorithm's settings apply to all of them.
        /// </summary>
        internal static IEnumerable<SerializedProperty> ComparisonSettingProperties(SerializedProperty settings)
        {
            var algorithm = settings.FindPropertyRelative(nameof(ImageComparisonSettings.ComparisonAlgorithm));
            yield return algorithm;

            if (algorithm.hasMultipleDifferentValues)
            {
                yield break;
            }

            foreach (var settingName in ComparisonSettingNames((ComparisonAlgorithm)algorithm.intValue))
            {
                yield return settings.FindPropertyRelative(settingName);
            }
        }

        /// <summary>
        /// The note the comparison section shows under the selector, or null when it shows none: one when the
        /// selected objects use different algorithms, one when the stored value selects no algorithm, and one
        /// when the selected objects use the tiled algorithm with the tile threshold unset.
        /// </summary>
        internal static (string Message, MessageType Type)? ComparisonNotice(SerializedProperty settings)
        {
            var algorithm = settings.FindPropertyRelative(nameof(ImageComparisonSettings.ComparisonAlgorithm));
            if (algorithm.hasMultipleDifferentValues)
            {
                return (
                    "The selected objects use different comparison algorithms. "
                        + "Select objects that use the same one to edit its settings.",
                    MessageType.Info);
            }

            if (!Enum.IsDefined(typeof(ComparisonAlgorithm), (ComparisonAlgorithm)algorithm.intValue))
            {
                return (
                    "No comparison algorithm has this value, so ImageAssert.AreEqual rejects it. "
                        + "Pick one above.",
                    MessageType.Warning);
            }

            if ((ComparisonAlgorithm)algorithm.intValue == ComparisonAlgorithm.TiledDeltaE)
            {
                var maxTileDeltaEThreshold =
                    settings.FindPropertyRelative(nameof(ImageComparisonSettings.MaxTileDeltaEThreshold));

                // Negative means unset, the condition PixelEvaluationGuide resolves the threshold with.
                if (!maxTileDeltaEThreshold.hasMultipleDifferentValues && maxTileDeltaEThreshold.floatValue < 0f)
                {
                    return (
                        "Max Tile Delta E Threshold is unset, so every tile must stay within "
                            + "Average Correctness Threshold. "
                            + "Set it to allow a tile more than the frame average.",
                        MessageType.Info);
                }
            }

            return null;
        }

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
                    foreach (var comparisonSetting in ComparisonSettingProperties(property))
                    {
                        EditorGUILayout.PropertyField(comparisonSetting);
                    }

                    var notice = ComparisonNotice(property);
                    if (notice.HasValue)
                    {
                        EditorGUILayout.HelpBox(notice.Value.Message, notice.Value.Type);
                    }
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
