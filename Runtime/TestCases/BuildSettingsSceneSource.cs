using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    class BuildSettingsSceneSource : SceneSource
    {
        readonly GraphicsTestBuildSettings m_OverrideSettings;

        public BuildSettingsSceneSource() { }

        internal BuildSettingsSceneSource(GraphicsTestBuildSettings settings)
        {
            m_OverrideSettings = settings;
        }

        GraphicsTestBuildSettings Settings => m_OverrideSettings ?? GraphicsTestBuildSettings.LoadOrDefault();

        internal override bool PathExists(string path)
        {
            throw new NotImplementedException();
        }

        internal override bool IsValidFolder(string path)
        {
            throw new NotImplementedException();
        }

        internal override IList<string> GetPathsFromDirectory(string directory)
        {
            throw new NotImplementedException();
        }

        internal override IList<string> GetPathsFromRegex(string source)
        {
            throw new NotImplementedException();
        }

        internal override IList<string> GetPathsFromAttribute(IMethodInfo methodInfo)
        {
            var parameters = methodInfo.MethodInfo.GetParameters();
            var typeNames = new string[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                typeNames[i] = parameters[i].ParameterType.FullName;
            var identifier = new MethodIdentifier(methodInfo.TypeInfo.FullName, methodInfo.Name, typeNames);
            return Settings.ScenePathsDictionary.TryGetValue(identifier, out var paths)
                ? paths
                : new List<string>();
        }
    }
}
