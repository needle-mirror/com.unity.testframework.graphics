using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Declares assets that a test fixture or method requires at run time on every platform. The
    /// build pipeline packs the declared assets into a content bundle for player builds, and the
    /// assets are available through <see cref="GraphicsTestCase.TestData"/> in the Editor and in
    /// players alike. Declarations that resolve to no assets fail the build; assets missing at run
    /// time fail the test.
    /// </summary>
    /// <example>
    /// [RequireTestData("ssao-testdata",
    ///     "Assets/Scenes/500_SSAO/depth.exr",
    ///     "Assets/Scenes/500_SSAO/*.json")]
    /// public class SSAOShaderTestCases { }
    /// </example>
    /// <remarks>
    /// Subclass and override <see cref="CreateDescriptor"/> to supply a custom
    /// <see cref="ITestDataDescriptor"/> with programmatic asset enumeration or custom addressing.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireTestDataAttribute : Attribute
    {
        readonly string m_BundleName;
        readonly string[] m_AssetPaths;

        /// <summary>
        /// Declares required test data assets packed into a bundle with the given logical name.
        /// Declarations sharing a bundle name, including declarations on different fixtures, are
        /// merged into one bundle. Asset paths support '*' wildcards over one directory, e.g.
        /// "Assets/Scenes/500_SSAO/*.json".
        /// </summary>
        /// <param name="bundleName">
        /// The logical bundle name, or null to derive it from the declaring type.
        /// </param>
        /// <param name="assetPaths">Asset paths or wildcard patterns to include.</param>
        public RequireTestDataAttribute(string bundleName, params string[] assetPaths)
        {
            m_BundleName = bundleName;
            m_AssetPaths = assetPaths ?? Array.Empty<string>();
        }

        /// <summary>
        /// The logical bundle name, or null when the name is derived from the declaring type.
        /// </summary>
        public string BundleName => m_BundleName;

        /// <summary>
        /// The default bundle name for a declaring type: its full identity, folded to the characters
        /// a bundle name allows. Short names alone would collide across namespaces and would carry
        /// CLR generic arity markers that no bundle name may contain.
        /// </summary>
        static string DeriveBundleName(Type declaringType)
        {
            if (declaringType == null)
                return "testdata";

            var identity = declaringType.FullName ?? declaringType.Name;

            // A constructed generic's FullName carries assembly-qualified arguments; the open name
            // is identity enough.
            var arguments = identity.IndexOf('[');
            if (arguments >= 0)
                identity = identity.Substring(0, arguments);

            var name = new System.Text.StringBuilder(identity.Length);
            foreach (var c in identity)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    name.Append(char.ToLowerInvariant(c));
                else if (name.Length > 0 && name[name.Length - 1] != '-')
                    name.Append('-');
            }

            while (name.Length > 0 && name[name.Length - 1] == '-')
                name.Length--;

            return name.Length > 0 ? name.ToString() : "testdata";
        }

        /// <summary>
        /// The declared asset paths or wildcard patterns.
        /// </summary>
        public IReadOnlyList<string> AssetPaths => m_AssetPaths;

        /// <summary>
        /// Creates the <see cref="ITestDataDescriptor"/> for this declaration: by default a
        /// <see cref="TestDataDescriptor"/> over <see cref="AssetPaths"/>, with the bundle name
        /// defaulting to the lower-cased declaring type name.
        /// </summary>
        /// <param name="declaringType">The type the attribute is declared on.</param>
        /// <returns>The descriptor describing the declared assets.</returns>
        public virtual ITestDataDescriptor CreateDescriptor(Type declaringType)
        {
            var bundleName = string.IsNullOrEmpty(m_BundleName) ? DeriveBundleName(declaringType) : m_BundleName;

            return new TestDataDescriptor(bundleName, m_AssetPaths);
        }
    }
}
