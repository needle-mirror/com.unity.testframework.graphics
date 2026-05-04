using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    abstract class SceneSource
    {
        internal abstract bool PathExists(string path);
        internal abstract bool IsValidFolder(string path);
        internal abstract IList<string> GetPathsFromDirectory(string directory);
        internal abstract IList<string> GetPathsFromRegex(string source);
        internal abstract IList<string> GetPathsFromAttribute(IMethodInfo methodInfo);
    }
}
