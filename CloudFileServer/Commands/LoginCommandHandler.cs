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
    /// Command handler for login requests.
    /// Implements the Command pattern.
    /// </summary>
    public class LoginCommandHandler : ICommandHandler
    {
        private readonly AuthenticationService _authService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the LoginCommandHandler class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="logService">The logging service.</param>
        public LoginCommandHandler(AuthenticationService authService, LogService logService)
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
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.LOGIN_REQUEST;
        }

        /// <summary>
        /// Handles a login request packet.
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
                    _logService.Warning("Received login request with no payload");
                    return _packetFactory.CreateLoginResponse(false, "Invalid login request. No credentials provided.");
                }

                // Deserialize the payload to extract username and password
                var credentials = JsonSerializer.Deserialize<LoginCredentials>(packet.Payload);
                
                if (string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
                {
                    _logService.Warning("Received login request with missing credentials");
                    return _packetFactory.CreateLoginResponse(false, "Username and password are required.");
                }

                // Attempt to authenticate
                var user = await _authService.Authenticate(credentials.Username, credentials.Password);
                
                if (user != null)
                {
                    _logService.Info($"User authenticated successfully: {user.Username} (ID: {user.Id})");
                    
                    // Update the client session with the authenticated user
                    session.UserId = user.Id;
                    
                    return _packetFactory.CreateLoginResponse(true, "Authentication successful.", user.Id);
                }
                else
                {
                    _logService.Warning($"Failed login attempt for username: {credentials.Username}");
                    return _packetFactory.CreateLoginResponse(false, "Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing login request: {ex.Message}", ex);
                return _packetFactory.CreateLoginResponse(false, "An error occurred during login.");
            }
        }

        /// <summary>
        /// Class for deserializing login credentials from a login request.
        /// </summary>
        private class LoginCredentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}