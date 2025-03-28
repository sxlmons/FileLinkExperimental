namespace CloudFileServer.Services.Logging
{
    /// <summary>
    /// Implements the ILogger interface to log messages to a file.
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly object _lockObj = new object();

        /// <summary>
        /// Initializes a new instance of the FileLogger class.
        /// </summary>
        /// <param name="logFilePath">The path to the log file</param>
        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
            
            // Ensure the directory exists
            string directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create or clear the log file
            File.WriteAllText(_logFilePath, $"Log started at {DateTime.Now}\n");
        }

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        public void Log(LogLevel level, string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            
            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    
                    // Also print to console for debugging purposes
                    Console.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    // If we can't log to file, at least try to output to console
                    Console.WriteLine($"Error logging to file: {ex.Message}");
                    Console.WriteLine(logEntry);
                }
            }
        }

        /// <summary>
        /// Logs a message with the specified log level and exception.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        public void Log(LogLevel level, string message, Exception exception)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            string exceptionDetails = $"Exception: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}";
            
            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine + exceptionDetails + Environment.NewLine);
                    
                    // Also print to console for debugging purposes
                    Console.WriteLine(logEntry);
                    Console.WriteLine(exceptionDetails);
                }
                catch (Exception ex)
                {
                    // If we can't log to file, at least try to output to console
                    Console.WriteLine($"Error logging to file: {ex.Message}");
                    Console.WriteLine(logEntry);
                    Console.WriteLine(exceptionDetails);
                }
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The log message</param>
        public void Debug(string message) => Log(LogLevel.Debug, message);

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The log message</param>
        public void Info(string message) => Log(LogLevel.Info, message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The log message</param>
        public void Warning(string message) => Log(LogLevel.Warning, message);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The log message</param>
        public void Error(string message) => Log(LogLevel.Error, message);

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        public void Error(string message, Exception exception) => Log(LogLevel.Error, message, exception);

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The log message</param>
        public void Fatal(string message) => Log(LogLevel.Fatal, message);

        /// <summary>
        /// Logs a fatal message with an exception.
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        public void Fatal(string message, Exception exception) => Log(LogLevel.Fatal, message, exception);
    }
}