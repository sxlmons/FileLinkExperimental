using CloudFileServer.Authentication;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Command handler for logout requests.
    /// Implements the Command pattern.
    /// </summary>
    public class LogoutCommandHandler : ICommandHandler
    {
        private readonly AuthenticationService _authService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the LogoutCommandHandler class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="logService">The logging service.</param>
        public LogoutCommandHandler(AuthenticationService authService, LogService logService)
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
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.LOGOUT_REQUEST;
        }

        /// <summary>
        /// Handles a logout request packet.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        /// <param name="session">The client session that sent the packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received logout request from unauthenticated session");
                    return _packetFactory.CreateLogoutResponse(false, "You are not logged in.");
                }

                _logService.Info($"User {session.UserId} is logging out");
                
                // Create response before clearing user ID
                var response = _packetFactory.CreateLogoutResponse(true, "Logout successful.");
                
                // Schedule disconnect after sending response
                _ = Task.Run(async () => 
                {
                    await Task.Delay(1000); // Give time for the response to be sent
                    await session.Disconnect("User logged out");
                });
                
                return response;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing logout request: {ex.Message}", ex);
                return _packetFactory.CreateLogoutResponse(false, "An error occurred during logout.");
            }
        }
    }
}