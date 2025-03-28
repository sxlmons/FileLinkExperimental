using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Command handler for file list requests.
    /// Implements the Command pattern.
    /// </summary>
    public class FileListCommandHandler : ICommandHandler
    {
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the FileListCommandHandler class.
        /// </summary>
        /// <param name="fileService">The file service.</param>
        /// <param name="logService">The logging service.</param>
        public FileListCommandHandler(FileService fileService, LogService logService)
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
            return commandCode == CloudFileServer.Protocol.Commands.CommandCode.FILE_LIST_REQUEST;
        }

        /// <summary>
        /// Handles a file list request packet.
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
                    _logService.Warning("Received file list request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "You must be logged in to list files.",
                        "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in file list request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(
                        packet.CommandCode,
                        "User ID in packet does not match the authenticated user.",
                        session.UserId);
                }

                _logService.Debug($"Fetching file list for user {session.UserId}");
                
                // Get the list of files for the user
                var files = await _fileService.GetUserFiles(session.UserId);
                
                // Project the files to a simpler format for the client
                var fileList = files.Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FileSize,
                    f.ContentType,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.IsComplete
                }).ToList();
                
                _logService.Info($"Returning file list with {fileList.Count} files for user {session.UserId}");
                
                // Create and return the response
                return _packetFactory.CreateFileListResponse(fileList, session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file list request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(
                    packet.CommandCode,
                    "An error occurred while listing files.",
                    session.UserId);
            }
        }
    }
}