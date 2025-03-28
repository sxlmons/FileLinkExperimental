using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Protocol;

namespace CloudFileClient.Commands
{
    /// <summary>
    /// Interface for client commands in the Command pattern.
    /// Each command encapsulates a specific operation.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        string CommandName { get; }
        
        /// <summary>
        /// Creates a packet for this command.
        /// </summary>
        /// <returns>The packet to send to the server.</returns>
        Packet CreatePacket();
        
        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="connection">The client connection to use.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        Task<CommandResult> ExecuteAsync(ClientConnection connection);
    }
}