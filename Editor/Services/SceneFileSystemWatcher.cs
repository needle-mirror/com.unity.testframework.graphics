using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.Services
{
    [InitializeOnLoad]
    static class SceneFileSystemWatcher
    {
        static readonly HashSet<FileSystemWatcher> k_Watchers = new();
        static volatile bool s_RecompileRequested;

        static SceneFileSystemWatcher()
        {
            EditorApplication.update += OnUpdate;
            GraphicsTestAttributeBase.TestCaseCreated += OnGraphicsTestCaseCreated;
        }

        static void OnGraphicsTestCaseCreated(object sender, GraphicsTestCaseCreatedArgs e)
        {
            if (!GraphicsTestBuildSettings.LoadOrDefault().ReloadDomainWhenEditingTestSceneAssets)
            {
                EditorApplication.update -= OnUpdate;
                GraphicsTestAttributeBase.TestCaseCreated -= OnGraphicsTestCaseCreated;
            }
            else if (e.TestCase is SceneGraphicsTestCase sceneGraphicsTestCase)
            {
                RegisterFileSystemWatcher(Path.GetDirectoryName(sceneGraphicsTestCase.ScenePath));
            }
        }

        static void RegisterFileSystemWatcher(string path)
        {
            var dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            var pathNotInWatchers = true;
            foreach (var w in k_Watchers)
            {
                if (w.Path == dir)
                {
                    pathNotInWatchers = false;
                    break;
                }
            }
            if (Directory.Exists(dir) && pathNotInWatchers)
            {
                FileSystemWatcher watcher = new();
                watcher.Path = dir;

                watcher.Filter = "*.unity";
                watcher.Created += OnFileEventTriggered;
                watcher.Deleted += OnFileEventTriggered;
                watcher.Renamed += OnFileEventTriggered;
                watcher.Error += OnError;
                watcher.EnableRaisingEvents = true;
                watcher.IncludeSubdirectories = true;

                k_Watchers.Add(watcher);
            }
        }

        static void OnFileEventTriggered(object source, FileSystemEventArgs e)
        {
            s_RecompileRequested = true;
        }

        static void OnError(object sender, ErrorEventArgs e) => PrintException(e.GetException());

        static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                GraphicsTestLogger.DebugLog($"Message: {ex.Message}\nStacktrace: {ex.StackTrace}");
                PrintException(ex.InnerException);
            }
        }

        static void OnUpdate()
        {
            if (
                s_RecompileRequested && GraphicsTestBuildSettings.LoadOrDefault().ReloadDomainWhenEditingTestSceneAssets
            )
            {
                EditorApplication.update -= OnUpdate;
                GraphicsTestAttributeBase.TestCaseCreated -= OnGraphicsTestCaseCreated;

                s_RecompileRequested = false;
                foreach (var watcher in k_Watchers)
                {
                    watcher.Dispose();
                }

                k_Watchers.Clear();
                EditorUtility.RequestScriptReload();
            }
        }
    }
}
