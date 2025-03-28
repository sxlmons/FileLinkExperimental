using System;
using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Commands.Auth
{
    /// <summary>
    /// Command for logging out a user.
    /// </summary>
    public class LogoutCommand : ICommand
    {
        private readonly string _userId;
        private readonly LogService _logService;
        private readonly ClientPacketFactory _packetFactory;
        private readonly ResponseParser _responseParser;

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName => "Logout";

        /// <summary>
        /// Initializes a new instance of the LogoutCommand class.
        /// </summary>
        /// <param name="userId">The user ID to log out.</param>
        /// <param name="logService">The logging service.</param>
        public LogoutCommand(string userId, LogService logService)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            _userId = userId;
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            _packetFactory = new ClientPacketFactory();
            _responseParser = new ResponseParser();
        }

        /// <summary>
        /// Creates a packet for this command.
        /// </summary>
        /// <returns>The packet to send to the server.</returns>
        public Packet CreatePacket()
        {
            return _packetFactory.CreateLogoutRequest(_userId);
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="connection">The client connection to use.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public async Task<CommandResult> ExecuteAsync(ClientConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            if (!connection.IsConnected)
                return new CommandResult("Not connected to server.");

            try
            {
                _logService.Info("Logging out...");
                
                // Create the logout packet
                var packet = CreatePacket();
                
                // Send the packet and get the response
                var response = await connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var (success, message) = _responseParser.ParseBasicResponse(response);
                
                if (success)
                {
                    _logService.Info("Logout successful");
                    
                    // Return success result
                    return new CommandResult(response);
                }
                else
                {
                    _logService.Warning($"Logout failed: {message}");
                    
                    // Return failure result
                    return new CommandResult(message, response);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during logout: {ex.Message}", ex);
                return new CommandResult($"Logout failed: {ex.Message}", exception: ex);
            }
        }
    }
}