using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Command handler for file move requests.
    /// Implements the Command pattern.
    /// </summary>
    public class FileMoveCommandHandler : ICommandHandler
    {
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the FileMoveCommandHandler class.
        /// </summary>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="logService">The logging service.</param>
        public FileMoveCommandHandler(DirectoryService directoryService, LogService logService)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Determines whether this handler can process the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code to check.</param>
        /// <returns>True if this handler can process the command code, otherwise false.</returns>
        public bool CanHandle(int commandCode)
        {
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.FILE_MOVE_REQUEST;
        }

        /// <summary>
        /// Handles a file move request packet.
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
                    _logService.Warning("Received file move request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "You must be logged in to move files.",
                        "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in file move request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "User ID in packet does not match the authenticated user.",
                        session.UserId);
                }

                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received file move request with no payload from user {session.UserId}");
                    return _packetFactory.CreateFileMoveResponse(
                        false, 0, null, "File move information is required.", session.UserId);
                }

                // Deserialize the payload to extract move information
                var moveInfo = JsonSerializer.Deserialize<FileMoveInfo>(packet.Payload);
                
                if (moveInfo.FileIds == null || moveInfo.FileIds.Count == 0)
                {
                    _logService.Warning($"Received file move request with no file IDs from user {session.UserId}");
                    return _packetFactory.CreateFileMoveResponse(
                        false, 0, moveInfo.TargetDirectoryId, "At least one file ID is required.", session.UserId);
                }

                // Validate target directory if specified
                if (!string.IsNullOrEmpty(moveInfo.TargetDirectoryId))
                {
                    var targetDir = await _directoryService.GetDirectoryById(moveInfo.TargetDirectoryId, session.UserId);
                    if (targetDir == null)
                    {
                        _logService.Warning($"Target directory not found or not owned by user: {moveInfo.TargetDirectoryId}");
                        return _packetFactory.CreateFileMoveResponse(
                            false, moveInfo.FileIds.Count, moveInfo.TargetDirectoryId,
                            "Target directory not found or you do not have permission to access it.", session.UserId);
                    }
                }

                // Move the files
                bool success = await _directoryService.MoveFilesToDirectory(
                    moveInfo.FileIds, moveInfo.TargetDirectoryId, session.UserId);
                
                if (success)
                {
                    _logService.Info($"Files moved: {moveInfo.FileIds.Count} files to directory {moveInfo.TargetDirectoryId ?? "root"} for user {session.UserId}");
                    return _packetFactory.CreateFileMoveResponse(
                        true, moveInfo.FileIds.Count, moveInfo.TargetDirectoryId,
                        "Files moved successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to move some or all files to directory {moveInfo.TargetDirectoryId ?? "root"} for user {session.UserId}");
                    return _packetFactory.CreateFileMoveResponse(
                        false, moveInfo.FileIds.Count, moveInfo.TargetDirectoryId,
                        "Failed to move some or all files. Files may not exist or you do not have permission.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file move request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred during file movement.",
                    session.UserId);
            }
        }

        /// <summary>
        /// Class for deserializing file move information from a file move request.
        /// </summary>
        private class FileMoveInfo
        {
            public List<string> FileIds { get; set; }
            public string TargetDirectoryId { get; set; }
        }
    }
}