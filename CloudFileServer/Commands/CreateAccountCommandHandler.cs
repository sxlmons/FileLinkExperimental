using CloudFileServer.Authentication;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Command handler for account creation requests.
    /// Implements the Command pattern.
    /// </summary>
    public class CreateAccountCommandHandler : ICommandHandler
    {
        private readonly AuthenticationService _authService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the CreateAccountCommandHandler class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="logService">The logging service.</param>
        public CreateAccountCommandHandler(AuthenticationService authService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Determines whether this handler can process the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code to check.</param>
        /// <returns>True if this handler can process the command code, otherwise false.</returns>
        public bool CanHandle(int commandCode)
        {
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.CREATE_ACCOUNT_REQUEST;
        }

        /// <summary>
        /// Handles an account creation request packet.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        /// <param name="session">The client session that sent the packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning("Received account creation request with no payload");
                    return _packetFactory.CreateAccountCreationResponse(false, "Invalid account creation request. No information provided.");
                }

                // Deserialize the payload to extract user information
                var accountInfo = JsonSerializer.Deserialize<AccountCreationInfo>(packet.Payload);
                
                if (string.IsNullOrEmpty(accountInfo.Username) || string.IsNullOrEmpty(accountInfo.Password))
                {
                    _logService.Warning("Received account creation request with missing required fields");
                    return _packetFactory.CreateAccountCreationResponse(false, "Username and password are required.");
                }

                // Attempt to create the account
                var user = await _authService.RegisterUser(accountInfo.Username, accountInfo.Password, "User", accountInfo.Email);
                
                if (user != null)
                {
                    _logService.Info($"Account created successfully: {user.Username} (ID: {user.Id})");
                    return _packetFactory.CreateAccountCreationResponse(true, "Account created successfully.", user.Id);
                }
                else
                {
                    _logService.Warning($"Failed to create account for username: {accountInfo.Username}");
                    return _packetFactory.CreateAccountCreationResponse(false, "Failed to create account. Username may already be taken.");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing account creation request: {ex.Message}", ex);
                return _packetFactory.CreateAccountCreationResponse(false, "An error occurred during account creation.");
            }
        }

        /// <summary>
        /// Class for deserializing account creation information from an account creation request.
        /// </summary>
        private class AccountCreationInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
        }
    }
}