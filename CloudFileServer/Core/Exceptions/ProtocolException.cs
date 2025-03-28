namespace CloudFileServer.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when there's an error in protocol serialization or deserialization.
    /// </summary>
    public class ProtocolException : CloudFileServerException
    {
        /// <summary>
        /// Initializes a new instance of the ProtocolException class.
        /// </summary>
        public ProtocolException() : base() { }

        /// <summary>
        /// Initializes a new instance of the ProtocolException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ProtocolException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the ProtocolException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ProtocolException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}