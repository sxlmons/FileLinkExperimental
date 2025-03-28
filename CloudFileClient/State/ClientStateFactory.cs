using System;
using CloudFileClient.Authentication;
using CloudFileClient.FileTransfer;
using CloudFileClient.Models;
using CloudFileClient.Utils;

namespace CloudFileClient.State
{
    /// <summary>
    /// Factory for creating client session state objects.
    /// Implements the Factory pattern.
    /// </summary>
    public class ClientStateFactory
    {
        private readonly LogService _logService;
        private readonly ClientAuthenticationService _authService;
        
        /// <summary>
        /// Initializes a new instance of the ClientStateFactory class.
        /// </summary>
        /// <param name="logService">The logging service.</param>
        /// <param name="authService">The authentication service.</param>
        public ClientStateFactory(LogService logService, ClientAuthenticationService authService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// Creates a new disconnected state.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <returns>The disconnected state.</returns>
        public IClientSessionState CreateDisconnectedState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
            
            return new DisconnectedState(clientSession, _logService);
        }

        /// <summary>
        /// Creates a new authentication required state.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <returns>The authentication required state.</returns>
        public IClientSessionState CreateAuthRequiredState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
            
            return new AuthRequiredState(clientSession, _authService, _logService);
        }

        /// <summary>
        /// Creates a new authenticated state.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <returns>The authenticated state.</returns>
        public IClientSessionState CreateAuthenticatedState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
            
            return new AuthenticatedState(clientSession, _logService);
        }

        /// <summary>
        /// Creates a new transfer state.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="transferManager">The transfer manager for the transfer.</param>
        /// <param name="fileMetadata">The file metadata for the transfer.</param>
        /// <param name="isUploading">Whether the transfer is an upload.</param>
        /// <returns>The transfer state.</returns>
        public IClientSessionState CreateTransferState(
            ClientSession clientSession, 
            FileTransferManager transferManager,
            FileMetadata fileMetadata, 
            bool isUploading)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
            
            if (transferManager == null)
                throw new ArgumentNullException(nameof(transferManager));
            
            if (fileMetadata == null)
                throw new ArgumentNullException(nameof(fileMetadata));
            
            return new TransferState(clientSession, transferManager, fileMetadata, isUploading, _logService);
        }
    }
}