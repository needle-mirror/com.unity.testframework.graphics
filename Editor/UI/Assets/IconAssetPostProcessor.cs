using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    class IconAssetPostProcessor : AssetPostprocessor
    {
        void OnPostprocessTexture(Texture2D texture)
        {
            if (EditorGUIUtility.isProSkin || !assetPath.Contains("gtf-icon-"))
                return;

            var pixels = texture.GetPixels();

            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 0.01f)
                    continue;

                pixels[i] = pixels[i].Darken();
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }
    }
}
