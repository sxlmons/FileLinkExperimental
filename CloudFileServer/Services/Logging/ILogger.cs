namespace CloudFileServer.Services.Logging
{
    /// <summary>
    /// Log level enumeration for categorizing log messages.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// Interface for logging services.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        void Log(LogLevel level, string message);

        /// <summary>
        /// Logs a message with the specified log level and exception.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        void Log(LogLevel level, string message, Exception exception);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The log message</param>
        void Debug(string message);

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The log message</param>
        void Info(string message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The log message</param>
        void Warning(string message);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The log message</param>
        void Error(string message);

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        void Error(string message, Exception exception);

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The log message</param>
        void Fatal(string message);

        /// <summary>
        /// Logs a fatal message with an exception.
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        void Fatal(string message, Exception exception);
    }
}