using CloudFileClient.Models;
using CloudFileClient.Protocol;
using CloudFileServer.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileClient.Services
{
    /// <summary>
    /// Provides directory management services for the cloud file client.
    /// </summary>
    public class DirectoryService
    {
        private readonly NetworkService _networkService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        /// <summary>
        /// Initializes a new instance of the DirectoryService class.
        /// </summary>
        /// <param name="networkService">The network service to use for communication.</param>
        public DirectoryService(NetworkService networkService)
        {
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        }

        /// <summary>
        /// Gets the contents of a directory.
        /// </summary>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A tuple containing lists of files and directories.</returns>
        public async Task<(List<FileItem> Files, List<DirectoryItem> Directories)> GetDirectoryContentsAsync(
            string? directoryId, string userId)
        {
            var files = new List<FileItem>();
            var directories = new List<DirectoryItem>();

            try
            {
                // Create the directory contents request packet
                var packet = new Packet(Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST)
                {
                    UserId = userId
                };

                // Set the directory ID in metadata
                if (!string.IsNullOrEmpty(directoryId))
                {
                    packet.Metadata["DirectoryId"] = directoryId;
                }
                else
                {
                    packet.Metadata["DirectoryId"] = "root";
                }

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return (files, directories);
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return (files, directories);
                }

                // Deserialize the response
                var directoryContents = JsonSerializer.Deserialize<DirectoryContentsResponse>(response.Payload);

                if (directoryContents == null)
                {
                    return (files, directories);
                }

                // Convert to models
                files = directoryContents.Files ?? new List<FileItem>();
                directories = directoryContents.Directories ?? new List<DirectoryItem>();

                return (files, directories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting directory contents: {ex.Message}");
                return (files, directories);
            }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="directoryName">The name of the directory to create.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for a root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The newly created directory, or null if creation failed.</returns>
        public async Task<DirectoryItem?> CreateDirectoryAsync(string directoryName, string? parentDirectoryId, string userId)
        {
            try
            {
                // Create directory info for serialization
                var directoryInfo = new
                {
                    DirectoryName = directoryName,
                    ParentDirectoryId = parentDirectoryId
                };

                // Serialize to JSON
                var payload = JsonSerializer.SerializeToUtf8Bytes(directoryInfo);

                // Create the directory creation request packet
                var packet = new Packet(Commands.CommandCode.DIRECTORY_CREATE_REQUEST)
                {
                    UserId = userId,
                    Payload = payload
                };

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return null;
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return null;
                }

                // Deserialize the response
                var createDirectoryResponse = JsonSerializer.Deserialize<CreateDirectoryResponse>(response.Payload);

                if (createDirectoryResponse == null || !createDirectoryResponse.Success)
                {
                    return null;
                }

                // Create and return the new directory
                return new DirectoryItem
                {
                    Id = createDirectoryResponse.DirectoryId,
                    Name = createDirectoryResponse.DirectoryName,
                    ParentDirectoryId = parentDirectoryId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="directoryId">The ID of the directory to delete.</param>
        /// <param name="recursive">Whether to recursively delete subdirectories and files.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the directory was deleted successfully.</returns>
        public async Task<bool> DeleteDirectoryAsync(string directoryId, bool recursive, string userId)
        {
            try
            {
                // Create the directory deletion request packet
                var packet = new Packet(Commands.CommandCode.DIRECTORY_DELETE_REQUEST)
                {
                    UserId = userId
                };

                packet.Metadata["DirectoryId"] = directoryId;
                packet.Metadata["Recursive"] = recursive.ToString();

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return false;
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return false;
                }

                // Deserialize the response
                var deleteDirectoryResponse = JsonSerializer.Deserialize<DeleteDirectoryResponse>(response.Payload);

                return deleteDirectoryResponse?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting directory: {ex.Message}");
                return false;
            }
        }

        // Helper classes for deserializing responses
        private class DirectoryContentsResponse
        {
            public List<FileItem>? Files { get; set; }
            public List<DirectoryItem>? Directories { get; set; }
            public string? DirectoryId { get; set; }
        }

        private class CreateDirectoryResponse
        {
            public bool Success { get; set; }
            public string DirectoryId { get; set; } = "";
            public string DirectoryName { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class DeleteDirectoryResponse
        {
            public bool Success { get; set; }
            public string DirectoryId { get; set; } = "";
            public string Message { get; set; } = "";
        }
    }
}