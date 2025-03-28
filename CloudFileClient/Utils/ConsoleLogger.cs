using System;

namespace CloudFileClient.Utils
{
    /// <summary>
    /// Implements the ILogger interface to log messages to the console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        public void Log(LogLevel level, string message)
        {
            Console.ForegroundColor = GetColorForLogLevel(level);
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs a message with the specified log level and exception.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        /// <param name="exception">The exception to log</param>
        public void Log(LogLevel level, string message, Exception exception)
        {
            Console.ForegroundColor = GetColorForLogLevel(level);
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
            Console.WriteLine($"Exception: {exception.GetType().Name}: {exception.Message}");
            Console.WriteLine($"StackTrace: {exception.StackTrace}");
            Console.ResetColor();
        }

        /// <summary>
        /// Gets the console color for the specified log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>The console color</returns>
        private ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
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