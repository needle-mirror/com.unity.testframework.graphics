using System.IO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        const string k_ExportDialogTitle = "Export Ignore Data";
        const string k_DefaultFileName = "ignore_data.csv";
        const string k_FileExtension = "csv";

        void SetupIgnoreUtils()
        {
            var toolbarMenu = m_Root.Q<ToolbarMenu>("ToolbarMenu");
            toolbarMenu.menu.AppendAction(k_ExportDialogTitle, (_) => ExportIgnoreData());
        }

        static void ExportIgnoreData()
        {
            var savePath = EditorUtility.SaveFilePanel(
                k_ExportDialogTitle,
                Application.dataPath,
                k_DefaultFileName,
                k_FileExtension
            );
            if (string.IsNullOrEmpty(savePath))
                return;

            var disabledTestsCsv = IgnoreDataExporter.GenerateIgnoreDataCsv(
                GraphicsTestCaseCollector.Instance.GetAllTestCases()
            );
            File.WriteAllText(savePath, disabledTestsCsv);
        }
    }
}
