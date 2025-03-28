using System;

namespace CloudFileClient.Connection
{
    /// <summary>
    /// Provides data for the ConnectionStatusChanged event.
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a value indicating whether the client is connected.
        /// </summary>
        public bool IsConnected { get; }
        
        /// <summary>
        /// Gets the error message if the connection failed or was lost.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the exception that caused the error, if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Gets the time when the connection status changed.
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the ConnectionStatusEventArgs class.
        /// </summary>
        /// <param name="isConnected">Whether the client is connected.</param>
        /// <param name="errorMessage">The error message if connection failed or was lost.</param>
        /// <param name="exception">The exception that caused the error, if any.</param>
        public ConnectionStatusEventArgs(bool isConnected, string errorMessage = null, Exception exception = null)
        {
            IsConnected = isConnected;
            ErrorMessage = errorMessage;
            Exception = exception;
            Timestamp = DateTime.Now;
        }
    }
}