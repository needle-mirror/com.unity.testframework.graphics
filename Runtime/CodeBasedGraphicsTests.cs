using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Interfaces;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.TestTools.TestRunner.Api;
using  UnityEditor.TestTools.Graphics;
#endif

namespace UnityEngine.TestTools.Graphics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public class CodeBasedGraphicsTestAttribute : Attribute
    {
        public string ReferenceImagesRoot { get; private set; }
        public string ActualImagesRoot { get; private set; }

        public CodeBasedGraphicsTestAttribute(string referenceImagesRoot, string actualImagesRoot = null)
        {
            ReferenceImagesRoot = referenceImagesRoot;
            ActualImagesRoot = string.IsNullOrEmpty(actualImagesRoot) ? Path.Combine(Path.GetDirectoryName(referenceImagesRoot), "ActualImages") : actualImagesRoot;
        }

        public static bool TryFindAttributeOn(ITest test, out CodeBasedGraphicsTestAttribute attrib)
            => TryFindAttributeOn(test.Method, test.TypeInfo, out attrib);

#if UNITY_EDITOR
        public static bool TryFindAttributeOn(ITestAdaptor test, out CodeBasedGraphicsTestAttribute attrib)
            => TryFindAttributeOn(test.Method, test.TypeInfo, out attrib);
#endif

        private static bool TryFindAttributeOn(IMethodInfo method, ITypeInfo type, out CodeBasedGraphicsTestAttribute attrib)
        {
            var attribs = method.GetCustomAttributes<CodeBasedGraphicsTestAttribute>(true);
            if (attribs.Length == 0)
                attribs = type.GetCustomAttributes<CodeBasedGraphicsTestAttribute>(true);
            if (attribs.Length != 0)
            {
                attrib = attribs[0];
                return true;
            }
            else
            {
                attrib = null;
                return false;
            }
        }
    }

#if UNITY_EDITOR
    public static class CodeBasedGraphicsTests
    {
        public static void AsyncEnumerate(Action<IEnumerable<GraphicsTestCase>> onEnumerate)
        {
            if (onEnumerate == null)
                throw new ArgumentNullException(nameof(onEnumerate));

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RetrieveTestList(TestMode.EditMode, (testRoot) =>
            {
                var newGraphicsTests = new List<GraphicsTestCase>();
                TraverseTests(testRoot, newGraphicsTests);
                if (newGraphicsTests.Count > 0)
                    onEnumerate(newGraphicsTests);
            });
            
            api.RetrieveTestList(TestMode.PlayMode, (testRoot) =>
            {
                var newGraphicsTests = new List<GraphicsTestCase>();
                TraverseTests(testRoot, newGraphicsTests);
                if (newGraphicsTests.Count > 0)
                    onEnumerate(newGraphicsTests);
            });
        }

        public static Texture2D LoadReferenceImage(string testName, CodeBasedGraphicsTestAttribute attrib)
        {
            if (attrib == null)
                throw new ArgumentNullException(nameof(attrib));

                var referenceImageFolder = Path.Combine(attrib.ReferenceImagesRoot, TestUtils.GetCurrentTestResultsFolderPath());
                var referenceImagePath = EditorUtils.ReplaceCharacters(Path.Combine(referenceImageFolder, $"{testName}.png"));
                var referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);

            if (referenceImage == null)
            {
                referenceImagePath = EditorUtils.ReplaceCharacters(Path.Combine(attrib.ReferenceImagesRoot, $"{testName}.png"));
                referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);
            }

            return referenceImage;
        }

        private static void TraverseTests(ITestAdaptor test, List<GraphicsTestCase> collection)
        {
            if (!test.HasChildren && Array.IndexOf(test.Categories, "Graphics") >= 0
                && CodeBasedGraphicsTestAttribute.TryFindAttributeOn(test, out var attrib))
            {
                var referenceImage = LoadReferenceImage(test.Name, attrib);
                collection.Add(new GraphicsTestCase(test.Name, attrib, referenceImage));
            }

            foreach (var child in test.Children)
                TraverseTests(child, collection);
        }
    }
#endif
}
