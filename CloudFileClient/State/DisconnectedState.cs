using System;
using System.Threading.Tasks;
using CloudFileClient.Commands;
using CloudFileClient.Connection;
using CloudFileClient.Utils;

namespace CloudFileClient.State
{
    /// <summary>
    /// Represents the disconnected state in the client state machine.
    /// In this state, the client is not connected to the server.
    /// </summary>
    public class DisconnectedState : IClientSessionState
    {
        private readonly LogService _logService;
        
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        public ClientSession ClientSession { get; }

        /// <summary>
        /// Initializes a new instance of the DisconnectedState class.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="logService">The logging service.</param>
        public DisconnectedState(ClientSession clientSession, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Handles a command in this state.
        /// Most commands will fail in the disconnected state.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public Task<CommandResult> HandleCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            
            _logService.Warning($"Cannot execute command '{command.CommandName}' in disconnected state.");
            
            // All commands fail in disconnected state
            return Task.FromResult(new CommandResult("Not connected to server. Please connect first."));
        }

        /// <summary>
        /// Called when entering the disconnected state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnEnter()
        {
            _logService.Info("Entered disconnected state.");
            
            // Log the user out if they were logged in
            if (ClientSession.UserSession.IsAuthenticated)
            {
                ClientSession.UserSession.Logout();
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when exiting the disconnected state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnExit()
        {
            _logService.Info("Exiting disconnected state.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to connect to the server.
        /// </summary>
        /// <param name="host">The server host.</param>
        /// <param name="port">The server port.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the success flag and error message if failed.</returns>
        public async Task<(bool Success, string ErrorMessage)> ConnectAsync(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                return (false, "Host cannot be empty.");
            
            if (port <= 0 || port > 65535)
                return (false, "Invalid port number.");

            try
            {
                _logService.Info($"Connecting to server {host}:{port}...");
                
                // Attempt to connect
                await ClientSession.Connection.ConnectAsync(host, port);
                
                if (ClientSession.Connection.IsConnected)
                {
                    _logService.Info($"Connected to server {host}:{port}");
                    
                    // Transition to auth required state
                    await ClientSession.TransitionToState(
                        ClientSession.StateFactory.CreateAuthRequiredState(ClientSession));
                    
                    return (true, null);
                }
                else
                {
                    string error = "Failed to connect to server.";
                    _logService.Error(error);
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                string error = $"Error connecting to server: {ex.Message}";
                _logService.Error(error, ex);
                return (false, error);
            }
        }
    }
}