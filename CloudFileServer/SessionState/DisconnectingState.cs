using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Threading.Tasks;

namespace CloudFileServer.SessionState
{
    /// <summary>
    /// Represents the disconnecting state in the session state machine.
    /// In this state, the session is being cleaned up before disconnection.
    /// </summary>
    public class DisconnectingState : SessionState
    {
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        public ClientSession ClientSession { get; }

        /// <summary>
        /// Initializes a new instance of the DisconnectingState class.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="logService">The logging service.</param>
        public DisconnectingState(ClientSession clientSession, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Handles a packet received while in the disconnecting state.
        /// In this state, all packets are responded to with an error message.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        public Task<Packet> HandlePacket(Packet packet)
        {
            _logService.Debug($"Received packet in disconnecting state: {CloudFileServer.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)}");
            
            // Always respond with an error in this state
            var response = _packetFactory.CreateErrorResponse(
                packet.CommandCode,
                "Session is disconnecting.",
                ClientSession.UserId);
            
            return Task.FromResult(response);
        }

        /// <summary>
        /// Called when entering the disconnecting state.
        /// Performs cleanup operations before disconnection.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnEnter()
        {
            _logService.Debug($"Session {ClientSession.SessionId} entered DisconnectingState");
            
            // Clean up any resources
            // This could include cancelling pending operations, releasing locks, etc.
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when exiting the disconnecting state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnExit()
        {
            _logService.Debug($"Session {ClientSession.SessionId} exited DisconnectingState");
            return Task.CompletedTask;
        }
    }
}