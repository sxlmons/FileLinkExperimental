using System;
using CloudFileClient.Protocol;

namespace CloudFileClient.Commands
{
    /// <summary>
    /// Represents the result of a command execution.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Gets a value indicating whether the command was successful.
        /// </summary>
        public bool Success { get; }
        
        /// <summary>
        /// Gets the error message if the command failed.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the response packet from the server.
        /// </summary>
        public Packet ResponsePacket { get; }
        
        /// <summary>
        /// Gets the result data if the command was successful.
        /// This could be a string, a collection, or any other type.
        /// </summary>
        public object Data { get; }
        
        /// <summary>
        /// Gets the exception that caused the command to fail, if any.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the CommandResult class for a successful command.
        /// </summary>
        /// <param name="responsePacket">The response packet from the server.</param>
        /// <param name="data">The result data.</param>
        public CommandResult(Packet responsePacket, object data = null)
        {
            Success = true;
            ResponsePacket = responsePacket;
            Data = data;
            ErrorMessage = null;
            Exception = null;
        }

        /// <summary>
        /// Initializes a new instance of the CommandResult class for a failed command.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="responsePacket">The response packet from the server, if any.</param>
        /// <param name="exception">The exception that caused the command to fail, if any.</param>
        public CommandResult(string errorMessage, Packet responsePacket = null, Exception exception = null)
        {
            Success = false;
            ErrorMessage = errorMessage;
            ResponsePacket = responsePacket;
            Data = null;
            Exception = exception;
        }

        /// <summary>
        /// Gets the result data cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast to.</typeparam>
        /// <returns>The data cast to the specified type, or default(T) if the data is null or cannot be cast.</returns>
        public T GetData<T>()
        {
            if (Data is T typedData)
            {
                return typedData;
            }
            
            return default;
        }
    }
}