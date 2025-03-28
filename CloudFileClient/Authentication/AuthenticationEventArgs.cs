using System;

namespace CloudFileClient.Authentication
{
    /// <summary>
    /// Provides data for the AuthenticationChanged event.
    /// </summary>
    public class AuthenticationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; }
        
        /// <summary>
        /// Gets the user ID if authenticated, otherwise empty.
        /// </summary>
        public string UserId { get; }
        
        /// <summary>
        /// Gets the username if authenticated, otherwise empty.
        /// </summary>
        public string Username { get; }
        
        /// <summary>
        /// Gets an error message if authentication failed.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the time when the authentication status changed.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the AuthenticationEventArgs class for successful authentication.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="username">The username.</param>
        public AuthenticationEventArgs(string userId, string username)
        {
            IsAuthenticated = true;
            UserId = userId;
            Username = username;
            ErrorMessage = null;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationEventArgs class for logout or failed authentication.
        /// </summary>
        /// <param name="errorMessage">The error message if authentication failed, or null for logout.</param>
        public AuthenticationEventArgs(string errorMessage = null)
        {
            IsAuthenticated = false;
            UserId = string.Empty;
            Username = string.Empty;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }
}