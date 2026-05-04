using System;
using UnityEngine.TestTools.Graphics.TestCases;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Attribute to create a new Scene Graphics Test. When used, it will create a new test case for the method it is applied to.
    /// </summary>
    /// <remarks>
    /// This attribute uses the <see cref="SceneGraphicsTestCaseSource"/> class to create the test cases.
    /// </remarks>
    public class SceneGraphicsTestAttribute : GraphicsTestAttributeBase
    {
        string[] m_ScenePaths;
        internal string[] ScenePaths
        {
            get => m_ScenePaths;
            private set
            {
                if (value.Length == 0)
                {
                    throw new ArgumentException("You must provide at least one scene path.", nameof(value));
                }

                if (Array.Exists(value, string.IsNullOrWhiteSpace) || Array.Exists(value, string.IsNullOrEmpty))
                {
                    throw new ArgumentException("You must provide at least one scene path.", nameof(value));
                }

                var trimmed = new string[value.Length];
                for (var i = 0; i < value.Length; i++)
                    trimmed[i] = value[i].Trim('/');
                m_ScenePaths = trimmed;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SceneGraphicsTestAttribute"/> class.
        /// </summary>
        /// <param name="scenePaths">
        /// The paths to the scenes to be used for the test. These paths are relative to the project folder and must be in a path that is indexed by the Unity Asset Database.
        /// </param>
        /// <remarks>
        /// <para>
        /// Valid scene paths are:<br/>
        /// - Direct paths to Unity scene assets, e.g. <c>Assets/Scenes/TestScene.unity</c> <br/>
        /// - Paths to asset directories, e.g. <c>Assets/Scenes</c>. In this case, all scenes in the directory and its subdirectories will be included. <br/>
        /// - Regular expressions only for the file names of test scenes, e.g. <c>Assets/Scenes/[0-9]+</c>.
        /// The regular expressions may not affect the base directory, so <c>Assets/.*/TestScene.unity</c> or equivalent is not valid.
        /// You may also not use backslashes as path separators when using the regex feature. <c>Assets\Scenes/[0-9]+</c> is invalid, but <c>Assets/Scenes/\d+</c> is valid. <br/>
        /// - Paths from any directory recognized by the <c>AssetDatabase</c>, so <c>Packages/YourPackageName/Scenes</c> is also valid. <br/>
        /// </para>
        /// </remarks>
        public SceneGraphicsTestAttribute(params string[] scenePaths)
            : base(typeof(SceneGraphicsTestCaseSource))
        {
            ScenePaths = scenePaths;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SceneGraphicsTestAttribute"/> class.
        /// </summary>
        /// <param name="source">
        /// The type of the source to be used for the test. This is usually <see cref="SceneGraphicsTestCaseSource"/> or a derived class.
        /// </param>
        /// <param name="scenePaths">
        /// The paths to the scenes to be used for the test. These paths are relative to the project folder and must be in a path that is indexed by the Unity Asset Database.
        /// </param>
        /// <remarks>
        /// <para>
        /// Valid scene paths are:<br/>
        /// - Direct paths to Unity scene assets, e.g. <c>Assets/Scenes/TestScene.unity</c> <br/>
        /// - Paths to asset directories, e.g. <c>Assets/Scenes</c>. In this case, all scenes in the directory and its subdirectories will be included. <br/>
        /// - Regular expressions only for the file names of test scenes, e.g. <c>Assets/Scenes/[0-9]+</c>.
        /// The regular expressions may not affect the base directory, so <c>Assets/.*/TestScene.unity</c> or equivalent is not valid.
        /// You may also not use backslashes as path separators when using the regex feature. <c>Assets\Scenes/[0-9]+</c> is invalid, but <c>Assets/Scenes/\d+</c> is valid. <br/>
        /// - Paths from any directory recognized by the <c>AssetDatabase</c>, so <c>Packages/YourPackageName/Scenes</c> is also valid. <br/>
        /// </para>
        /// </remarks>
        public SceneGraphicsTestAttribute(Type source, params string[] scenePaths)
            : base(source)
        {
            ScenePaths = scenePaths;
        }
    }
}
