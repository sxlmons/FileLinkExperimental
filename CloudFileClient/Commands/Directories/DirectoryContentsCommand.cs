using System;
using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Models;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Commands.Directories
{
    /// <summary>
    /// Command for listing both files and directories in a specific directory.
    /// </summary>
    public class DirectoryContentsCommand : ICommand
    {
        private readonly string _userId;
        private readonly string _directoryId;
        private readonly LogService _logService;
        private readonly ClientPacketFactory _packetFactory;
        private readonly ResponseParser _responseParser;

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName => "DirectoryContents";

        /// <summary>
        /// Initializes a new instance of the DirectoryContentsCommand class.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <param name="logService">The logging service.</param>
        public DirectoryContentsCommand(string userId, string directoryId, LogService logService)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            _userId = userId;
            _directoryId = directoryId; // Can be null for root directory
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
            return _packetFactory.CreateDirectoryContentsRequest(_userId, _directoryId);
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
                string locationDescription = string.IsNullOrEmpty(_directoryId) 
                    ? "root directory" 
                    : $"directory with ID: {_directoryId}";
                
                _logService.Info($"Requesting directory contents for {locationDescription}...");
                
                // Create the directory contents request packet
                var packet = CreatePacket();
                
                // Send the packet and get the response
                var response = await connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var (files, directories) = _responseParser.ParseDirectoryContentsResponse(response);
                
                _logService.Info($"Received directory contents with {files.Count} files and {directories.Count} directories.");
                
                // Return success result with both files and directories
                return new CommandResult(response, new DirectoryContentsResult
                {
                    Files = files,
                    Directories = directories,
                    DirectoryId = _directoryId
                });
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directory contents: {ex.Message}", ex);
                return new CommandResult($"Failed to get directory contents: {ex.Message}", exception: ex);
            }
        }
    }

    /// <summary>
    /// Holds the result of a directory contents command.
    /// </summary>
    public class DirectoryContentsResult
    {
        /// <summary>
        /// Gets or sets the list of files in the directory.
        /// </summary>
        public List<FileMetadata> Files { get; set; } = new List<FileMetadata>();
        
        /// <summary>
        /// Gets or sets the list of directories in the directory.
        /// </summary>
        public List<DirectoryMetadata> Directories { get; set; } = new List<DirectoryMetadata>();
        
        /// <summary>
        /// Gets or sets the directory ID.
        /// </summary>
        public string DirectoryId { get; set; }
    }
}