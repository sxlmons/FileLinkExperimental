using CloudFileServer.Commands;
using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServer.SessionState
{
    /// <summary>
    /// Represents the authenticated state in the session state machine.
    /// In this state, the client can perform general file and directory operations.
    /// </summary>
    public class AuthenticatedState : SessionState
    {
        private readonly FileService _fileService;
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        private DateTime _lastActivityTime;
        
        /// <summary>
        /// Gets the client session this state is associated with.
        /// </summary>
        public ClientSession ClientSession { get; }

        /// <summary>
        /// Initializes a new instance of the AuthenticatedState class.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        /// <param name="fileService">The file service.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="logService">The logging service.</param>
        public AuthenticatedState(
            ClientSession clientSession, 
            FileService fileService, 
            DirectoryService directoryService, 
            LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// Handles a packet received while in the authenticated state.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        public async Task<Packet> HandlePacket(Packet packet)
        {
            // Update last activity time to prevent session timeout
            _lastActivityTime = DateTime.Now;

            // Verify the user ID in the packet matches the session's user ID
            if (!string.IsNullOrEmpty(ClientSession.UserId) && 
                !string.IsNullOrEmpty(packet.UserId) && 
                packet.UserId != ClientSession.UserId)
            {
                _logService.Warning($"User ID mismatch in packet: {packet.UserId} vs session: {ClientSession.UserId}");
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "User ID in packet does not match the authenticated user.",
                    ClientSession.UserId);
            }

            try
            {
                // Use the command handler factory to get the appropriate handler
                var handler = ClientSession.CommandHandlerFactory.CreateHandler(packet.CommandCode);
                
                if (handler != null)
                {
                    return await handler.Handle(packet, ClientSession);
                }

                // If no specific handler was found, fallback to default handling based on command codes
                switch (packet.CommandCode)
                {
                    case CloudFileServer.Protocol.Commands.CommandCode.LOGOUT_REQUEST:
                        return await HandleLogoutRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_LIST_REQUEST:
                        return await HandleFileListRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST:
                        return await HandleFileUploadInitRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST:
                        return await HandleFileDownloadInitRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_DELETE_REQUEST:
                        return await HandleFileDeleteRequest(packet);
                    
                    // Directory operation commands
                    case CloudFileServer.Protocol.Commands.CommandCode.DIRECTORY_CREATE_REQUEST:
                        return await HandleDirectoryCreateRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.DIRECTORY_LIST_REQUEST:
                        return await HandleDirectoryListRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.DIRECTORY_RENAME_REQUEST:
                        return await HandleDirectoryRenameRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.DIRECTORY_DELETE_REQUEST:
                        return await HandleDirectoryDeleteRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_MOVE_REQUEST:
                        return await HandleFileMoveRequest(packet);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST:
                        return await HandleDirectoryContentsRequest(packet);
                        
                    default:
                        _logService.Warning($"Received unexpected command in AuthenticatedState: {CloudFileServer.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)}");
                        return _packetFactory.CreateErrorResponse(
                            packet.CommandCode,
                            $"Command {CloudFileServer.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)} not supported in authenticated state.",
                            ClientSession.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling packet in AuthenticatedState: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while processing your request.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a logout request.
        /// </summary>
        /// <param name="packet">The logout request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleLogoutRequest(Packet packet)
        {
            _logService.Info($"User {ClientSession.UserId} is logging out");
            
            // Create response before transitioning states
            var response = _packetFactory.CreateLogoutResponse(true, "Logout successful.");
            
            // Transition to the disconnecting state
            ClientSession.TransitionToState(ClientSession.StateFactory.CreateDisconnectingState(ClientSession));
            
            // Schedule disconnect after sending response
            _ = Task.Run(async () => 
            {
                await Task.Delay(1000); // Give time for the response to be sent
                await ClientSession.Disconnect("User logged out");
            });
            
            return response;
        }

        /// <summary>
        /// Handles a file list request.
        /// </summary>
        /// <param name="packet">The file list request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileListRequest(Packet packet)
        {
            _logService.Debug($"Fetching file list for user {ClientSession.UserId}");
            
            try
            {
                // Get the list of files for the user
                var files = await _fileService.GetUserFiles(ClientSession.UserId);
                
                // Create and return the response
                return _packetFactory.CreateFileListResponse(files, ClientSession.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting file list: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "Error retrieving file list: " + ex.Message,
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a file upload initialization request.
        /// </summary>
        /// <param name="packet">The file upload initialization request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileUploadInitRequest(Packet packet)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received file upload init request with no payload from user {ClientSession.UserId}");
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, "", "No file information provided.", ClientSession.UserId);
                }

                // Extract file information from payload
                var fileInfo = JsonSerializer.Deserialize<FileUploadInitInfo>(packet.Payload);
                
                // Validate file name
                if (string.IsNullOrEmpty(fileInfo.FileName))
                {
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, "", "File name is required.", ClientSession.UserId);
                }

                // Check for directory ID in the packet metadata
                string directoryId = null;
                if (packet.Metadata.TryGetValue("DirectoryId", out string dirId) && dirId != "root")
                {
                    directoryId = dirId;
                    
                    // Validate directory if specified
                    var dir = await _directoryService.GetDirectoryById(directoryId, ClientSession.UserId);
                    if (dir == null)
                    {
                        _logService.Warning($"Directory not found or not owned by user: {directoryId}");
                        return _packetFactory.CreateFileUploadInitResponse(
                            false, "", "Directory not found or you do not have permission to upload to it.", 
                            ClientSession.UserId);
                    }
                }

                // Initialize the file upload
                var fileMetadata = await _fileService.InitializeFileUpload(
                    ClientSession.UserId,
                    fileInfo.FileName,
                    fileInfo.FileSize,
                    fileInfo.ContentType);
                
                // Set the directory ID if specified
                if (!string.IsNullOrEmpty(directoryId))
                {
                    fileMetadata.DirectoryId = directoryId;
                    await _fileService.UpdateFileMetadata(fileMetadata);
                }
                
                // Transition to transfer state for upload
                ClientSession.TransitionToState(
                    ClientSession.StateFactory.CreateTransferState(ClientSession, fileMetadata, true));
                
                _logService.Info($"File upload initialized: {fileInfo.FileName} (ID: {fileMetadata.Id}) for user {ClientSession.UserId}");
                
                // Create and return the response
                return _packetFactory.CreateFileUploadInitResponse(
                    true, fileMetadata.Id, "File upload initialized successfully.", ClientSession.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error initializing file upload: {ex.Message}", ex);
                return _packetFactory.CreateFileUploadInitResponse(
                    false, "", "Error initializing file upload: " + ex.Message, ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a file download initialization request.
        /// </summary>
        /// <param name="packet">The file download initialization request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileDownloadInitRequest(Packet packet)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file download init request with no file ID from user {ClientSession.UserId}");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "File ID is required.",
                        ClientSession.UserId);
                }

                // Initialize the file download
                var fileMetadata = await _fileService.InitializeFileDownload(fileId, ClientSession.UserId);
                
                if (fileMetadata == null)
                {
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "File not found or you do not have permission to download it.",
                        ClientSession.UserId);
                }

                // Calculate total chunks
                int totalChunks = (int)Math.Ceiling((double)fileMetadata.FileSize / _fileService.ChunkSize);
                
                // Transition to transfer state for download
                ClientSession.TransitionToState(
                    ClientSession.StateFactory.CreateTransferState(ClientSession, fileMetadata, false));
                
                _logService.Info($"File download initialized: {fileMetadata.FileName} (ID: {fileMetadata.Id}) for user {ClientSession.UserId}");
                
                // Create and return the response
                return _packetFactory.CreateFileDownloadInitResponse(
                    true, 
                    fileMetadata.Id, 
                    fileMetadata.FileName, 
                    fileMetadata.FileSize, 
                    fileMetadata.ContentType, 
                    totalChunks, 
                    "File download initialized successfully.", 
                    ClientSession.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error initializing file download: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "Error initializing file download: " + ex.Message,
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a file delete request.
        /// </summary>
        /// <param name="packet">The file delete request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileDeleteRequest(Packet packet)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file delete request with no file ID from user {ClientSession.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(
                        false, "", "File ID is required.", ClientSession.UserId);
                }

                // Delete the file
                bool success = await _fileService.DeleteFile(fileId, ClientSession.UserId);
                
                if (success)
                {
                    _logService.Info($"File deleted: {fileId} by user {ClientSession.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(
                        true, fileId, "File deleted successfully.", ClientSession.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to delete file: {fileId} by user {ClientSession.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(
                        false, fileId, "Failed to delete file. File not found or you do not have permission to delete it.", ClientSession.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting file: {ex.Message}", ex);
                return _packetFactory.CreateFileDeleteResponse(
                    false, "", "Error deleting file: " + ex.Message, ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a directory create request.
        /// </summary>
        /// <param name="packet">The directory create request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleDirectoryCreateRequest(Packet packet)
        {
            try
            {
                // Delegate to command handler
                var handler = new DirectoryCreateCommandHandler(_directoryService, _logService);
                return await handler.Handle(packet, ClientSession);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory create request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while creating the directory.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a directory list request.
        /// </summary>
        /// <param name="packet">The directory list request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleDirectoryListRequest(Packet packet)
        {
            try
            {
                // Delegate to command handler
                var handler = new DirectoryListCommandHandler(_directoryService, _logService);
                return await handler.Handle(packet, ClientSession);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory list request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while listing directories.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a directory rename request.
        /// </summary>
        /// <param name="packet">The directory rename request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleDirectoryRenameRequest(Packet packet)
        {
            try
            {
                // Delegate to command handler
                var handler = new DirectoryRenameCommandHandler(_directoryService, _logService);
                return await handler.Handle(packet, ClientSession);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory rename request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while renaming the directory.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a directory delete request.
        /// </summary>
        /// <param name="packet">The directory delete request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleDirectoryDeleteRequest(Packet packet)
        {
            try
            {
                // Delegate to command handler
                var handler = new DirectoryDeleteCommandHandler(_directoryService, _logService);
                return await handler.Handle(packet, ClientSession);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory delete request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while deleting the directory.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a file move request.
        /// </summary>
        /// <param name="packet">The file move request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileMoveRequest(Packet packet)
        {
            try
            {
                // Delegate to command handler
                var handler = new FileMoveCommandHandler(_directoryService, _logService);
                return await handler.Handle(packet, ClientSession);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file move request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while moving files.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Handles a directory contents request.
        /// </summary>
        /// <param name="packet">The directory contents request packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleDirectoryContentsRequest(Packet packet)
        {
            try
            {
                // Delegate to command handler
                var handler = new DirectoryContentsCommandHandler(_directoryService, _logService);
                return await handler.Handle(packet, ClientSession);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory contents request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while retrieving directory contents.",
                    ClientSession.UserId);
            }
        }

        /// <summary>
        /// Called when entering the authenticated state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnEnter()
        {
            _logService.Debug($"Session {ClientSession.SessionId} entered AuthenticatedState for user {ClientSession.UserId}");
            _lastActivityTime = DateTime.Now;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when exiting the authenticated state.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OnExit()
        {
            _logService.Debug($"Session {ClientSession.SessionId} exited AuthenticatedState for user {ClientSession.UserId}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Class for deserializing file upload initialization information.
        /// </summary>
        private class FileUploadInitInfo
        {
            public string FileName { get; set; }
            public long FileSize { get; set; }
            public string ContentType { get; set; }
        }
    }
}