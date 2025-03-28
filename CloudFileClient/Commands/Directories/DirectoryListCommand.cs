using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Models;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Commands.Directories
{
    /// <summary>
    /// Command for listing directories in a specific parent directory.
    /// </summary>
    public class DirectoryListCommand : ICommand
    {
        private readonly string _userId;
        private readonly string _parentDirectoryId;
        private readonly LogService _logService;
        private readonly ClientPacketFactory _packetFactory;
        private readonly ResponseParser _responseParser;

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName => "DirectoryList";

        /// <summary>
        /// Initializes a new instance of the DirectoryListCommand class.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <param name="logService">The logging service.</param>
        public DirectoryListCommand(string userId, string parentDirectoryId, LogService logService)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            _userId = userId;
            _parentDirectoryId = parentDirectoryId; // Can be null for root directory
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
            return _packetFactory.CreateDirectoryListRequest(_userId, _parentDirectoryId);
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
                string locationDescription = string.IsNullOrEmpty(_parentDirectoryId) 
                    ? "root directory" 
                    : $"directory with ID: {_parentDirectoryId}";
                
                _logService.Info($"Requesting directory list for {locationDescription}...");
                
                // Create the directory list request packet
                var packet = CreatePacket();
                
                // Send the packet and get the response
                var response = await connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var directories = _responseParser.ParseDirectoryListResponse(response);
                
                _logService.Info($"Received directory list with {directories.Count} directories.");
                
                // Return success result with the directory list
                return new CommandResult(response, directories);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error listing directories: {ex.Message}", ex);
                return new CommandResult($"Failed to list directories: {ex.Message}", exception: ex);
            }
        }
    }
}