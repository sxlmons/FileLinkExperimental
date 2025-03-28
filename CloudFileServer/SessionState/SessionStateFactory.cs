using CloudFileServer.Authentication;
using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Services.Logging;
using System;

namespace CloudFileServer.SessionState
{
    /// <summary>
    /// Factory for creating session states.
    /// Implements the Factory pattern to create different states for client sessions.
    /// </summary>
    public class SessionStateFactory
    {
        private readonly AuthenticationService _authService;
        private readonly FileService _fileService;
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;

        /// <summary>
        /// Initializes a new instance of the SessionStateFactory class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="fileService">The file service.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="logService">The logging service.</param>
        public SessionStateFactory(
            AuthenticationService authService, 
            FileService fileService, 
            DirectoryService directoryService,
            LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Creates a new authentication-required state for a client session.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <returns>The authentication-required state.</returns>
        public SessionState CreateAuthRequiredState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));

            return new AuthRequiredState(clientSession, _authService, _logService);
        }

        /// <summary>
        /// Creates a new authenticated state for a client session.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <returns>The authenticated state.</returns>
        public SessionState CreateAuthenticatedState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));

            return new AuthenticatedState(clientSession, _fileService, _directoryService, _logService);
        }

        /// <summary>
        /// Creates a new transfer state for a client session.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="fileMetadata">The file metadata for the transfer.</param>
        /// <param name="isUploading">Indicates whether the transfer is an upload or download.</param>
        /// <returns>The transfer state.</returns>
        public SessionState CreateTransferState(ClientSession clientSession, FileMetadata fileMetadata, bool isUploading)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
            if (fileMetadata == null)
                throw new ArgumentNullException(nameof(fileMetadata));

            return new TransferState(clientSession, _fileService, fileMetadata, isUploading, _logService);
        }

        /// <summary>
        /// Creates a new disconnecting state for a client session.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <returns>The disconnecting state.</returns>
        public SessionState CreateDisconnectingState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));

            return new DisconnectingState(clientSession, _logService);
        }
    }
}