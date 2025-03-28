using System;

namespace CloudFileClient.Authentication
{
    /// <summary>
    /// Provides data for the SessionTimedOut event.
    /// </summary>
    public class SessionTimeoutEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the user ID of the timed out session.
        /// </summary>
        public string UserId { get; }
        
        /// <summary>
        /// Gets the username of the timed out session.
        /// </summary>
        public string Username { get; }
        
        /// <summary>
        /// Gets the time when the session timed out.
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets the duration of inactivity that caused the timeout.
        /// </summary>
        public TimeSpan InactiveDuration { get; }

        /// <summary>
        /// Initializes a new instance of the SessionTimeoutEventArgs class.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="username">The username.</param>
        /// <param name="inactiveDuration">The duration of inactivity that caused the timeout.</param>
        public SessionTimeoutEventArgs(string userId, string username, TimeSpan inactiveDuration)
        {
            UserId = userId;
            Username = username;
            Timestamp = DateTime.Now;
            InactiveDuration = inactiveDuration;
        }
    }
}