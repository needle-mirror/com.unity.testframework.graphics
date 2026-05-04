using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools.Graphics.Platforms;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics.TestCases
{
    /// <summary>
    /// A built-in implementation of the <see cref="GraphicsTestCaseSource"/> class.
    /// This class is used to create a graphics test case from a scene path.
    /// </summary>
    public class SceneGraphicsTestCaseSource : GraphicsTestCaseSource
    {
        SceneSource m_SceneSource;

        internal SceneSource SceneSource
        {
            get
            {
                if (m_SceneSource == null)
                {
#if UNITY_EDITOR
                    m_SceneSource = new FileSystemSceneSource();
#else
                    m_SceneSource = new BuildSettingsSceneSource();
#endif
                }
                return m_SceneSource;
            }

            set
            {
                m_SceneSource = value;
            }
        }

        ///<inheritdoc/>
        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite)
        {
            var rawPaths = SceneSource.GetPathsFromAttribute(methodInfo);
#if UNITY_EDITOR
            var allPaths = new List<string>();
            foreach (var path in rawPaths)
            {
                foreach (var resolved in ResolveScenePaths(path))
                    allPaths.Add(resolved.SanitizeBackslashes());
            }
            rawPaths = allPaths.ReorderBasedOnBuildSettings();
#endif

           foreach (var path in rawPaths)
           {
               yield return
                   new SceneGraphicsTestCase(Path.GetFileNameWithoutExtension(path), methodInfo, suite, path);
           }
        }

        IEnumerable<string> ResolveScenePaths(string scenePath)
        {
            var scenePaths = new List<string>();
            if (SceneSource.IsValidFolder(scenePath))
            {
                foreach (var path in SceneSource.GetPathsFromDirectory(scenePath))
                {
                    scenePaths.Add(path);
                }
            }
            else if (SceneSource.PathExists(scenePath))
            {
                scenePaths.Add(scenePath);
            }
            else
            {
                foreach (var path in SceneSource.GetPathsFromRegex(scenePath))
                {
                    scenePaths.Add(path);
                }
            }
            return scenePaths;
        }
    }
}
