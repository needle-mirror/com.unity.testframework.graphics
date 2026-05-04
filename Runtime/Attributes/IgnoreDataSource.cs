using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A source of IgnoreData to use for ignoring Graphics Tests.
    /// Inherit from this class to create a separable ignore data source.
    /// </summary>
    public abstract class IgnoreDataSource : IEnumerable<IgnoreGraphicsTestData>
    {
        /// <summary>
        /// Retrieves the ignore data. Override this method and yield the ignore data objects.
        /// </summary>
        /// <returns>
        /// The ignore data for the data source.
        /// </returns>
        protected abstract IEnumerable<IgnoreGraphicsTestData> GetData();

        /// <summary>
        /// The enumerator for the ignore data source.
        /// </summary>
        /// <returns>
        /// An enumerator over the ignore data.
        /// </returns>
        public IEnumerator<IgnoreGraphicsTestData> GetEnumerator() => GetData().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
