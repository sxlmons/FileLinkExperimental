using CloudFileServer.Core.Exceptions;
using CloudFileServer.Services.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Service that provides file management functionality.
    /// </summary>
    public class FileService
    {
        /// <summary>
        /// Private fields in FileService class
        /// </summary>
        private readonly IFileRepository _fileRepository;
        private readonly PhysicalStorageService _storageService;
        private readonly LogService _logService;
        private readonly ArrayPool<byte> _bufferPool;
        
        /// <summary>
        /// Gets the chunk size for file transfers.
        /// </summary>
        public int ChunkSize { get; }

        /// <summary>
        /// Initializes a new instance of the FileService class.
        /// </summary>
        /// <param name="fileRepository">The file repository.</param>
        /// <param name="storageService">The physical storage service.</param>
        /// <param name="logService">The logging service.</param>
        /// <param name="chunkSize">The chunk size for file transfers. Default is 1MB.</param>
        public FileService(
            IFileRepository fileRepository,
            PhysicalStorageService storageService,
            LogService logService,
            int chunkSize = 1024 * 1024)
        {
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            ChunkSize = chunkSize;
    
            // Initialize buffer pool for efficient memory usage
            _bufferPool = ArrayPool<byte>.Shared;
        }

        /// <summary>
        /// Initializes a file upload.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileSize">The file size.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="directoryId">Optional directory ID where the file should be stored.</param>
        /// <returns>The file metadata for the new file.</returns>
        public async Task<FileMetadata> InitializeFileUpload(string userId, string fileName, long fileSize, string contentType, string directoryId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID cannot be empty.", nameof(userId));
                
                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentException("File name cannot be empty.", nameof(fileName));
                
                if (fileSize <= 0)
                    throw new ArgumentException("File size must be greater than zero.", nameof(fileSize));
                
                // Sanitize the file name
                fileName = SanitizeFileName(fileName);
                
                // Generate a unique file ID
                string fileId = Guid.NewGuid().ToString();
                
                // Generate the file path
                string filePath;
                
                if (string.IsNullOrEmpty(directoryId))
                {
                    // Store in user's root directory
                    filePath = _storageService.GetRootFilePath(userId, fileName, fileId);
                }
                else
                {
                    // Lookup the directory's physical path
                    var directoryMetadata = await _fileRepository.GetDirectoryById(directoryId);
                    if (directoryMetadata == null || directoryMetadata.UserId != userId)
                    {
                        throw new FileOperationException($"Directory {directoryId} not found or not owned by user {userId}");
                    }
                    
                    // Get the file path in this directory
                    filePath = _storageService.GetFilePathInDirectory(directoryMetadata.DirectoryPath, fileName, fileId);
                    
                    // Ensure the directory exists
                    _storageService.CreateDirectory(directoryMetadata.DirectoryPath);
                }
                
                // Create file metadata
                var metadata = new FileMetadata(userId, fileName, fileSize, contentType, filePath)
                {
                    TotalChunks = CalculateTotalChunks(fileSize),
                    ChunksReceived = 0,
                    IsComplete = false,
                    DirectoryId = directoryId
                };
                
                // Create an empty file
                if (!_storageService.CreateEmptyFile(filePath))
                {
                    throw new FileOperationException($"Failed to create file at {filePath}");
                }
                
                // Add the metadata to the repository
                bool success = await _fileRepository.AddFileMetadata(metadata);
                
                if (!success)
                {
                    // Clean up the empty file
                    _storageService.DeleteFile(filePath);
                    throw new FileOperationException("Failed to initialize file upload.");
                }
                
                _logService.Info($"File upload initialized: {fileName} (ID: {fileId}, Size: {fileSize} bytes, User: {userId}, Directory: {directoryId ?? "root"}, Path: {filePath})");
                
                return metadata;
            }
            catch (Exception ex) when (!(ex is FileOperationException))
            {
                _logService.Error($"Error initializing file upload: {ex.Message}", ex);
                throw new FileOperationException("Failed to initialize file upload.", ex);
            }
        }

       /// <summary>
        /// Processes a file chunk.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <param name="isLastChunk">Whether this is the last chunk.</param>
        /// <param name="chunkData">The chunk data.</param>
        /// <returns>True if the chunk was processed successfully, otherwise false.</returns>
        public async Task<bool> ProcessFileChunk(string fileId, int chunkIndex, bool isLastChunk, byte[] chunkData)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            
            if (chunkIndex < 0)
                throw new ArgumentException("Chunk index must be non-negative.", nameof(chunkIndex));
            
            if (chunkData == null || chunkData.Length == 0)
                throw new ArgumentException("Chunk data cannot be empty.", nameof(chunkData));
            
            try
            {
                // Get the file metadata
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Attempted to process chunk for non-existent file: {fileId}");
                    return false;
                }
                
                // Validate chunk index
                if (chunkIndex != metadata.ChunksReceived)
                {
                    _logService.Warning($"Received out-of-order chunk {chunkIndex} for file {fileId}, expected {metadata.ChunksReceived}");
                    return false;
                }
                
                // Calculate the offset in the file
                long offset = (long)chunkIndex * ChunkSize;
                
                // Write the chunk to the file using the storage service
                bool writeSuccess = await _storageService.WriteFileChunk(metadata.FilePath, chunkData, offset);
                if (!writeSuccess)
                {
                    _logService.Warning($"Failed to write chunk {chunkIndex} for file {fileId}");
                    return false;
                }
                
                // Update the metadata
                metadata.AddChunk();
                
                // If this is the last chunk, mark the file as complete
                if (isLastChunk)
                {
                    metadata.MarkComplete();
                }
                
                // Update the metadata in the repository
                await _fileRepository.UpdateFileMetadata(metadata);
                
                _logService.Debug($"Processed chunk {chunkIndex} for file {fileId} (Size: {chunkData.Length} bytes)");
                
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file chunk: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Finalizes a file upload.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <returns>True if the file upload was finalized successfully, otherwise false.</returns>
        public async Task<bool> FinalizeFileUpload(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            
            try
            {
                // Get the file metadata
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Attempted to finalize upload for non-existent file: {fileId}");
                    return false;
                }
                
                // Check if all chunks have been received
                if (metadata.ChunksReceived < metadata.TotalChunks)
                {
                    _logService.Warning($"Attempted to finalize incomplete upload: {fileId} (Received: {metadata.ChunksReceived}, Total: {metadata.TotalChunks})");
                    return false;
                }
                
                // Mark the file as complete
                metadata.MarkComplete();
                
                // Update the metadata in the repository
                await _fileRepository.UpdateFileMetadata(metadata);
                
                // Verify the file size - using file system API since this is just a verification
                var fileInfo = new FileInfo(metadata.FilePath);
                
                if (fileInfo.Length != metadata.FileSize)
                {
                    _logService.Warning($"File size mismatch for {fileId}: Expected {metadata.FileSize}, Actual {fileInfo.Length}");
                    // We'll still consider this a success, but log the discrepancy
                }
                
                _logService.Info($"File upload finalized: {metadata.FileName} (ID: {fileId}, Size: {fileInfo.Length} bytes)");
                
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error finalizing file upload: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Initializes a file download.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The file metadata for the file to download.</returns>
        public async Task<FileMetadata> InitializeFileDownload(string fileId, string userId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the file metadata
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Attempted to download non-existent file: {fileId}");
                    return null;
                }
                
                // Check if the user owns the file
                if (!await ValidateUserOwnership(fileId, userId))
                {
                    _logService.Warning($"User {userId} attempted to download file {fileId} owned by {metadata.UserId}");
                    return null;
                }
                
                // Check if the file is complete
                if (!metadata.IsComplete)
                {
                    _logService.Warning($"Attempted to download incomplete file: {fileId}");
                    return null;
                }
                
                // Check if the file exists
                if (!File.Exists(metadata.FilePath))
                {
                    _logService.Warning($"File not found at {metadata.FilePath} for file {fileId}");
                    return null;
                }
                
                _logService.Info($"File download initialized: {metadata.FileName} (ID: {fileId}, Size: {metadata.FileSize} bytes, User: {userId})");
                
                return metadata;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error initializing file download: {ex.Message}", ex);
                throw new FileOperationException("Failed to initialize file download.", ex);
            }
        }

        /// <summary>
        /// Gets a file chunk.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <returns>A tuple containing the chunk data and a flag indicating if this is the last chunk.</returns>
        public async Task<(byte[] data, bool isLastChunk)> GetFileChunk(string fileId, int chunkIndex)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            
            if (chunkIndex < 0)
                throw new ArgumentException("Chunk index must be non-negative.", nameof(chunkIndex));
            
            try
            {
                // Get the file metadata
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Attempted to get chunk for non-existent file: {fileId}");
                    return (null, false);
                }
                
                // Check if the file exists
                if (!File.Exists(metadata.FilePath))
                {
                    _logService.Warning($"File not found at {metadata.FilePath} for file {fileId}");
                    return (null, false);
                }
                
                // Calculate the offset in the file
                long offset = (long)chunkIndex * ChunkSize;
                
                // Check if this is the last chunk
                int totalChunks = CalculateTotalChunks(metadata.FileSize);
                bool isLastChunk = chunkIndex == totalChunks - 1;
                
                // Calculate the chunk size (the last chunk may be smaller)
                int actualChunkSize = isLastChunk
                    ? (int)(metadata.FileSize - offset)
                    : ChunkSize;
                
                // Check if the offset is valid
                if (offset >= metadata.FileSize)
                {
                    _logService.Warning($"Invalid chunk index {chunkIndex} for file {fileId} (Size: {metadata.FileSize}, Offset: {offset})");
                    return (null, false);
                }
                
                // Read the chunk from the file
                byte[] buffer = _bufferPool.Rent(actualChunkSize);
                
                try
                {
                    using (var fileStream = new FileStream(metadata.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fileStream.Seek(offset, SeekOrigin.Begin);
                        int bytesRead = await fileStream.ReadAsync(buffer, 0, actualChunkSize);
                        
                        if (bytesRead != actualChunkSize)
                        {
                            _logService.Warning($"Expected to read {actualChunkSize} bytes, but read {bytesRead} bytes for file {fileId}");
                        }
                        
                        // Copy the data to a new array of the exact size
                        byte[] data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);
                        
                        _logService.Debug($"Read chunk {chunkIndex} for file {fileId} (Size: {bytesRead} bytes, IsLastChunk: {isLastChunk})");
                        
                        return (data, isLastChunk);
                    }
                }
                finally
                {
                    // Return the buffer to the pool
                    _bufferPool.Return(buffer);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting file chunk: {ex.Message}", ex);
                return (null, false);
            }
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the file was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteFile(string fileId, string userId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
    
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
    
            try
            {
                // Get the file metadata
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
        
                if (metadata == null)
                {
                    _logService.Warning($"Attempted to delete non-existent file: {fileId}");
                    return false;
                }
        
                // Check if the user owns the file
                if (metadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to delete file {fileId} owned by {metadata.UserId}");
                    return false;
                }
        
                // Delete the file using the storage service
                _storageService.DeleteFile(metadata.FilePath);
        
                // Delete the metadata
                bool success = await _fileRepository.DeleteFileMetadata(fileId);
        
                if (success)
                {
                    _logService.Info($"File deleted: {metadata.FileName} (ID: {fileId}, User: {userId})");
                }
        
                return success;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting file: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets all files for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of file metadata for the user.</returns>
        public async Task<IEnumerable<FileMetadata>> GetUserFiles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                var files = await _fileRepository.GetFileMetadataByUserId(userId);
                
                _logService.Debug($"Retrieved {files.Count()} files for user {userId}");
                
                return files;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting user files: {ex.Message}", ex);
                throw new FileOperationException("Failed to get user files.", ex);
            }
        }

        /// <summary>
        /// Validates that a user owns a file.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the user owns the file, otherwise false.</returns>
        private async Task<bool> ValidateUserOwnership(string fileId, string userId)
        {
            var metadata = await _fileRepository.GetFileMetadataById(fileId);
            
            if (metadata == null)
                return false;
            
            return metadata.UserId == userId;
        }

        /// <summary>
        /// Calculates the total number of chunks for a file of the given size.
        /// </summary>
        /// <param name="fileSize">The file size.</param>
        /// <returns>The total number of chunks.</returns>
        private int CalculateTotalChunks(long fileSize)
        {
            return (int)Math.Ceiling((double)fileSize / ChunkSize);
        }

        /// <summary>
        /// Sanitizes a file name to ensure it's safe for the file system.
        /// </summary>
        /// <param name="fileName">The file name to sanitize.</param>
        /// <returns>The sanitized file name.</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed_file";
            
            // Replace invalid characters with underscores
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            
            // Ensure the file name isn't too long
            const int maxFileNameLength = 100;
            if (fileName.Length > maxFileNameLength)
            {
                string extension = Path.GetExtension(fileName);
                fileName = fileName.Substring(0, maxFileNameLength - extension.Length) + extension;
            }
            
            return fileName;
        }
        
        /// <summary>
        /// Gets file metadata by ID.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <returns>The file metadata, or null if not found.</returns>
        public async Task<FileMetadata> GetFileMetadataById(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
    
            try
            {
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
        
                if (metadata == null)
                {
                    _logService.Debug($"File metadata not found for ID: {fileId}");
                }
        
                return metadata;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting file metadata by ID: {ex.Message}", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Updates the metadata for a file.
        /// </summary>
        /// <param name="fileMetadata">The file metadata to update.</param>
        /// <returns>True if the file metadata was updated successfully, otherwise false.</returns>
        public async Task<bool> UpdateFileMetadata(FileMetadata fileMetadata)
        {
            if (fileMetadata == null)
                throw new ArgumentNullException(nameof(fileMetadata));
    
            try
            {
                bool success = await _fileRepository.UpdateFileMetadata(fileMetadata);
        
                if (success)
                {
                    _logService.Debug($"File metadata updated: {fileMetadata.FileName} (ID: {fileMetadata.Id})");
                }
                else
                {
                    _logService.Warning($"Failed to update metadata for file {fileMetadata.Id}");
                }
        
                return success;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error updating file metadata: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Moves a file to a different directory, updating both logical and physical locations.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="targetDirectoryId">The target directory ID, or null for root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the file was moved successfully, otherwise false.</returns>
        public async Task<bool> MoveFileToDirectory(string fileId, string targetDirectoryId, string userId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
                
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the file metadata
                var fileMetadata = await _fileRepository.GetFileMetadataById(fileId);
                if (fileMetadata == null)
                {
                    _logService.Warning($"File not found: {fileId}");
                    return false;
                }
                
                // Check if the user owns the file
                if (fileMetadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to move file owned by {fileMetadata.UserId}");
                    return false;
                }
                
                // If the file is already in the target directory, nothing to do
                if ((targetDirectoryId == null && fileMetadata.DirectoryId == null) ||
                    (fileMetadata.DirectoryId == targetDirectoryId))
                {
                    _logService.Debug($"File {fileId} is already in the specified directory");
                    return true;
                }
                
                // Determine the target directory path
                string targetPath;
                if (string.IsNullOrEmpty(targetDirectoryId))
                {
                    // Target is root directory
                    targetPath = _storageService.GetUserDirectory(userId);
                }
                else
                {
                    // Target is a specific directory
                    var targetDir = await _fileRepository.GetDirectoryById(targetDirectoryId);
                    if (targetDir == null || targetDir.UserId != userId)
                    {
                        _logService.Warning($"Target directory not found or not owned by user: {targetDirectoryId}");
                        return false;
                    }
                    
                    targetPath = targetDir.DirectoryPath;
                }
                
                // Get just the filename part of the current path
                string fileName = Path.GetFileName(fileMetadata.FilePath);
                string newFilePath = Path.Combine(targetPath, fileName);
                
                // If a file with the same name already exists in the target, create a unique name
                if (new FileInfo(newFilePath).Exists && newFilePath != fileMetadata.FilePath)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string uniqueName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    newFilePath = Path.Combine(targetPath, uniqueName);
                }
                
                // Move the physical file using the storage service
                bool moveSuccess = _storageService.MoveFile(fileMetadata.FilePath, newFilePath);
                if (!moveSuccess && new FileInfo(fileMetadata.FilePath).Exists)
                {
                    _logService.Warning($"Failed to move file from {fileMetadata.FilePath} to {newFilePath}");
                    return false;
                }
                
                // Update the file metadata
                string oldDirectoryId = fileMetadata.DirectoryId;
                string oldFilePath = fileMetadata.FilePath;
                
                fileMetadata.DirectoryId = targetDirectoryId;
                fileMetadata.FilePath = newFilePath;
                fileMetadata.UpdatedAt = DateTime.Now;
                
                // Update the metadata in the repository
                bool success = await _fileRepository.UpdateFileMetadata(fileMetadata);
                
                if (success)
                {
                    _logService.Info($"File moved: {fileId} from directory {oldDirectoryId ?? "root"} to {targetDirectoryId ?? "root"}, path updated from {oldFilePath} to {newFilePath}");
                    return true;
                }
                else
                {
                    _logService.Error($"Failed to update metadata for file {fileId} after moving");
                    
                    // Try to move the file back if we moved it
                    if (new FileInfo(newFilePath).Exists && oldFilePath != newFilePath)
                    {
                        _storageService.MoveFile(newFilePath, oldFilePath);
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error moving file to directory: {ex.Message}", ex);
                return false;
            }
        }
    }
}