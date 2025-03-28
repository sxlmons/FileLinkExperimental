using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Command handler for file deletion requests.
    /// Implements the Command pattern.
    /// </summary>
    public class FileDeleteCommandHandler : ICommandHandler
    {
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the FileDeleteCommandHandler class.
        /// </summary>
        /// <param name="fileService">The file service.</param>
        /// <param name="logService">The logging service.</param>
        public FileDeleteCommandHandler(FileService fileService, LogService logService)
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
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.FILE_DELETE_REQUEST;
        }

        /// <summary>
        /// Handles a file delete request packet.
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
                    _logService.Warning("Received file delete request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "You must be logged in to delete files.",
                        "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in file delete request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "User ID in packet does not match the authenticated user.",
                        session.UserId);
                }

                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file delete request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(
                        false, "", "File ID is required.", session.UserId);
                }

                // Delete the file
                bool success = await _fileService.DeleteFile(fileId, session.UserId);
                
                if (success)
                {
                    _logService.Info($"File deleted: {fileId} by user {session.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(
                        true, fileId, "File deleted successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to delete file: {fileId} by user {session.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(
                        false, fileId, "Failed to delete file. File not found or you do not have permission to delete it.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file delete request: {ex.Message}", ex);
                string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                
                return _packetFactory.CreateFileDeleteResponse(
                    false, fileId, $"Error deleting file: {ex.Message}", session.UserId);
            }
        }
    }
}