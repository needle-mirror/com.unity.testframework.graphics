using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// A logger for the graphics tests. This class is used to log messages to a file.
    /// The default log file is created as <c>Logs/GraphicsTestLogs.log</c> in the project's root directory.
    /// </summary>
    public sealed class GraphicsTestLogger : IDisposable
    {
        const string k_DefaultLogPath = "../Logs/GraphicsTestLogs.log";
        internal StreamWriter Writer { get; set; }

        private GraphicsTestLogger(string logPath)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                Debug.LogWarning("Graphics Tests: Log path is empty. Using default log path.");
                logPath = Path.Combine(Application.dataPath, k_DefaultLogPath);
            }
            else if (!Path.IsPathRooted(logPath))
            {
                logPath = Path.Combine(Application.dataPath, logPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            Writer = new StreamWriter(logPath, true);
        }

        /// <summary>
        /// Logs a message to the log file. The message is also written to the Unity console.
        /// </summary>
        /// <param name="type">
        /// The type of message to log. This can be LogType.Error, LogType.Warning, or LogType.Log.
        /// </param>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="logPath">
        /// The path to the log file. If this is null or empty, the default log path is used.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the type is not a valid LogType.
        /// </exception>

        public static void Log(LogType type, string message, string logPath = k_DefaultLogPath)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            using (GraphicsTestLogger logger = new GraphicsTestLogger(logPath))
            {
                string timestamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                switch (type)
                {
                    case LogType.Error:
                        Debug.LogError($"Graphics Tests: {message}");
                        logger.Writer.WriteLine($"{timestamp} - [Error]:\t{message}");
                        break;
                    case LogType.Warning:
                        Debug.LogWarning($"Graphics Tests: {message}");
                        logger.Writer.WriteLine($"{timestamp} - [Warn]:\t{message}");
                        break;
                    case LogType.Log:
                        Debug.Log($"Graphics Tests: {message}");
                        logger.Writer.WriteLine($"{timestamp} - [Info]:\t{message}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
        }

        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}
