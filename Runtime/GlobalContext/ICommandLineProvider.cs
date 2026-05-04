namespace UnityEngine.TestTools.Graphics
{
    interface ICommandLineProvider
    {
        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <returns>An array of command line arguments.</returns>
        string[] GetCommandLineArgs();
    }
}
