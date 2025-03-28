using System;
using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Commands.Auth
{
    /// <summary>
    /// Command for creating a new user account.
    /// </summary>
    public class CreateAccountCommand : ICommand
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _email;
        private readonly LogService _logService;
        private readonly ClientPacketFactory _packetFactory;
        private readonly ResponseParser _responseParser;

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName => "CreateAccount";

        /// <summary>
        /// Initializes a new instance of the CreateAccountCommand class.
        /// </summary>
        /// <param name="username">The username for the new account.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="email">The email address for the new account.</param>
        /// <param name="logService">The logging service.</param>
        public CreateAccountCommand(string username, string password, string email, LogService logService)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));
            
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            
            _username = username;
            _password = password;
            _email = email ?? string.Empty;
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
            return _packetFactory.CreateAccountCreationRequest(_username, _password, _email);
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
                _logService.Info($"Creating account for '{_username}'...");
                
                // Create the account creation packet
                var packet = CreatePacket();
                
                // Send the packet and get the response
                var response = await connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var (success, userId, message) = _responseParser.ParseAccountCreationResponse(response);
                
                if (success)
                {
                    _logService.Info($"Account created successfully for '{_username}' (User ID: {userId})");
                    
                    // Return success result with user ID
                    return new CommandResult(response, new
                    {
                        UserId = userId,
                        Username = _username
                    });
                }
                else
                {
                    _logService.Warning($"Account creation failed for '{_username}': {message}");
                    
                    // Return failure result
                    return new CommandResult(message, response);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating account: {ex.Message}", ex);
                return new CommandResult($"Account creation failed: {ex.Message}", exception: ex);
            }
        }
    }
}