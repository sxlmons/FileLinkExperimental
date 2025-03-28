using System;
using CloudFileClient.Utils;

namespace CloudFileClient.Authentication
{
    /// <summary>
    /// Manages the user's authentication state and session.
    /// </summary>
    public class UserSession
    {
        private readonly LogService _logService;
        private DateTime _lastActivityTime;

        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; private set; }

        /// <summary>
        /// Gets the ID of the authenticated user.
        /// </summary>
        public string UserId { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the username of the authenticated user.
        /// </summary>
        public string Username { get; private set; } = string.Empty;

        /// <summary>
        /// Event raised when the authentication status changes.
        /// </summary>
        public event EventHandler<AuthenticationEventArgs> AuthenticationChanged;

        /// <summary>
        /// Event raised when the session times out.
        /// </summary>
        public event EventHandler<SessionTimeoutEventArgs> SessionTimedOut;

        /// <summary>
        /// Initializes a new instance of the UserSession class.
        /// </summary>
        /// <param name="logService">The logging service.</param>
        public UserSession(LogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// Authenticates the user with the specified credentials.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="username">The username.</param>
        public void Authenticate(string userId, string username)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));

            // Update authentication state
            IsAuthenticated = true;
            UserId = userId;
            Username = username;
            _lastActivityTime = DateTime.Now;

            _logService.Info($"User authenticated: {username} (ID: {userId})");

            // Raise the authentication changed event
            OnAuthenticationChanged(new AuthenticationEventArgs(userId, username));
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        public void Logout()
        {
            if (!IsAuthenticated)
                return;

            string oldUsername = Username;
            string oldUserId = UserId;

            // Clear authentication state
            IsAuthenticated = false;
            UserId = string.Empty;
            Username = string.Empty;

            _logService.Info($"User logged out: {oldUsername} (ID: {oldUserId})");

            // Raise the authentication changed event
            OnAuthenticationChanged(new AuthenticationEventArgs());
        }

        /// <summary>
        /// Records user activity to prevent session timeout.
        /// </summary>
        public void RecordActivity()
        {
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// Checks if the session has timed out.
        /// </summary>
        /// <param name="timeoutMinutes">The session timeout period in minutes.</param>
        /// <returns>True if the session has timed out, otherwise false.</returns>
        public bool CheckSessionTimeout(int timeoutMinutes)
        {
            if (!IsAuthenticated)
                return false;

            TimeSpan inactiveDuration = DateTime.Now - _lastActivityTime;
            
            if (inactiveDuration.TotalMinutes > timeoutMinutes)
            {
                _logService.Warning($"Session timed out after {inactiveDuration.TotalMinutes:F1} minutes of inactivity");
                
                // Capture user info before logout
                string oldUserId = UserId;
                string oldUsername = Username;
                
                // Log the user out
                Logout();
                
                // Raise the session timed out event
                OnSessionTimedOut(new SessionTimeoutEventArgs(oldUserId, oldUsername, inactiveDuration));
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Raises the AuthenticationChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnAuthenticationChanged(AuthenticationEventArgs e)
        {
            AuthenticationChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the SessionTimedOut event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSessionTimedOut(SessionTimeoutEventArgs e)
        {
            SessionTimedOut?.Invoke(this, e);
        }
    }
}