using System;
using System.Collections.Generic;
using System.Text.Json;
using CloudFileClient.Models;

namespace CloudFileClient.Protocol
{
    /// <summary>
    /// Parses response packets from the server and extracts meaningful data.
    /// </summary>
    public class ResponseParser
    {
        /// <summary>
        /// Extracts a success flag and message from a response.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag and the message.</returns>
        public (bool Success, string Message) ParseBasicResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, "Invalid response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<BasicResponse>(packet.Payload);
                return (response.Success, response.Message);
            }
            catch (Exception ex)
            {
                return (false, $"Error parsing response: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a login response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag, user ID, and message.</returns>
        public (bool Success, string UserId, string Message) ParseLoginResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, string.Empty, "Invalid login response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<LoginResponse>(packet.Payload);
                return (response.Success, packet.UserId, response.Message);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error parsing login response: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses an account creation response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag, user ID, and message.</returns>
        public (bool Success, string UserId, string Message) ParseAccountCreationResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, string.Empty, "Invalid account creation response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<AccountCreationResponse>(packet.Payload);
                return (response.Success, response.UserId, response.Message);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error parsing account creation response: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a file list response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A list of file metadata.</returns>
        public List<FileMetadata> ParseFileListResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return new List<FileMetadata>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<FileMetadata>>(packet.Payload);
            }
            catch (Exception)
            {
                return new List<FileMetadata>();
            }
        }

        /// <summary>
        /// Parses a directory list response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A list of directory metadata.</returns>
        public List<DirectoryMetadata> ParseDirectoryListResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return new List<DirectoryMetadata>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<DirectoryMetadata>>(packet.Payload);
            }
            catch (Exception)
            {
                return new List<DirectoryMetadata>();
            }
        }

        /// <summary>
        /// Parses a directory contents response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing lists of file and directory metadata.</returns>
        public (List<FileMetadata> Files, List<DirectoryMetadata> Directories) ParseDirectoryContentsResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (new List<FileMetadata>(), new List<DirectoryMetadata>());
            }

            try
            {
                var response = JsonSerializer.Deserialize<DirectoryContentsResponse>(packet.Payload);
                return (response.Files, response.Directories);
            }
            catch (Exception)
            {
                return (new List<FileMetadata>(), new List<DirectoryMetadata>());
            }
        }

        /// <summary>
        /// Parses a file upload initialization response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag, file ID, and message.</returns>
        public (bool Success, string FileId, string Message) ParseFileUploadInitResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, string.Empty, "Invalid file upload initialization response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<FileUploadInitResponse>(packet.Payload);
                return (response.Success, response.FileId, response.Message);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error parsing file upload initialization response: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a file upload chunk response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag, file ID, chunk index, and message.</returns>
        public (bool Success, string FileId, int ChunkIndex, string Message) ParseFileUploadChunkResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, string.Empty, -1, "Invalid file upload chunk response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<FileUploadChunkResponse>(packet.Payload);
                return (response.Success, response.FileId, response.ChunkIndex, response.Message);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, -1, $"Error parsing file upload chunk response: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a file download initialization response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag, file ID, file name, file size, content type, total chunks, and message.</returns>
        public (bool Success, string FileId, string FileName, long FileSize, string ContentType, int TotalChunks, string Message) 
            ParseFileDownloadInitResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, string.Empty, string.Empty, 0, string.Empty, 0, "Invalid file download initialization response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<FileDownloadInitResponse>(packet.Payload);
                return (response.Success, response.FileId, response.FileName, response.FileSize, response.ContentType, response.TotalChunks, response.Message);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, string.Empty, 0, string.Empty, 0, $"Error parsing file download initialization response: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a directory creation response packet.
        /// </summary>
        /// <param name="packet">The response packet.</param>
        /// <returns>A tuple containing the success flag, directory ID, directory name, and message.</returns>
        public (bool Success, string DirectoryId, string DirectoryName, string Message) ParseDirectoryCreateResponse(Packet packet)
        {
            if (packet == null || packet.Payload == null)
            {
                return (false, string.Empty, string.Empty, "Invalid directory creation response received.");
            }

            try
            {
                var response = JsonSerializer.Deserialize<DirectoryCreateResponse>(packet.Payload);
                return (response.Success, response.DirectoryId, response.DirectoryName, response.Message);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, string.Empty, $"Error parsing directory creation response: {ex.Message}");
            }
        }

        #region Response Class Definitions

        private class BasicResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        private class LoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        private class AccountCreationResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string UserId { get; set; }
        }

        private class FileUploadInitResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; }
            public string Message { get; set; }
        }

        private class FileUploadChunkResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; }
            public int ChunkIndex { get; set; }
            public string Message { get; set; }
        }

        private class FileDownloadInitResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; }
            public string FileName { get; set; }
            public long FileSize { get; set; }
            public string ContentType { get; set; }
            public int TotalChunks { get; set; }
            public string Message { get; set; }
        }

        private class DirectoryContentsResponse
        {
            public List<FileMetadata> Files { get; set; }
            public List<DirectoryMetadata> Directories { get; set; }
            public string DirectoryId { get; set; }
        }

        private class DirectoryCreateResponse
        {
            public bool Success { get; set; }
            public string DirectoryId { get; set; }
            public string DirectoryName { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }
}