using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    public class CopyImageReferenceTest
    {
        [Test]
        public void CopyLinearImages()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath("Packages/com.unity.testframework.graphics/Tests/TestImages/16pxDummyReference.png", typeof(Texture2D));
            ReferenceImageUtility.CopyLinearImages();
            string[] leafFolders = ReferenceImageUtility.EnumerateLeafFolders(ReferenceImageUtility.referenceLinearImagesPath).ToArray();
            foreach (var folder in leafFolders)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath(folder + "/16pxDummyReference.png", typeof(Texture2D)), null);
            }
        }
    }
}
