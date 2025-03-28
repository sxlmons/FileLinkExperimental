using System.Text.Json;

namespace CloudFileServer.Protocol
{
    /// <summary>
    /// Factory for creating packets with specific command codes and payloads.
    /// Implements the Factory pattern to create different types of packets.
    /// </summary>
    public class PacketFactory
    {
        /// <summary>
        /// Creates an account creation request packet.
        /// </summary>
        /// <param name="username">The username for the new account.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="email">The email address for the new account.</param>
        /// <returns>An account creation request packet.</returns>
        public Packet CreateAccountCreationRequest(string username, string password, string email)
        {
            var accountInfo = new { Username = username, Password = password, Email = email };
            var payload = JsonSerializer.SerializeToUtf8Bytes(accountInfo);

            return new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_REQUEST,
                Payload = payload
            };
        }
        
        /// <summary>
        /// Creates an account creation response packet.
        /// </summary>
        /// <param name="success">Whether the account creation was successful.</param>
        /// <param name="message">A message about the account creation result.</param>
        /// <param name="userId">The ID of the newly created user, if successful.</param>
        /// <returns>An account creation response packet.</returns>
        public Packet CreateAccountCreationResponse(bool success, string message, string userId = "")
        {
            var response = new 
            { 
                Success = success, 
                Message = message,
                UserId = userId
            };
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            
            return packet;
        }

        /// <summary>
        /// Creates a login request packet.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A login request packet.</returns>
        public Packet CreateLoginRequest(string username, string password)
        {
            var credentials = new { Username = username, Password = password };
            var payload = JsonSerializer.SerializeToUtf8Bytes(credentials);

            return new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_REQUEST,
                Payload = payload
            };
        }

        /// <summary>
        /// Creates a login response packet.
        /// </summary>
        /// <param name="success">Whether the login was successful.</param>
        /// <param name="message">A message about the login result.</param>
        /// <param name="userId">The user ID, if login was successful.</param>
        /// <returns>A login response packet.</returns>
        public Packet CreateLoginResponse(bool success, string message, string userId = "")
        {
            var response = new 
            { 
                Success = success, 
                Message = message
            };
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            
            return packet;
        }

        /// <summary>
        /// Creates a logout request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A logout request packet.</returns>
        public Packet CreateLogoutRequest(string userId)
        {
            return new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_REQUEST,
                UserId = userId
            };
        }

        /// <summary>
        /// Creates a logout response packet.
        /// </summary>
        /// <param name="success">Whether the logout was successful.</param>
        /// <param name="message">A message about the logout result.</param>
        /// <returns>A logout response packet.</returns>
        public Packet CreateLogoutResponse(bool success, string message)
        {
            var response = new 
            { 
                Success = success, 
                Message = message
            };
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            
            return packet;
        }

        /// <summary>
        /// Creates a file list request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file list request packet.</returns>
        public Packet CreateFileListRequest(string userId)
        {
            return new Packet
            {
                CommandCode = Commands.CommandCode.FILE_LIST_REQUEST,
                UserId = userId
            };
        }

        /// <summary>
        /// Creates a file list response packet.
        /// </summary>
        /// <param name="files">The list of file metadata.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file list response packet.</returns>
        public Packet CreateFileListResponse(IEnumerable<object> files, string userId)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(files);

            return new Packet
            {
                CommandCode = Commands.CommandCode.FILE_LIST_RESPONSE,
                UserId = userId,
                Payload = payload
            };
        }

        /// <summary>
        /// Creates a file upload initialization request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <returns>A file upload initialization request packet.</returns>
        public Packet CreateFileUploadInitRequest(string userId, string fileName, long fileSize, string contentType)
        {
            var initData = new
            {
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(initData);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST,
                UserId = userId,
                Payload = payload,
                Metadata =
                {
                    ["FileName"] = fileName,
                    ["FileSize"] = fileSize.ToString(),
                    ["ContentType"] = contentType
                }
            };

            return packet;
        }

        /// <summary>
        /// Creates a file upload initialization response packet.
        /// </summary>
        /// <param name="success">Whether the initialization was successful.</param>
        /// <param name="fileId">The ID assigned to the file.</param>
        /// <param name="message">A message about the initialization result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file upload initialization response packet.</returns>
        public Packet CreateFileUploadInitResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file upload chunk request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="isLastChunk">Whether this is the last chunk.</param>
        /// <param name="data">The chunk data.</param>
        /// <returns>A file upload chunk request packet.</returns>
        public Packet CreateFileUploadChunkRequest(string userId, string fileId, int chunkIndex, bool isLastChunk, byte[] data)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
                UserId = userId,
                Payload = data
            };

            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();
            packet.Metadata["IsLastChunk"] = isLastChunk.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a file upload chunk response packet.
        /// </summary>
        /// <param name="success">Whether the chunk was successfully processed.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="message">A message about the chunk processing result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file upload chunk response packet.</returns>
        public Packet CreateFileUploadChunkResponse(bool success, string fileId, int chunkIndex, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                ChunkIndex = chunkIndex,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a file upload complete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file upload complete request packet.</returns>
        public Packet CreateFileUploadCompleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file upload complete response packet.
        /// </summary>
        /// <param name="success">Whether the upload was successfully completed.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="message">A message about the completion result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file upload complete response packet.</returns>
        public Packet CreateFileUploadCompleteResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file download initialization request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file download initialization request packet.</returns>
        public Packet CreateFileDownloadInitRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file download initialization response packet.
        /// </summary>
        /// <param name="success">Whether the initialization was successful.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="totalChunks">The total number of chunks.</param>
        /// <param name="message">A message about the initialization result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file download initialization response packet.</returns>
        public Packet CreateFileDownloadInitResponse(bool success, string fileId, string fileName, 
            long fileSize, string contentType, int totalChunks, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                TotalChunks = totalChunks,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_RESPONSE,
                UserId = userId,
                Payload = payload,
                Metadata =
                {
                    ["Success"] = success.ToString(),
                    ["FileId"] = fileId,
                    ["FileName"] = fileName,
                    ["FileSize"] = fileSize.ToString(),
                    ["ContentType"] = contentType,
                    ["TotalChunks"] = totalChunks.ToString()
                }
            };

            return packet;
        }

        /// <summary>
        /// Creates a file download chunk request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk to download.</param>
        /// <returns>A file download chunk request packet.</returns>
        public Packet CreateFileDownloadChunkRequest(string userId, string fileId, int chunkIndex)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a file download chunk response packet.
        /// </summary>
        /// <param name="success">Whether the chunk was successfully retrieved.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="isLastChunk">Whether this is the last chunk.</param>
        /// <param name="data">The chunk data.</param>
        /// <param name="message">A message about the chunk retrieval result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file download chunk response packet.</returns>
        public Packet CreateFileDownloadChunkResponse(bool success, string fileId, int chunkIndex, 
            bool isLastChunk, byte[] data, string message, string userId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_RESPONSE,
                UserId = userId,
                Payload = data
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();
            packet.Metadata["IsLastChunk"] = isLastChunk.ToString();
            packet.Metadata["Message"] = message;

            return packet;
        }

        /// <summary>
        /// Creates a file download complete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file download complete request packet.</returns>
        public Packet CreateFileDownloadCompleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file download complete response packet.
        /// </summary>
        /// <param name="success">Whether the download was successfully completed.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="message">A message about the completion result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file download complete response packet.</returns>
        public Packet CreateFileDownloadCompleteResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file delete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file delete request packet.</returns>
        public Packet CreateFileDeleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DELETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file delete response packet.
        /// </summary>
        /// <param name="success">Whether the file was successfully deleted.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="message">A message about the deletion result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file delete response packet.</returns>
        public Packet CreateFileDeleteResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DELETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates an error response packet.
        /// </summary>
        /// <param name="originalCommandCode">The command code of the request that caused the error.</param>
        /// <param name="message">The error message.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>An error response packet.</returns>
        public Packet CreateErrorResponse(int originalCommandCode, string message, string userId = "")
        {
            var response = new
            {
                OriginalCommandCode = originalCommandCode,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.ERROR,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["OriginalCommandCode"] = originalCommandCode.ToString();
            packet.Metadata["OriginalCommandName"] = Commands.CommandCode.GetCommandName(originalCommandCode);

            return packet;
        }
        
        /// <summary>
        /// Creates a directory creation request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryName">The name of the directory to create.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for a root directory.</param>
        /// <returns>A directory creation request packet.</returns>
        public Packet CreateDirectoryCreateRequest(string userId, string directoryName, string parentDirectoryId = null)
        {
            var directoryInfo = new
            {
                DirectoryName = directoryName,
                ParentDirectoryId = parentDirectoryId
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(directoryInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CREATE_REQUEST,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["DirectoryName"] = directoryName;
            if (!string.IsNullOrEmpty(parentDirectoryId))
            {
                packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory creation response packet.
        /// </summary>
        /// <param name="success">Whether the directory creation was successful.</param>
        /// <param name="directoryId">The ID of the new directory, if successful.</param>
        /// <param name="directoryName">The name of the new directory.</param>
        /// <param name="message">A message about the directory creation result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory creation response packet.</returns>
        public Packet CreateDirectoryCreateResponse(bool success, string directoryId, string directoryName, string message, string userId)
        {
            var response = new
            {
                Success = success,
                DirectoryId = directoryId,
                DirectoryName = directoryName,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CREATE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["DirectoryName"] = directoryName;

            return packet;
        }

        /// <summary>
        /// Creates a directory list request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <returns>A directory list request packet.</returns>
        public Packet CreateDirectoryListRequest(string userId, string parentDirectoryId = null)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_LIST_REQUEST,
                UserId = userId
            };

            if (!string.IsNullOrEmpty(parentDirectoryId))
            {
                packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory list response packet.
        /// </summary>
        /// <param name="directories">The list of directory metadata.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory list response packet.</returns>
        public Packet CreateDirectoryListResponse(IEnumerable<object> directories, string parentDirectoryId, string userId)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(directories);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_LIST_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            if (!string.IsNullOrEmpty(parentDirectoryId))
            {
                packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
            }

            packet.Metadata["Count"] = directories is ICollection<object> collection ? collection.Count.ToString() : "unknown";

            return packet;
        }

        /// <summary>
        /// Creates a directory rename request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The ID of the directory to rename.</param>
        /// <param name="newName">The new name for the directory.</param>
        /// <returns>A directory rename request packet.</returns>
        public Packet CreateDirectoryRenameRequest(string userId, string directoryId, string newName)
        {
            var renameInfo = new
            {
                DirectoryId = directoryId,
                NewName = newName
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(renameInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_RENAME_REQUEST,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["NewName"] = newName;

            return packet;
        }

        /// <summary>
        /// Creates a directory rename response packet.
        /// </summary>
        /// <param name="success">Whether the rename operation was successful.</param>
        /// <param name="directoryId">The ID of the renamed directory.</param>
        /// <param name="newName">The new name of the directory.</param>
        /// <param name="message">A message about the rename result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory rename response packet.</returns>
        public Packet CreateDirectoryRenameResponse(bool success, string directoryId, string newName, string message, string userId)
        {
            var response = new
            {
                Success = success,
                DirectoryId = directoryId,
                NewName = newName,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_RENAME_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["NewName"] = newName;

            return packet;
        }

        /// <summary>
        /// Creates a directory delete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The ID of the directory to delete.</param>
        /// <param name="recursive">Whether to delete the directory recursively.</param>
        /// <returns>A directory delete request packet.</returns>
        public Packet CreateDirectoryDeleteRequest(string userId, string directoryId, bool recursive)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_DELETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["Recursive"] = recursive.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a directory delete response packet.
        /// </summary>
        /// <param name="success">Whether the delete operation was successful.</param>
        /// <param name="directoryId">The ID of the deleted directory.</param>
        /// <param name="message">A message about the delete result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory delete response packet.</returns>
        public Packet CreateDirectoryDeleteResponse(bool success, string directoryId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                DirectoryId = directoryId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_DELETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["DirectoryId"] = directoryId;

            return packet;
        }

        /// <summary>
        /// Creates a file move request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileIds">The IDs of the files to move.</param>
        /// <param name="targetDirectoryId">The ID of the target directory, or null for the root directory.</param>
        /// <returns>A file move request packet.</returns>
        public Packet CreateFileMoveRequest(string userId, IEnumerable<string> fileIds, string targetDirectoryId)
        {
            var moveInfo = new
            {
                FileIds = fileIds,
                TargetDirectoryId = targetDirectoryId
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(moveInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_MOVE_REQUEST,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["FileCount"] = fileIds is ICollection<string> collection ? collection.Count.ToString() : "unknown";
            if (!string.IsNullOrEmpty(targetDirectoryId))
            {
                packet.Metadata["TargetDirectoryId"] = targetDirectoryId;
            }
            else
            {
                packet.Metadata["TargetDirectoryId"] = "root";
            }

            return packet;
        }

        /// <summary>
        /// Creates a file move response packet.
        /// </summary>
        /// <param name="success">Whether the move operation was successful.</param>
        /// <param name="fileCount">The number of files moved.</param>
        /// <param name="targetDirectoryId">The ID of the target directory, or null for the root directory.</param>
        /// <param name="message">A message about the move result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file move response packet.</returns>
        public Packet CreateFileMoveResponse(bool success, int fileCount, string targetDirectoryId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileCount = fileCount,
                TargetDirectoryId = targetDirectoryId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_MOVE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileCount"] = fileCount.ToString();
            if (!string.IsNullOrEmpty(targetDirectoryId))
            {
                packet.Metadata["TargetDirectoryId"] = targetDirectoryId;
            }
            else
            {
                packet.Metadata["TargetDirectoryId"] = "root";
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory contents request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <returns>A directory contents request packet.</returns>
        public Packet CreateDirectoryContentsRequest(string userId, string directoryId = null)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST,
                UserId = userId
            };

            if (!string.IsNullOrEmpty(directoryId))
            {
                packet.Metadata["DirectoryId"] = directoryId;
            }
            else
            {
                packet.Metadata["DirectoryId"] = "root";
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory contents response packet.
        /// </summary>
        /// <param name="files">The list of file metadata in the directory.</param>
        /// <param name="directories">The list of subdirectory metadata in the directory.</param>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory contents response packet.</returns>
        public Packet CreateDirectoryContentsResponse(IEnumerable<object> files, IEnumerable<object> directories, string directoryId, string userId)
        {
            var contentsInfo = new
            {
                Files = files,
                Directories = directories,
                DirectoryId = directoryId
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(contentsInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["FileCount"] = files is ICollection<object> fileCollection ? fileCollection.Count.ToString() : "unknown";
            packet.Metadata["DirectoryCount"] = directories is ICollection<object> dirCollection ? dirCollection.Count.ToString() : "unknown";
            
            if (!string.IsNullOrEmpty(directoryId))
            {
                packet.Metadata["DirectoryId"] = directoryId;
            }
            else
            {
                packet.Metadata["DirectoryId"] = "root";
            }

            return packet;
        }
    }
}