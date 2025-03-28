using System;
using System.Threading.Tasks;
using CloudFileClient.Commands;
using CloudFileClient.FileTransfer;
using CloudFileClient.Models;
using CloudFileClient.Utils;

namespace CloudFileClient.State
{
    /// <summary>
    /// Represents the transfer state in the client state machine.
    /// In this state, the client is transferring a file (upload or download).
    /// </summary>
    public class TransferState : IClientSessionState
    {
        private readonly FileTransferManager _transferManager;
        private readonly FileMetadata _fileMetadata;
        private readonly bool _isUploading;
        private readonly LogService _logService;
        
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        public ClientSession ClientSession { get; }
        
        /// <summary>
        /// Gets a value indicating whether the transfer is an upload.
        /// </summary>
        public bool IsUploading => _isUploading;
        
        /// <summary>
        /// Gets a value indicating whether the transfer is a download.
        /// </summary>
        public bool IsDownloading => !_isUploading;
        
        /// <summary>
        /// Gets the file metadata for the transfer.
        /// </summary>
        public FileMetadata FileMetadata => _fileMetadata;

        /// <summary>
        /// Initializes a new instance of the TransferState class.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="transferManager">The file transfer manager.</param>
        /// <param name="fileMetadata">The file metadata for the transfer.</param>
        /// <param name="isUploading">Whether the transfer is an upload.</param>
        /// <param name="logService">The logging service.</param>
        public TransferState(
            ClientSession clientSession,
            FileTransferManager transferManager,
            FileMetadata fileMetadata,
            bool isUploading,
            LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _transferManager = transferManager ?? throw new ArgumentNullException(nameof(transferManager));
            _fileMetadata = fileMetadata ?? throw new ArgumentNullException(nameof(fileMetadata));
            _isUploading = isUploading;
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Handles a command in this state.
        /// Only transfer-related and limited other commands are allowed in this state.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public async Task<CommandResult> HandleCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Only allow transfer-related commands specific to the current transfer operation
            // and a few basic commands like checking transfer status
            
            // Determine if this command is allowed in the current transfer state
            bool isAllowedCommand = IsCommandAllowedInTransferState(command);
            
            if (!isAllowedCommand)
            {
                string transferType = _isUploading ? "upload" : "download";
                _logService.Warning($"Command '{command.CommandName}' not allowed during file {transferType}.");
                return new CommandResult($"Cannot execute this command during file {transferType}. " +
                                         $"Please wait for the {transferType} to complete or cancel it.");
            }

            try
            {
                // Record activity to prevent session timeout
                ClientSession.UserSession.RecordActivity();
                
                // Execute the command
                return await command.ExecuteAsync(ClientSession.Connection);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error executing command '{command.CommandName}': {ex.Message}", ex);
                return new CommandResult($"Error executing {command.CommandName}: {ex.Message}", exception: ex);
            }
        }

        /// <summary>
        /// Determines if a command is allowed in the current transfer state.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <returns>True if the command is allowed, otherwise false.</returns>
        private bool IsCommandAllowedInTransferState(ICommand command)
        {
            // This is a placeholder. In a real implementation, we would check against
            // specific command types that are allowed during transfer, such as:
            // - For uploads: FileUploadChunkCommand, FileUploadCompleteCommand, CancelTransferCommand
            // - For downloads: FileDownloadChunkCommand, FileDownloadCompleteCommand, CancelTransferCommand
            // - General commands: GetTransferStatusCommand
            
            // For now, we'll just return true for simplicity, assuming proper command factory
            // logic would only create appropriate commands for the current state
            return true;
        }

        /// <summary>
        /// Called when entering the transfer state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task OnEnter()
        {
            string transferType = _isUploading ? "upload" : "download";
            _logService.Info($"Entered transfer state for file {transferType}: {_fileMetadata.FileName}");
            
            // Start the transfer if it hasn't already started
            if (_isUploading)
            {
                // Start upload
                await _transferManager.StartUploadAsync(_fileMetadata);
            }
            else
            {
                // Start download
                await _transferManager.StartDownloadAsync(_fileMetadata);
            }
        }

        /// <summary>
        /// Called when exiting the transfer state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnExit()
        {
            string transferType = _isUploading ? "upload" : "download";
            _logService.Info($"Exiting transfer state for file {transferType}: {_fileMetadata.FileName}");
            
            // If the transfer is still in progress, we should cancel it
            if (!_transferManager.IsTransferComplete)
            {
                _transferManager.CancelTransfer();
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cancels the current transfer.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CancelTransferAsync()
        {
            _logService.Info($"Cancelling {(_isUploading ? "upload" : "download")} of {_fileMetadata.FileName}");
            
            // Cancel the transfer
            _transferManager.CancelTransfer();
            
            // Transition back to authenticated state
            await ClientSession.TransitionToState(
                ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));
        }
    }
}