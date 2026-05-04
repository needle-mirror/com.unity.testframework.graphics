using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Represents the result of an image optimization process.
    /// This class encapsulates the outcome of optimizing reference images,
    /// including the time taken, the number of comparisons made, and the status of the optimization.
    /// It also includes lists of deleted and moved files as part of the optimization process.
    /// </summary>
    public class OptimizationResult : EventArgs
    {
        /// <summary>
        /// The time taken to complete the optimization process.
        /// This property indicates how long the optimization took from start to finish.
        /// </summary>
        public TimeSpan ElapsedTime { get; init; }

        /// <summary>
        /// The total number of image comparisons performed during the optimization.
        /// This property provides insight into the scale of the optimization effort,
        /// indicating how many images were compared to determine the best reference images.
        /// </summary>
        public int TotalComparisons { get; init; }

        /// <summary>
        /// The number of unique image comparisons made during the optimization.
        /// This property indicates how many distinct pairs of images were compared,
        /// helping to understand the efficiency of the optimization process.
        /// </summary>
        public int UniqueComparisons { get; init; }

        /// <summary>
        /// The total number of texture loads that occurred during the optimization.
        /// This property tracks how many times textures were loaded into memory,
        /// including the times we hit the LRU texture cache.
        /// </summary>
        public int TotalTextureLoads { get; init; }

        /// <summary>
        ///  The total number of texture loads that occurred during the optimization.
        /// This property tracks how many times textures were loaded from disk,
        /// without considering cache hits.
        /// It helps to understand the efficiency of texture loading during the optimization process.
        /// It is particularly useful for identifying how many unique textures were loaded,
        /// which can impact performance and memory usage.
        /// </summary>
        public int UniqueTextureLoads { get; init; }

        /// <summary>
        /// A list of file paths that were deleted as part of the optimization process.
        /// This property contains the paths of files that were removed to streamline the reference images,
        /// helping to reduce clutter and improve the efficiency of the image set.
        /// </summary>
        public IList<string> DeletedFiles { get; init; }

        /// <summary>
        /// A list of tuples representing files that were moved during the optimization.
        /// Each tuple contains the source file path and the destination file path.
        /// This property is useful for tracking changes made to the file structure during optimization,
        /// allowing users to see which files were relocated and where they were moved to.
        /// </summary>
        public IList<(string Source, string Destination)> MovedFiles { get; init; }

        IList<string> DeletedFromMoving
        {
            get
            {
                var list = new List<string>(MovedFiles.Count);
                foreach (var f in MovedFiles)
                    list.Add(f.Source);
                return list;
            }
        }

        IList<string> NewFromMoving
        {
            get
            {
                var list = new List<string>(MovedFiles.Count);
                foreach (var f in MovedFiles)
                    list.Add(f.Destination);
                return list;
            }
        }

        /// <summary>
        /// The status of the optimization process.
        /// This property indicates whether the optimization was successful, failed, or encountered any issues.
        /// It provides a summary of the outcome, allowing users to quickly assess the result of the optimization.
        /// </summary>
        public OptimizationStatus Status { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var allDeleted = new List<string>(DeletedFiles);
            foreach (var f in DeletedFromMoving)
                allDeleted.Add(f);
            var deletedResult = $"({allDeleted.Count})\n\t\t{string.Join("\n\t\t", allDeleted)}";
            var newFilesResult = $"({NewFromMoving.Count})\n\t\t{string.Join("\n\t\t", NewFromMoving)}";
            return @$"
    Result: {Status}
    Elapsed Time: {ElapsedTime}
    Total Comparisons: {TotalComparisons}
        Cache Hit Rate: {(1 - (UniqueComparisons / Mathf.Max(1.0f, TotalComparisons))) * 100}%
        Unique Comparisons: {UniqueComparisons}
    Total Texture Loads: {TotalTextureLoads}
        Cache Hit Rate: {(1 - (UniqueTextureLoads / Mathf.Max(1.0f, TotalTextureLoads))) * 100}%
        Unique Texture Loads: {UniqueTextureLoads}
    Deleted Files: {deletedResult}
    New Files: {newFilesResult}";
        }
    }
}
