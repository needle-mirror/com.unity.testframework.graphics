using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UnityEditor.TestTools.Graphics
{
    class ReferenceImageUtility
    {
        static internal string referenceLinearImagesPath => Path. Combine("Assets", "ReferenceImages", "Linear");
        static internal string referenceImagesPath => Path. Combine("Assets", "ReferenceImages");


        [MenuItem("Assets/Graphics Test Framework/Copy To Linear Reference Images", priority = 1)]
        internal static void CopyLinearImages()
        {
            CopyImages(referenceLinearImagesPath);
        }

        [MenuItem("Assets/Graphics Test Framework/Copy Images To Folder Recursively", priority = 1)]
        internal static void CopyImagesToCustomFolder()
        {
            CopyImages(EditorUtility.OpenFolderPanel("Destination",referenceImagesPath, "ReferenceImages"));
        }

        static void CopyImages(string path)
        {
            Directory.CreateDirectory(path);
            string[] leafFolders = EnumerateLeafFolders(path).ToArray();
            int numOfLeafFolders = leafFolders.Length;

            Object[] selectedObjects = Selection.objects;
            int numOfCopies = numOfLeafFolders * selectedObjects.Length;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                Texture2D selected = selectedObjects[i] as Texture2D;
                if (selected != null)
                {
                    string pathToOriginalImage = AssetDatabase.GetAssetPath(selected);
                    string extension = Path.GetExtension(pathToOriginalImage);
                    string imageName = selected.name + extension;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Copied \"" + imageName + "\" to...");

                    for (int j = 0; j < numOfLeafFolders; j++)
                    {
                        string leafFolder = leafFolders[j];
                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Copy " + imageName + " to ReferenceImages",
                                string.Format("({0} of {1}) {2}", j, numOfCopies, leafFolder),
                                (float)j / numOfLeafFolders))
                        {
                            break;
                        }
                        AssetDatabase.CopyAsset(pathToOriginalImage, Path.Combine(leafFolder, imageName));
                        sb.AppendLine("-> " + leafFolder);
                    }
                    EditorUtility.ClearProgressBar();
                    Debug.Log(sb);
                }
            }
        }

        internal static IEnumerable<string> EnumerateLeafFolders(string root)
        {
            Stack<string> dir = new Stack<string>();
            dir.Push(root);

            while (dir.Count != 0)
            {
                bool anySubfolders = false;
                root = dir.Pop();

                foreach (var subfolder in Directory.EnumerateDirectories(root))
                {
                    dir.Push(subfolder);
                    anySubfolders = true;
                }

                if (!anySubfolders)
                {
                    yield return root;
                }
            }
        }
    }
}
