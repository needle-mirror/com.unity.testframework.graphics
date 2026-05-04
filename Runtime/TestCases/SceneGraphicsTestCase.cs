using System;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Represents one automatically-generated graphics test case that uses a scene.
    /// </summary>
    [Serializable]
    public record SceneGraphicsTestCase : GraphicsTestCase
    {
        [SerializeField]
        string scenePath;

        /// <summary>
        /// The path to the scene that this test case uses.
        /// </summary>
        public string ScenePath
        {
            get => scenePath;
            set => scenePath = value;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SceneGraphicsTestCase"/> class.
        /// </summary>
        public SceneGraphicsTestCase() { }

        /// <summary>
        /// Creates a new instance of the <see cref="SceneGraphicsTestCase"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the test case. Note that this is not the full name and may not be unique.
        /// </param>
        /// <param name="methodInfo">
        /// The method info for the test case.
        /// </param>
        /// <param name="suite">
        /// The suite this test case belongs to.
        /// </param>
        /// <param name="scenePath">
        /// The path to the scene that this test case uses.
        /// </param>
        public SceneGraphicsTestCase(string name, IMethodInfo methodInfo, ITest suite, string scenePath)
            : base(name, methodInfo, suite)
        {
            ScenePath = scenePath;
        }

        /// <summary>
        /// Returns a string representation of the test case.
        /// This includes the name, scene path, reference image, and ignore reason.
        /// </summary>
        /// <returns>
        /// A string representation of the test case.
        /// </returns>
        public override string ToString()
        {
            return $"SceneGraphicsTestCase: {FullName}\nScenePath: {ScenePath}\nReferenceImage: {ReferenceImage}\nShouldBeIgnored: {ShouldBeIgnored}\nIgnoreReason: {IgnoreReason}";
        }
    }
}
