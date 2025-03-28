using CloudFileServer.Network;
using CloudFileServer.Protocol;
using System.Threading.Tasks;

namespace CloudFileServer.SessionState
{
    /// <summary>
    /// Interface for session state in the State pattern.
    /// Each concrete state represents a different state of a client session.
    /// </summary>
    public interface SessionState
    {
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        ClientSession ClientSession { get; }

        /// <summary>
        /// Handles a packet received while in this state.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        Task<Packet> HandlePacket(Packet packet);

        /// <summary>
        /// Called when entering this state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task OnEnter();

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task OnExit();
    }
}