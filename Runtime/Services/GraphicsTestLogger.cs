using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking.PlayerConnection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A logger for the graphics tests. This class is used to log messages to a file.
    /// The default log file is created as <c>Logs/GraphicsTestLogs.log</c> in the project's root directory.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class GraphicsTestLogger
    {
        const string k_DebugEnvironmentVariable = "GRAPHICS_TEST_FRAMEWORK_DEBUG";
        const string k_DebugCommandLineArg = "-graphics-test-framework-debug";
        const string k_DefaultLogPath = "Logs/GraphicsTestLogs.log";
        const char k_LogSeparator = '\x1F';
        static readonly CommandLineReader s_CommandLineReader = new();

        /// <summary>
        /// The most recent log path used by the test logger.
        /// </summary>
        public static string MostRecentLogPath = k_DefaultLogPath;
        static bool s_DebugMode;
        static readonly Guid k_LogChannel = new("9a3c8e52-4af7-442b-95ef-4d9f28c91f5f");
        static readonly ConcurrentQueue<string> k_LOGQueue = new();
        const int k_BufferSize = 1024;
        static readonly List<string> k_LOGBuffer = new(k_BufferSize);
        static readonly object k_LOGBufferLock = new();
        static readonly SemaphoreSlim k_LOGSignal = new(0);
        static readonly CancellationTokenSource k_Cts = new();
        static string s_ResolvedLogPath;
        static long s_TotalLogsAdded;
        static long s_TotalLogsProcessed;

#if UNITY_EDITOR
        static GraphicsTestLogger()
        {
            Initialize();
            EditorApplication.quitting += OnExit;
        }
#else // UNITY_PLAYER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitPlayerRuntime()
        {
            Initialize();
            Application.quitting += OnExit;
        }
#endif

        static void Initialize()
        {
            s_DebugMode = IsDebugEnabled();
            s_ResolvedLogPath = SafeResolveLogPath(k_DefaultLogPath);

            EnsureLogDirectory();
            MigrateOrLoadExistingLogs();

#if UNITY_EDITOR
            EditorConnection.instance.RegisterConnection(OnPlayerConnected);
            EditorConnection.instance.RegisterDisconnection(OnPlayerDisconnected);
            EditorConnection.instance.Register(k_LogChannel, OnPlayerLogReceived);
#else // UNITY_PLAYER
            MainThreadDispatcher.Initialize();
            PlayerConnection.instance.RegisterConnection((id) => { });
            PlayerConnection.instance.RegisterDisconnection((id) => { });
#endif
            Task.Run(() => LogWriterLoop(k_Cts.Token));
        }

        static void EnsureLogDirectory()
        {
            var logDirectory = Path.GetDirectoryName(s_ResolvedLogPath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }

        static void MigrateOrLoadExistingLogs()
        {
            if (!File.Exists(s_ResolvedLogPath))
                return;

            var text = File.ReadAllText(s_ResolvedLogPath);
            if (!string.IsNullOrEmpty(text) && !text.Contains(k_LogSeparator))
            {
                var logDirectory = Path.GetDirectoryName(s_ResolvedLogPath) ?? "Logs";
                File.Move(
                    s_ResolvedLogPath,
                    Path.Combine(logDirectory, $"{Path.GetFileNameWithoutExtension(s_ResolvedLogPath)}.old.log")
                );
                return;
            }

            var lines = text.Split(k_LogSeparator);
            var linesToAdd = Math.Min(lines.Length, k_BufferSize - k_LOGBuffer.Count);
            var startIndex = lines.Length - linesToAdd;
            for (var i = startIndex; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    k_LOGBuffer.Add(lines[i].TrimEnd(k_LogSeparator).Trim());
            }
        }

#if UNITY_EDITOR
        static void OnPlayerConnected(int playerId) { }

        static void OnPlayerDisconnected(int playerId) { }
#endif

        /// <summary>
        /// Logs a message. The message is written to the buffer, log file and/or console, depending on settings.
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
        /// <param name="logToConsole">
        /// Whether to log the message to the Unity console. If this is false, the message is only logged to the file.
        /// </param>
        /// <remarks>
        /// This method logs a message to the log file and the Unity console.
        /// The message is prefixed with a timestamp and the type of message.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the type is not a valid LogType.
        /// </exception>
        public static void Log(
            LogType type,
            string message,
            string logPath = k_DefaultLogPath,
            bool logToConsole = true
        )
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Trim(k_LogSeparator)))
                return;

            MostRecentLogPath = logPath;

            var timestamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            message = message.Trim();
            var formattedMessage = type switch
            {
                LogType.Error => $"{timestamp} - [ERROR]:\t{message}{k_LogSeparator}",
                LogType.Warning => $"{timestamp} - [WARN]:\t{message}{k_LogSeparator}",
                LogType.Log => $"{timestamp} - [INFO]:\t{message}{k_LogSeparator}",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            if (logToConsole)
            {
                MainThreadDispatcher.RunOnMainThread(() =>
                {
                    switch (type)
                    {
                        case LogType.Error:
                            Debug.LogError($"Graphics Tests: {message}");
                            break;
                        case LogType.Warning:
                            Debug.LogWarning($"Graphics Tests: {message}");
                            break;
                        default:
                            Debug.Log($"Graphics Tests: {message}");
                            break;
                    }
                });
            }

            k_LOGQueue.Enqueue(formattedMessage);
            k_LOGSignal.Release();
            Interlocked.Increment(ref s_TotalLogsAdded);
        }

        ///<inheritdoc cref="Log(UnityEngine.LogType,string,string,bool)"/>
        public static void Log(string message, bool logToConsole = true) =>
            Log(LogType.Log, message, logToConsole: logToConsole);

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        /// <param name="logPath">
        /// The path to the log file. If this is null or empty, the default log path is used.
        /// </param>
        /// <param name="logToConsole">
        /// Whether to log the message to the Unity console. If this is false, the message is only logged to the file.
        /// </param>
        public static void LogWarning(
            string message,
            string logPath = k_DefaultLogPath,
            bool logToConsole = true
        ) => Log(LogType.Warning, message, logPath, logToConsole);

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        /// <param name="logPath">
        /// The path to the log file. If this is null or empty, the default log path is used.
        /// </param>
        /// <param name="logToConsole">
        /// Whether to log the message to the Unity console. If this is false, the message is only logged to the file.
        /// </param>
        public static void LogError(
            string message,
            string logPath = k_DefaultLogPath,
            bool logToConsole = true
        ) => Log(LogType.Error, message, logPath, logToConsole);

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception">
        /// The exception to be logged.
        /// </param>
        /// <param name="logPath">
        /// The path to the log file. If this is null or empty, the default log path is used.
        /// </param>
        /// <param name="logToConsole">
        /// Whether to log the exception to the Unity console. If this is false, the message is only logged to the file.
        /// </param>
        public static void LogException(
            Exception exception,
            string logPath = k_DefaultLogPath,
            bool logToConsole = true
        )
        {
            if (exception == null)
                return;
            var formatted =
                $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
            Log(LogType.Error, formatted, logPath, logToConsole);
        }

        /// <summary>
        /// Logs a message to the file. The message will appear in the console if debug mode is on.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void DebugLog(string message) => Log($"[DEBUG] {message}", logToConsole: s_DebugMode);

        /// <summary>
        /// Logs a warning to the file. The message will appear in the console if debug mode is on.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void DebugWarning(string message) =>
            LogWarning($"[DEBUG] {message}", logToConsole: s_DebugMode);

        /// <summary>
        /// Logs an error to the file. The message will appear in the console if debug mode is on.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void DebugError(string message) =>
            LogError($"[DEBUG] {message}", logToConsole: s_DebugMode);

        /// <summary>
        /// Logs an exception to the file. The message will appear in the console if debug mode is on.
        /// </summary>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        public static void DebugException(Exception exception) =>
            LogException(exception, logToConsole: s_DebugMode);

        internal static IReadOnlyList<string> GetLogBuffer()
        {
            lock (k_LOGBufferLock)
                return k_LOGBuffer.AsReadOnly();
        }

        internal static void ClearLogBuffer()
        {
            lock (k_LOGBufferLock)
            {
                k_LOGBuffer.Clear();
            }
        }

        internal static long GetTotalLogsAdded()
        {
            return Interlocked.Read(ref s_TotalLogsAdded);
        }

        internal static void WaitForQueueDrained(int timeoutMs = 2000)
        {
            if (Interlocked.Read(ref s_TotalLogsProcessed) >= Interlocked.Read(ref s_TotalLogsAdded))
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var spin = new SpinWait();
            while (Interlocked.Read(ref s_TotalLogsProcessed) < Interlocked.Read(ref s_TotalLogsAdded))
            {
                if (sw.ElapsedMilliseconds >= timeoutMs)
                    return;
                spin.SpinOnce();
            }
        }

        static async Task LogWriterLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await k_LOGSignal.WaitAsync(token);
                        while (k_LOGQueue.TryDequeue(out var line))
                        {
                            lock (k_LOGBufferLock)
                            {
                                k_LOGBuffer.Add(line);
                                if (k_LOGBuffer.Count > k_BufferSize)
                                    k_LOGBuffer.RemoveAt(0);
                            }

#if UNITY_EDITOR
                            WriteToFile(line);
#else
                            SendToConnectedEditor(line);
#endif
                            Interlocked.Increment(ref s_TotalLogsProcessed);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception innerEx)
                    {
                        Debug.LogError($"[GraphicsTestLogger] LogWriterLoop exception: {innerEx}");
                    }
                }
            }
            catch (Exception outerEx)
            {
                Debug.LogError(
                    $"[GraphicsTestLogger] LogWriterLoop unhandled outer exception: {outerEx}"
                );
            }
        }

        static void WriteToFile(string message)
        {
            try
            {
                using var writer = new StreamWriter(s_ResolvedLogPath, append: true);
                writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[GraphicsTestLogger] File write failed to '{s_ResolvedLogPath}': {ex.Message}"
                );
            }
        }

#if !UNITY_EDITOR
        static void SendToConnectedEditor(string message)
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                if (!PlayerConnection.instance.isConnected)
                {
                    Debug.LogWarning(
                        "[GraphicsTestLogger] Player not connected to Editor. Logs will not be sent."
                    );
                    return;
                }

                const int chunkSize = 512;
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                try
                {
                    for (int i = 0; i < messageBytes.Length; i += chunkSize)
                    {
                        int length = Math.Min(chunkSize, messageBytes.Length - i);
                        byte[] chunk = new byte[length];
                        Buffer.BlockCopy(messageBytes, i, chunk, 0, length);
                        PlayerConnection.instance.Send(k_LogChannel, chunk);
                    }
                }
                catch (Exception sendEx)
                {
                    Debug.LogError(
                        $"[GraphicsTestLogger] Player failed to send log chunk: {sendEx.Message}"
                    );
                }
            });
        }
#else // UNITY_EDITOR
        static void OnPlayerLogReceived(MessageEventArgs args)
        {
            try
            {
                if (args.data == null || args.data.Length == 0)
                {
                    return;
                }

                var messageChunk = Encoding.UTF8.GetString(args.data);
                var message = $"[PLAYER ID:{args.playerId}] {messageChunk}";

                lock (k_LOGBufferLock)
                {
                    k_LOGBuffer.Add(message);
                    if (k_LOGBuffer.Count > k_BufferSize)
                        k_LOGBuffer.RemoveAt(0);
                }
                WriteToFile(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphicsTestLogger] Editor failed to handle player log: {ex}");
            }
        }
#endif

        static string SafeResolveLogPath(string logPath)
        {
#if UNITY_EDITOR
            var basePath = Path.GetDirectoryName(Application.dataPath);
#else
            string basePath = Application.persistentDataPath;
#endif
            if (string.IsNullOrWhiteSpace(logPath))
                logPath = k_DefaultLogPath;

            if (!Path.IsPathRooted(logPath))
            {
                logPath = Path.Combine(basePath, logPath);
            }
            return logPath;
        }

        static bool IsDebugEnabled()
        {
#if GRAPHICS_TEST_FRAMEWORK_DEBUG
            return true;
#else
            if (Environment.GetEnvironmentVariable(k_DebugEnvironmentVariable) == "1")
                return true;

            if (s_CommandLineReader.CommandLineArgumentExists(k_DebugCommandLineArg))
                return true;
            return false;
#endif
        }

        static void OnExit()
        {
#if UNITY_EDITOR
            EditorConnection.instance.UnregisterConnection(OnPlayerConnected);
            EditorConnection.instance.UnregisterDisconnection(OnPlayerDisconnected);
            EditorConnection.instance.Unregister(k_LogChannel, OnPlayerLogReceived);
#endif
            k_Cts.Cancel();
        }
    }
}
