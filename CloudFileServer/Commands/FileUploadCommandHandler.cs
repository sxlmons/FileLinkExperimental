using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Command handler for file upload operations.
    /// Implements the Command pattern.
    /// </summary>
    public class FileUploadCommandHandler : ICommandHandler
    {
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the FileUploadCommandHandler class.
        /// </summary>
        /// <param name="fileService">The file service.</param>
        /// <param name="logService">The logging service.</param>
        public FileUploadCommandHandler(FileService fileService, LogService logService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Determines whether this handler can process the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code to check.</param>
        /// <returns>True if this handler can process the command code, otherwise false.</returns>
        public bool CanHandle(int commandCode)
        {
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST ||
                   commandCode == CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST ||
                   commandCode == CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST;
        }

        /// <summary>
        /// Handles a file upload packet.
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
                    _logService.Warning("Received file upload request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "You must be logged in to upload files.",
                        "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in file upload request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "User ID in packet does not match the authenticated user.",
                        session.UserId);
                }

                // Handle the appropriate upload command
                switch (packet.CommandCode)
                {
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST:
                        return await HandleFileUploadInitRequest(packet, session);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST:
                        return await HandleFileUploadChunkRequest(packet, session);
                        
                    case CloudFileServer.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST:
                        return await HandleFileUploadCompleteRequest(packet, session);
                        
                    default:
                        _logService.Warning($"Unexpected command code in file upload handler: {packet.CommandCode}");
                        return _packetFactory.CreateErrorResponse(
                            packet.CommandCode,
                            "Unexpected command code for file upload operation.",
                            session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file upload request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred during the file upload operation.",
                    session.UserId);
            }
        }

        /// <summary>
        /// Handles a file upload initialization request.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        /// <param name="session">The client session that sent the packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileUploadInitRequest(Packet packet, ClientSession session)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received file upload init request with no payload from user {session.UserId}");
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, "", "No file information provided.", session.UserId);
                }

                // Deserialize the payload to extract file information
                var fileInfo = JsonSerializer.Deserialize<FileUploadInitInfo>(packet.Payload);
                
                if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FileName))
                {
                    _logService.Warning($"Received file upload init request with invalid file information from user {session.UserId}");
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, "", "File name is required.", session.UserId);
                }

                if (fileInfo.FileSize <= 0)
                {
                    _logService.Warning($"Received file upload init request with invalid file size: {fileInfo.FileSize} from user {session.UserId}");
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, "", "File size must be greater than zero.", session.UserId);
                }
                
                // Check for directory ID in the metadata
                string directoryId = null;
                if (packet.Metadata.TryGetValue("DirectoryId", out string dirId) && dirId != "root")
                {
                    directoryId = dirId;
    
                    // Validation of directory would be done here
                    // This would require injecting DirectoryService into this handler
                    // For now, we'll assume this validation is handled at the service level
                }

                // Initialize the file upload
                var fileMetadata = await _fileService.InitializeFileUpload(
                    session.UserId,
                    fileInfo.FileName,
                    fileInfo.FileSize,
                    fileInfo.ContentType ?? "application/octet-stream");
                
                // Set the directory ID if specified
                if (!string.IsNullOrEmpty(directoryId))
                {
                    fileMetadata.DirectoryId = directoryId;
                    await _fileService.UpdateFileMetadata(fileMetadata);
                }
                
                if (fileMetadata == null)
                {
                    _logService.Error($"Failed to initialize file upload for user {session.UserId}");
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, "", "Failed to initialize file upload.", session.UserId);
                }
                
                _logService.Info($"File upload initialized: {fileInfo.FileName} (ID: {fileMetadata.Id}) for user {session.UserId}");
                
                // Create and return the response
                return _packetFactory.CreateFileUploadInitResponse(
                    true, fileMetadata.Id, "File upload initialized successfully.", session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling file upload init request: {ex.Message}", ex);
                return _packetFactory.CreateFileUploadInitResponse(
                    false, "", $"Error initializing file upload: {ex.Message}", session.UserId);
            }
        }

        /// <summary>
        /// Handles a file upload chunk request.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        /// <param name="session">The client session that sent the packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileUploadChunkRequest(Packet packet, ClientSession session)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file upload chunk request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateFileUploadChunkResponse(
                        false, "", -1, "File ID is required.", session.UserId);
                }

                // Get chunk index from metadata
                if (!packet.Metadata.TryGetValue("ChunkIndex", out string chunkIndexStr) || 
                    !int.TryParse(chunkIndexStr, out int chunkIndex))
                {
                    _logService.Warning($"Received file upload chunk request with invalid chunk index: {chunkIndexStr} from user {session.UserId}");
                    return _packetFactory.CreateFileUploadChunkResponse(
                        false, fileId, -1, "Valid chunk index is required.", session.UserId);
                }

                // Get isLastChunk flag from metadata
                bool isLastChunk = false;
                if (packet.Metadata.TryGetValue("IsLastChunk", out string isLastChunkStr))
                {
                    bool.TryParse(isLastChunkStr, out isLastChunk);
                }

                // Check if payload contains data
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received file upload chunk request with no data from user {session.UserId}");
                    return _packetFactory.CreateFileUploadChunkResponse(
                        false, fileId, chunkIndex, "Chunk data is required.", session.UserId);
                }

                // Process the chunk
                bool success = await _fileService.ProcessFileChunk(fileId, chunkIndex, isLastChunk, packet.Payload);
                
                if (success)
                {
                    _logService.Debug($"Processed chunk {chunkIndex} for file {fileId} from user {session.UserId}");
                    return _packetFactory.CreateFileUploadChunkResponse(
                        true, fileId, chunkIndex, "Chunk processed successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to process chunk {chunkIndex} for file {fileId} from user {session.UserId}");
                    return _packetFactory.CreateFileUploadChunkResponse(
                        false, fileId, chunkIndex, "Failed to process chunk.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling file upload chunk request: {ex.Message}", ex);
                string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                int chunkIndex = packet.Metadata.TryGetValue("ChunkIndex", out string idx) && int.TryParse(idx, out int index) ? index : -1;
                
                return _packetFactory.CreateFileUploadChunkResponse(
                    false, fileId, chunkIndex, $"Error processing chunk: {ex.Message}", session.UserId);
            }
        }

        /// <summary>
        /// Handles a file upload complete request.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        /// <param name="session">The client session that sent the packet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response packet.</returns>
        private async Task<Packet> HandleFileUploadCompleteRequest(Packet packet, ClientSession session)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file upload complete request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateFileUploadCompleteResponse(
                        false, "", "File ID is required.", session.UserId);
                }

                // Finalize the upload
                bool success = await _fileService.FinalizeFileUpload(fileId);
                
                if (success)
                {
                    _logService.Info($"File upload completed for file {fileId} by user {session.UserId}");
                    return _packetFactory.CreateFileUploadCompleteResponse(
                        true, fileId, "File upload completed successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to complete file upload for file {fileId} by user {session.UserId}");
                    return _packetFactory.CreateFileUploadCompleteResponse(
                        false, fileId, "Failed to complete file upload.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling file upload complete request: {ex.Message}", ex);
                string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                
                return _packetFactory.CreateFileUploadCompleteResponse(
                    false, fileId, $"Error completing file upload: {ex.Message}", session.UserId);
            }
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