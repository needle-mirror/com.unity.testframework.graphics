using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics
{
    static class EnumerableExtensions
    {
        internal static List<string> ReorderBasedOnBuildSettings(this IEnumerable<string> scenePaths)
        {
#if UNITY_EDITOR
            var collection = ToArray(scenePaths);
            var scenePathSet = new HashSet<string>(collection);
            var seen = new HashSet<string>();
            var result = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scenePathSet.Contains(scene.path) && seen.Add(scene.path))
                    result.Add(scene.path);
            }

            foreach (var path in collection)
            {
                if (seen.Add(path))
                    result.Add(path);
            }

            return result;
#else
            return new List<string>(scenePaths);
#endif
        }

        static string[] ToArray(IEnumerable<string> source)
        {
            if (source is string[] array)
                return array;

            var list = new List<string>(source);
            return list.ToArray();
        }
    }
}
