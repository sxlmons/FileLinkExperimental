using CloudFileServer.Network;
using CloudFileServer.Protocol;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Interface for command handlers in the Command pattern.
    /// Each command handler is responsible for processing a specific type of packet.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Determines whether this handler can process the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code to check.</param>
        /// <returns>True if this handler can process the command code, otherwise false.</returns>
        bool CanHandle(int commandCode);

        /// <summary>
        /// Handles the processing of a packet.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        /// <param name="session">The client session that sent the packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        Task<Packet> Handle(Packet packet, ClientSession session);
    }
}