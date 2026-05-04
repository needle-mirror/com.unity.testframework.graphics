using System;

namespace UnityEngine.TestTools.Graphics
{
    class EnvironmentCommandLineReader : ICommandLineProvider
    {
        /// <summary>
        /// Gets the command line arguments from the environment.
        /// </summary>
        /// <returns>An array of command line arguments.</returns>
        public string[] GetCommandLineArgs()
        {
            return Environment.GetCommandLineArgs();
        }
    }
}
