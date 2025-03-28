using System;
using System.Threading.Tasks;
using CloudFileClient.Commands;
using CloudFileClient.Commands.Auth;
using CloudFileClient.Utils;

namespace CloudFileClient.State
{
    /// <summary>
    /// Represents the authenticated state in the client state machine.
    /// In this state, the client is connected and the user is authenticated.
    /// </summary>
    public class AuthenticatedState : IClientSessionState
    {
        private readonly LogService _logService;
        
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        public ClientSession ClientSession { get; }

        /// <summary>
        /// Initializes a new instance of the AuthenticatedState class.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="logService">The logging service.</param>
        public AuthenticatedState(ClientSession clientSession, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Handles a command in this state.
        /// Most commands are allowed in this state except for login and create account.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public async Task<CommandResult> HandleCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Don't allow login or create account commands in this state
            if (command is LoginCommand)
            {
                _logService.Warning("Cannot execute login command while already authenticated.");
                return new CommandResult("Already logged in. Please logout first if you want to login as a different user.");
            }
            else if (command is CreateAccountCommand)
            {
                _logService.Warning("Cannot execute create account command while authenticated.");
                return new CommandResult("Already logged in. Please logout first if you want to create a new account.");
            }
            
            // For logout command, we'll handle the user session update after execution
            bool isLogoutCommand = command is LogoutCommand;

            try
            {
                // Record activity to prevent session timeout
                ClientSession.UserSession.RecordActivity();
                
                // Execute the command
                var result = await command.ExecuteAsync(ClientSession.Connection);
                
                // If the command is a logout command and it succeeded, logout the user
                if (isLogoutCommand && result.Success)
                {
                    ClientSession.UserSession.Logout();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error executing command '{command.CommandName}': {ex.Message}", ex);
                
                // Still logout if it was a logout command that failed
                if (isLogoutCommand)
                {
                    ClientSession.UserSession.Logout();
                }
                
                return new CommandResult($"Error executing {command.CommandName}: {ex.Message}", exception: ex);
            }
        }

        /// <summary>
        /// Called when entering the authenticated state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnEnter()
        {
            _logService.Info($"Entered authenticated state as '{ClientSession.UserSession.Username}'.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when exiting the authenticated state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnExit()
        {
            _logService.Info("Exiting authenticated state.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the success flag and error message if failed.</returns>
        public async Task<(bool Success, string ErrorMessage)> LogoutAsync()
        {
            try
            {
                // Create a logout command
                var command = new LogoutCommand(ClientSession.UserSession.UserId, _logService);
                
                // Execute the command
                var result = await HandleCommand(command);
                
                return (result.Success, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during logout: {ex.Message}", ex);
                
                // Even if there's an error, still log out locally
                ClientSession.UserSession.Logout();
                
                return (false, $"Logout failed: {ex.Message}");
            }
        }
    }
}