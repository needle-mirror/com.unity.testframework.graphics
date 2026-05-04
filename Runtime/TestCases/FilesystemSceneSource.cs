using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics
{
    class FileSystemSceneSource : SceneSource
    {
        static readonly ConcurrentDictionary<string, Regex> k_RegexCache = new();
        static readonly ConcurrentDictionary<string, IList<string>> k_PathsFromRegexCache = new();
#if UNITY_EDITOR
        // Cache FindAssets results per directory - avoids repeated AssetDatabase queries
        static readonly ConcurrentDictionary<string, List<string>> k_DirectoryScenePathsCache = new();
#endif

        internal static void ClearCaches()
        {
            k_RegexCache.Clear();
            k_PathsFromRegexCache.Clear();
#if UNITY_EDITOR
            k_DirectoryScenePathsCache.Clear();
#endif
        }

        internal override bool PathExists(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.AssetPathExists(path.SanitizeBackslashes());
#else
            return false;
#endif
        }

        internal override bool IsValidFolder(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.IsValidFolder(path.SanitizeBackslashes());
#else
            return false;
#endif
        }

        internal override  IList<string> GetPathsFromDirectory(string directory)
        {
            return Directory.GetFiles(
                directory.SanitizeBackslashes(),
                "*.unity",
                SearchOption.AllDirectories
            );
        }

        internal override IList<string> GetPathsFromRegex(string source)
        {
#if UNITY_EDITOR
            if (k_PathsFromRegexCache.TryGetValue(source, out var cached))
                return cached;

            var lastIndex = source.LastIndexOf('/');
            if (lastIndex == -1)
            {
                throw new FileNotFoundException("Could not find scene path: " + source);
            }
            var directory = source.Substring(0, lastIndex);
            var pattern = source.Substring(lastIndex + 1);

            var regex = k_RegexCache.GetOrAdd(
                pattern,
                p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1))
            );

            // Cache scene paths per directory to avoid repeated FindAssets + GUIDToAssetPath calls
            var scenePaths = k_DirectoryScenePathsCache.GetOrAdd(directory, dir =>
            {
                var guids = AssetDatabase.FindAssets("t:SceneAsset", new[] { dir });
                var paths = new List<string>(guids.Length);
                foreach (var guid in guids)
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
                return paths;
            });

            var results = new List<string>();
            foreach (var path in scenePaths)
            {
                var fileName = path.Substring(path.LastIndexOf('/') + 1);
                if (regex.IsMatch(fileName))
                    results.Add(path);
            }

            k_PathsFromRegexCache.TryAdd(source, results);
            return results;
#else
            return Array.Empty<string>();
#endif
        }

        internal override IList<string> GetPathsFromAttribute(IMethodInfo methodInfo)
        {
            var methodAttrs = Attribute.GetCustomAttributes(methodInfo.MethodInfo, true);
            var typeAttrs = Attribute.GetCustomAttributes(methodInfo.MethodInfo.DeclaringType!, true);
            foreach (var att in methodAttrs)
            {
                if ((att.GetType().IsSubclassOf(typeof(SceneGraphicsTestAttribute)) || att.GetType() == typeof(SceneGraphicsTestAttribute))
                    && att is SceneGraphicsTestAttribute sattr)
                    return sattr.ScenePaths;
            }
            foreach (var att in typeAttrs)
            {
                if ((att.GetType().IsSubclassOf(typeof(SceneGraphicsTestAttribute)) || att.GetType() == typeof(SceneGraphicsTestAttribute))
                    && att is SceneGraphicsTestAttribute sattr)
                    return sattr.ScenePaths;
            }
            throw new InvalidOperationException("Expected exactly one SceneGraphicsTestAttribute per test.");
        }
    }
}
