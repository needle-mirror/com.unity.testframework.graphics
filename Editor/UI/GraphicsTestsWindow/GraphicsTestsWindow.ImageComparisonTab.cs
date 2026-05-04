using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        [Serializable]
        internal class ImageComparisonTab
        {
            [JsonIgnore]
            internal IAssetService AssetService { get; set; } = new AssetDatabaseService();

            [SerializeField]
            [JsonProperty("imageAPath")]
            string m_ImageAPath;

            [SerializeField]
            [JsonProperty("imageBPath")]
            string m_ImageBPath;

            [SerializeField]
            [JsonProperty("imageALabel")]
            string m_ImageALabel = "Reference Image";

            [SerializeField]
            [JsonProperty("imageBLabel")]
            string m_ImageBLabel = "Actual Image";

            [SerializeField]
            [JsonProperty("isAdhoc")]
            bool m_IsAdhoc;

            [JsonIgnore]
            Texture2D m_AdhocImageA;

            [JsonIgnore]
            Texture2D m_AdhocImageB;

            [JsonIgnore]
            public string ImageAPath => m_ImageAPath;

            [JsonIgnore]
            public string ImageBPath => m_ImageBPath;

            [JsonIgnore]
            public string ImageALabel
            {
                get => m_ImageALabel;
                set => m_ImageALabel = value;
            }

            [JsonIgnore]
            public string ImageBLabel
            {
                get => m_ImageBLabel;
                set => m_ImageBLabel = value;
            }

            [JsonIgnore]
            public bool IsAdhoc => m_IsAdhoc;

            [JsonIgnore]
            public Texture2D AdhocImageA => m_AdhocImageA ??= AssetService.LoadAssetAtPath<Texture2D>(m_ImageAPath);

            [JsonIgnore]
            public Texture2D AdhocImageB => m_AdhocImageB ??= AssetService.LoadAssetAtPath<Texture2D>(m_ImageBPath);

            public ImageComparisonTab() { }

            public ImageComparisonTab(GraphicsTestPlatform platform)
            {
                m_ImageAPath = platform.Schema.rootPath + "/" + platform.ResultsPath;
                m_ImageBPath = GraphicsTestBuildSettings.LoadOrDefault().ActualImagesPath + "/" + platform.ResultsPath;
            }

            public ImageComparisonTab(string imageAPath, string imageBPath)
            {
                m_ImageAPath = imageAPath;
                m_ImageBPath = imageBPath;
            }

            public ImageComparisonTab(Texture2D imageA, Texture2D imageB, string imageALabel, string imageBLabel)
            {
                m_IsAdhoc = true;
                m_AdhocImageA = imageA;
                m_AdhocImageB = imageB;
                m_ImageAPath = AssetService.GetAssetPath(imageA);
                m_ImageBPath = AssetService.GetAssetPath(imageB);
                m_ImageALabel = imageALabel;
                m_ImageBLabel = imageBLabel;
            }
        }
    }
}
