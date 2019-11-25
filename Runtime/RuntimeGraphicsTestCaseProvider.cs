using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Networking;

namespace UnityEngine.TestTools.Graphics
{
    internal class RuntimeGraphicsTestCaseProvider : IGraphicsTestCaseProvider
    {
        public IEnumerable<GraphicsTestCase> GetTestCases()
        {
            AssetBundle referenceImagesBundle = null;
            string[] scenePaths;

            // apparently unity automatically saves the asset bundle as all lower case
            var referenceImagesBundlePath = string.Format("referenceimages-{0}-{1}-{2}-{3}", 
                UseGraphicsTestCasesAttribute.ColorSpace, 
                UseGraphicsTestCasesAttribute.Platform, 
                UseGraphicsTestCasesAttribute.GraphicsDevice,
                UseGraphicsTestCasesAttribute.LoadedXRDevice).ToLower();

           referenceImagesBundlePath = Path.Combine(Application.streamingAssetsPath, referenceImagesBundlePath);

            using (var webRequest = UnityWebRequest.Get(referenceImagesBundlePath))
            {
                var handler = new DownloadHandlerAssetBundle(referenceImagesBundlePath, 0);
                webRequest.downloadHandler = handler;

                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    // wait for response
                }

                if (string.IsNullOrEmpty(webRequest.error))
                {
                    referenceImagesBundle = handler.assetBundle;
                }
                else
                {
                    Debug.Log("Error loading reference image bundle, " + webRequest.error);
                }
            }

            using (var webRequest = UnityWebRequest.Get(Application.streamingAssetsPath + "/SceneList.txt"))
            {
                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    // wait for download
                }

                if(string.IsNullOrEmpty(webRequest.error) || webRequest.downloadHandler.text != null)
                {
                    scenePaths = webRequest.downloadHandler.text.Split(
                        new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    scenePaths = new string[] { string.Empty };
                    Debug.Log("Scene list was not found.");
                }       
            }

            foreach (var scenePath in scenePaths)
            {
                var imagePath = Path.GetFileNameWithoutExtension(scenePath);

                Texture2D referenceImage = null;

                // The bundle might not exist if there are no reference images for this configuration yet
                if (referenceImagesBundle != null)
                    referenceImage = referenceImagesBundle.LoadAsset<Texture2D>(imagePath);

                yield return new GraphicsTestCase(scenePath, referenceImage);
            }
        }

        public GraphicsTestCase GetTestCaseFromPath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return null;

            GraphicsTestCase output = null;

            AssetBundle referenceImagesBundle = null;

            var referenceImagesBundlePath = string.Format("{0}/referenceimages-{1}-{2}-{3}", Application.streamingAssetsPath, UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice);
            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

            var imagePath = Path.GetFileNameWithoutExtension(scenePath);

            Texture2D referenceImage = null;

            // The bundle might not exist if there are no reference images for this configuration yet
            if (referenceImagesBundle != null)
                referenceImage = referenceImagesBundle.LoadAsset<Texture2D>(imagePath);

            output = new GraphicsTestCase( scenePath, referenceImage );

            return output;
        }
    }
}
