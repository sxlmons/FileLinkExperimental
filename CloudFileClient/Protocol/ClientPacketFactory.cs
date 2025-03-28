using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudFileClient.Protocol
{
    /// <summary>
    /// Factory for creating client request packets.
    /// </summary>
    public class ClientPacketFactory
    {
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
        /// Creates a file upload initialization request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <returns>A file upload initialization request packet.</returns>
        public Packet CreateFileUploadInitRequest(string userId, string fileName, long fileSize, string contentType, string directoryId = null)
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

            if (!string.IsNullOrEmpty(directoryId))
            {
                packet.Metadata["DirectoryId"] = directoryId;
            }

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

            if (fileIds is ICollection<string> collection)
            {
                packet.Metadata["FileCount"] = collection.Count.ToString(); 
            }
            
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
    }
}