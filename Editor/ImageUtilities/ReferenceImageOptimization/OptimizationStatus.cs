namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Represents the status of an optimization process.
    /// This enumeration defines various states that can occur during the optimization of reference images,
    /// including whether the optimization was successful, encountered an error, was cancelled, or is currently running.
    /// It is used to provide feedback on the outcome of the optimization process, allowing users
    /// to understand the state of the optimization and take appropriate actions based on the status.
    /// The values in this enumeration can be used to track the progress of the optimization and handle
    /// different scenarios that may arise during the optimization workflow.
    /// </summary>
    public enum OptimizationStatus
    {
        /// <summary>
        /// Indicates that no optimization has been performed yet.
        /// This status is typically used to represent the initial state before any optimization actions have been taken.
        /// It signifies that the optimization process has not started or has been reset.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the optimization was successful.
        /// This status is used when the optimization process has completed without any errors,
        /// and the reference images have been optimized as intended.
        /// It signifies that the optimization achieved its goals and the images are now in an optimized state
        /// </summary>
        Success = 1,

        /// <summary>
        /// Indicates that an error occurred during the optimization process.
        /// This status is used when the optimization could not be completed due to an unexpected issue,
        /// such as a failure in processing the images or an exception being thrown.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Indicates that the optimization process was cancelled.
        /// This status is used when the optimization was intentionally stopped before completion,
        /// either by user action or programmatically.
        /// It signifies that the optimization did not finish and any changes made up to that point may not be finalized.
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Indicates that the optimization process is currently running.
        /// This status is used to represent an ongoing optimization operation,
        /// where the process has started but has not yet completed.
        /// It signifies that the optimization is in progress and the final outcome is not yet determined.
        /// </summary>
        Running = 4,
    }
}
