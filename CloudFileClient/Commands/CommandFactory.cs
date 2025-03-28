using System;
using CloudFileClient.Commands.Auth;
using CloudFileClient.Utils;

namespace CloudFileClient.Commands
{
    /// <summary>
    /// Factory for creating command objects.
    /// Implements the Factory pattern.
    /// </summary>
    public class CommandFactory
    {
        private readonly LogService _logService;
        
        /// <summary>
        /// Initializes a new instance of the CommandFactory class.
        /// </summary>
        /// <param name="logService">The logging service.</param>
        public CommandFactory(LogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Creates a login command.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The login command.</returns>
        public ICommand CreateLoginCommand(string username, string password)
        {
            return new LoginCommand(username, password, _logService);
        }

        /// <summary>
        /// Creates a logout command.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The logout command.</returns>
        public ICommand CreateLogoutCommand(string userId)
        {
            return new LogoutCommand(userId, _logService);
        }

        /// <summary>
        /// Creates a create account command.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="email">The email address.</param>
        /// <returns>The create account command.</returns>
        public ICommand CreateCreateAccountCommand(string username, string password, string email = "")
        {
            return new CreateAccountCommand(username, password, email, _logService);
        }

        // Additional factory methods for other commands will be added in subsequent phases
        // as we implement file and directory operations, transfers, etc.
    }
}