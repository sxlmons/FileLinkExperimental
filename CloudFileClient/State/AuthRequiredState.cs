using System;
using System.Threading.Tasks;
using CloudFileClient.Authentication;
using CloudFileClient.Commands;
using CloudFileClient.Commands.Auth;
using CloudFileClient.Utils;

namespace CloudFileClient.State
{
    /// <summary>
    /// Represents the authentication-required state in the client state machine.
    /// In this state, the client is connected but the user is not authenticated.
    /// </summary>
    public class AuthRequiredState : IClientSessionState
    {
        private readonly ClientAuthenticationService _authService;
        private readonly LogService _logService;
        private int _failedLoginAttempts = 0;
        private const int MaxFailedLoginAttempts = 5;
        
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        public ClientSession ClientSession { get; }

        /// <summary>
        /// Initializes a new instance of the AuthRequiredState class.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="authService">The authentication service.</param>
        /// <param name="logService">The logging service.</param>
        public AuthRequiredState(
            ClientSession clientSession,
            ClientAuthenticationService authService,
            LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Handles a command in this state.
        /// Only authentication commands are allowed in this state.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public async Task<CommandResult> HandleCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Only allow login and create account commands in this state
            if (command is LoginCommand || command is CreateAccountCommand)
            {
                try
                {
                    // Execute the command
                    var result = await command.ExecuteAsync(ClientSession.Connection);
                    
                    // If the command succeeded and it's a login command, update the user session
                    if (result.Success && command is LoginCommand)
                    {
                        var data = result.GetData<dynamic>();
                        if (data != null)
                        {
                            // Authenticate the user
                            ClientSession.UserSession.Authenticate(data.UserId, data.Username);
                            
                            // Reset failed login attempts
                            _failedLoginAttempts = 0;
                        }
                    }
                    else if (!result.Success && command is LoginCommand)
                    {
                        // Increment failed login attempts
                        _failedLoginAttempts++;
                        
                        if (_failedLoginAttempts >= MaxFailedLoginAttempts)
                        {
                            _logService.Warning($"Maximum failed login attempts reached ({MaxFailedLoginAttempts}).");
                            
                            // Reset failed login attempts
                            _failedLoginAttempts = 0;
                            
                            // Add a note to the result
                            return new CommandResult(
                                $"{result.ErrorMessage}\nMaximum login attempts reached. Please try again later.",
                                result.ResponsePacket,
                                result.Exception);
                        }
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    _logService.Error($"Error executing command '{command.CommandName}': {ex.Message}", ex);
                    return new CommandResult($"Error executing {command.CommandName}: {ex.Message}", exception: ex);
                }
            }
            else
            {
                _logService.Warning($"Command '{command.CommandName}' not allowed in authentication-required state.");
                return new CommandResult("Authentication required. Please login or create an account first.");
            }
        }

        /// <summary>
        /// Called when entering the authentication-required state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnEnter()
        {
            _logService.Info("Entered authentication-required state.");
            _failedLoginAttempts = 0;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when exiting the authentication-required state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnExit()
        {
            _logService.Info("Exiting authentication-required state.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to login with the specified credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the success flag and error message if failed.</returns>
        public async Task<(bool Success, string ErrorMessage)> LoginAsync(string username, string password)
        {
            try
            {
                // Create a login command
                var command = new LoginCommand(username, password, _logService);
                
                // Execute the command
                var result = await HandleCommand(command);
                
                return (result.Success, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during login: {ex.Message}", ex);
                return (false, $"Login failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new account.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="email">The email address.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the success flag, user ID, and error message if failed.</returns>
        public async Task<(bool Success, string UserId, string ErrorMessage)> CreateAccountAsync(
            string username, string password, string email = "")
        {
            try
            {
                // Create a create account command
                var command = new CreateAccountCommand(username, password, email, _logService);
                
                // Execute the command
                var result = await HandleCommand(command);
                
                if (result.Success)
                {
                    var data = result.GetData<dynamic>();
                    string userId = data?.UserId;
                    return (true, userId, null);
                }
                else
                {
                    return (false, null, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating account: {ex.Message}", ex);
                return (false, null, $"Account creation failed: {ex.Message}");
            }
        }
    }
}