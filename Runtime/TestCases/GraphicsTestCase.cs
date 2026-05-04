using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Represents one automatically-generated graphics test case.
    /// </summary>
    [Serializable]
    public record GraphicsTestCase
    {
        const int k_MaxNumberOfAdditionalReferenceImages = 10000;

        [SerializeField]
        string name;

        /// <summary>
        /// The name of the test case. Note that this is not the full name and may not be unique.
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        [SerializeField]
        string fileName;

        /// <summary>
        /// The name of the Graphics Test Case converted to a valid file name. <see cref="StringExtensions.ToValidPath"/>
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        [SerializeField]
        string fullName;

        /// <summary>
        /// The full name of the test case, including the class name.
        /// This is used to uniquely identify the test case.
        /// </summary>
        public string FullName
        {
            get => fullName;
            set => fullName = value;
        }

        IReferenceImageFileDescriptor m_ReferenceImageDescriptor;

        /// <summary>
        /// Gets or initializes a descriptor that is used to find the reference image(s)
        /// of a graphics test case.
        /// </summary>
        public IReferenceImageFileDescriptor ReferenceImageDescriptor
        {
            get
            {
                if (m_ReferenceImageDescriptor == null)
                {
                    GraphicsTestLogger.LogWarning(
                        $"ReferenceImageDescriptor for test case '{Name ?? FullName ?? "Unknown"}' was not initialized. Test will be skipped.");
                }
                return m_ReferenceImageDescriptor;
            }
            init => m_ReferenceImageDescriptor = value;
        }

        /// <summary>
        /// Gets a collection of reference images used by this test case. Enumeration breaks once the image is not found.
        /// </summary>
        public IEnumerable<ReferenceImage> AdditionalReferenceImages
        {
            get
            {
                var descriptor = ReferenceImageDescriptor;
                if (descriptor == null)
                    yield break;

                for (var i = 0; ; i++)
                {
                    if (i >= k_MaxNumberOfAdditionalReferenceImages)
                    {
                        GraphicsTestLogger.LogWarning(
                            $"You have reached the maximum supported number of reference images: {k_MaxNumberOfAdditionalReferenceImages}."
                        );
                        yield break;
                    }

                    var currentImage = new ReferenceImage(
                        descriptor.BuildVariant(i),
                        descriptor.Format,
                        descriptor.Extension
                    );
                    if (currentImage.Image is null)
                    {
                        yield break;
                    }

                    yield return currentImage;
                }
            }
        }

        [NonSerialized]
        IMethodInfo m_MethodInfo;

        /// <summary>
        /// The method that this test case is generated from.
        /// </summary>
        public IMethodInfo MethodInfo
        {
            get => m_MethodInfo;
            set => m_MethodInfo = value;
        }

        [NonSerialized]
        ITest m_Fixture;

        /// <summary>
        /// The parent test fixture for this test case.
        /// </summary>
        public ITest Fixture
        {
            get => m_Fixture;
            set => m_Fixture = value;
        }

        [NonSerialized]
        ReferenceImage m_ReferenceImage;

        /// <summary>
        /// Gets the reference image that this test case is compared against.
        /// </summary>
        public ReferenceImage ReferenceImage
        {
            get => m_ReferenceImage;
            set => m_ReferenceImage = value;
        }

        [SerializeField]
        [HideInInspector]
        bool shouldBeIgnored;

        /// <summary>
        /// Whether this test case should be ignored.
        /// </summary>
        public bool ShouldBeIgnored
        {
            get => shouldBeIgnored;
            set => shouldBeIgnored = value;
        }

        [SerializeField]
        [HideInInspector]
        string ignoreReason = string.Empty;

        /// <summary>
        /// The reason why this test case should be ignored.
        /// </summary>
        public string IgnoreReason
        {
            get => ignoreReason;
            set => ignoreReason = value;
        }

        [SerializeField]
        IgnoreGraphicsTestData[] ignoreData;

        /// <summary>
        /// The ignore attributes for this test case.
        /// </summary>
        public IgnoreGraphicsTestData[] IgnoreData
        {
            get => ignoreData;
            set => ignoreData = value;
        }

        [NonSerialized]
        TestCaseData m_TestData;

        /// <summary>
        /// The test case data connected to this Graphics Test Case.
        /// </summary>
        public TestCaseData TestData
        {
            get => m_TestData;
            set => m_TestData = value;
        }

        /// <summary>
        /// The log message for the reference image.
        /// </summary>
        [Obsolete("Use ReferenceImage.LoadMessage instead.")]
        public string ReferenceImagePathLog => ReferenceImage.LoadMessage;

        internal GraphicsTestCase() { }

        /// <summary>
        /// Creates a new GraphicsTestCase with the specified name and method.
        /// This constructor is used to create a graphics test case from a method.
        /// </summary>
        /// <param name="name">
        /// The name of the test case.
        /// This is not the full name and may not be unique.
        /// </param>
        /// <param name="methodInfo">
        /// The method information associated with this test case
        /// </param>
        /// <param name="fixture">
        /// The parent fixture of this test case.
        /// </param>
        public GraphicsTestCase(string name, IMethodInfo methodInfo, ITest fixture)
        {
            Name = name;
            FullName = $"{fixture.FullName}.{methodInfo.MethodInfo.Name}.{name}";
            MethodInfo = methodInfo;
            Fixture = fixture;
        }

        /// <summary>
        /// Returns a string representation of the GraphicsTestCase.
        /// This includes the full name, reference image, whether it should be ignored, and the ignore reason.
        /// </summary>
        /// <returns>
        /// A string representation of the GraphicsTestCase.
        /// </returns>
        public override string ToString()
        {
            return $"GraphicsTestCase: {FullName}\nReferenceImage: {ReferenceImage}\nShouldBeIgnored: {ShouldBeIgnored}\nIgnoreReason: {IgnoreReason}";
        }
    }
}
