using System.Threading.Tasks;
using CloudFileClient.Commands;
using CloudFileClient.Protocol;
using CloudFileClient.Authentication;

namespace CloudFileClient.State
{
    /// <summary>
    /// Interface for client session state in the State pattern.
    /// Each concrete state represents a different state of the client session.
    /// </summary>
    public interface IClientSessionState
    {
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        ClientSession ClientSession { get; }
        
        /// <summary>
        /// Handles a command in this state.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        Task<CommandResult> HandleCommand(ICommand command);
        
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