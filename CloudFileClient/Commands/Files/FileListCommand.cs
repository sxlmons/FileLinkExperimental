using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Models;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Commands.Files
{
    /// <summary>
    /// Command for listing files in the user's account.
    /// </summary>
    public class FileListCommand : ICommand
    {
        private readonly string _userId;
        private readonly LogService _logService;
        private readonly ClientPacketFactory _packetFactory;
        private readonly ResponseParser _responseParser;

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName => "FileList";

        /// <summary>
        /// Initializes a new instance of the FileListCommand class.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="logService">The logging service.</param>
        public FileListCommand(string userId, LogService logService)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            _userId = userId;
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            _packetFactory = new ClientPacketFactory();
            _responseParser = new ResponseParser();
        }

        /// <summary>
        /// Creates a packet for this command.
        /// </summary>
        /// <returns>The packet to send to the server.</returns>
        public Packet CreatePacket()
        {
            return _packetFactory.CreateFileListRequest(_userId);
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="connection">The client connection to use.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public async Task<CommandResult> ExecuteAsync(ClientConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            if (!connection.IsConnected)
                return new CommandResult("Not connected to server.");

            try
            {
                _logService.Info("Requesting file list...");
                
                // Create the file list request packet
                var packet = CreatePacket();
                
                // Send the packet and get the response
                var response = await connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var files = _responseParser.ParseFileListResponse(response);
                
                _logService.Info($"Received file list with {files.Count} files.");
                
                // Return success result with the file list
                return new CommandResult(response, files);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error listing files: {ex.Message}", ex);
                return new CommandResult($"Failed to list files: {ex.Message}", exception: ex);
            }
        }
    }
}