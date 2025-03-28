using System;
using System.Collections.Generic;
using System.Linq;
using CloudFileClient.Protocol;

namespace CloudFileClient.Utils
{
    /// <summary>
    /// Centralized logging service that can log to multiple loggers.
    /// Also provides packet logging functionality.
    /// </summary>
    public class LogService : ILogger
    {
        private readonly List<ILogger> _loggers = new List<ILogger>();

        /// <summary>
        /// Initializes a new instance of the LogService class.
        /// </summary>
        /// <param name="loggers">The loggers to use</param>
        public LogService(params ILogger[] loggers)
        {
            _loggers.AddRange(loggers);
        }

        /// <summary>
        /// Adds a logger to the service.
        /// </summary>
        /// <param name="logger">The logger to add</param>
        public void AddLogger(ILogger logger)
        {
            _loggers.Add(logger);
        }

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The log message</param>
        public void Log(LogLevel level, string message)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(level, message);
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
            foreach (var logger in _loggers)
            {
                logger.Log(level, message, exception);
            }
        }

        /// <summary>
        /// Logs a packet being sent or received.
        /// </summary>
        /// <param name="packet">The packet to log</param>
        /// <param name="isSending">True if sending, false if receiving</param>
        public void LogPacket(Packet packet, bool isSending)
        {
            string direction = isSending ? "SENT" : "RECEIVED";
            string commandName = Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode);
            
            // Determine payload description
            string payloadDesc = packet.Payload == null || packet.Payload.Length == 0
                ? "no payload"
                : $"payload size: {packet.Payload.Length} bytes";
            
            // Build metadata string
            string metadata = string.Join(", ", packet.Metadata.Select(kv => $"{kv.Key}={kv.Value}"));
            metadata = string.IsNullOrEmpty(metadata) ? "no metadata" : metadata;
            
            // Log basic packet info at Info level
            Log(LogLevel.Info, $"[{direction}] Command: {commandName}, {payloadDesc}");
            
            // Log detailed metadata at Debug level
            Log(LogLevel.Debug, $"[{direction}] Command: {commandName}, Metadata: {metadata}");
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