using CloudFileServer.Core.Exceptions;
using CloudFileServer.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Repository for file metadata storage.
    /// Implements the Repository pattern with file-based storage.
    /// </summary>
    public class FileRepository : IFileRepository
    {
        private readonly string _metadataPath;
        private readonly string _storagePath;
        private readonly object _lock = new object();
        private Dictionary<string, FileMetadata> _metadata = new Dictionary<string, FileMetadata>();
        private readonly LogService _logService;
        private readonly IDirectoryRepository _directoryRepository;

        /// <summary>
        /// Initializes a new instance of the FileRepository class.
        /// </summary>
        /// <param name="metadataPath">The path to the directory where file metadata is stored.</param>
        /// <param name="storagePath">The path to the directory where files are stored.</param>
        /// <param name="directoryRepository">The directory repository.</param>
        /// <param name="logService">The logging service.</param>
        public FileRepository(string metadataPath, string storagePath, IDirectoryRepository directoryRepository, LogService logService)
        {
            _metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
            _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
            _directoryRepository = directoryRepository ?? throw new ArgumentNullException(nameof(directoryRepository));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    
            // Ensure directories exist
            Directory.CreateDirectory(_metadataPath);
            Directory.CreateDirectory(_storagePath);
    
            // Load metadata from storage
            LoadMetadata().Wait();
        }

        /// <summary>
        /// Gets file metadata by file ID.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <returns>The file metadata, or null if not found.</returns>
        public Task<FileMetadata> GetFileMetadataById(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                return Task.FromResult<FileMetadata>(null);

            lock (_lock)
            {
                _metadata.TryGetValue(fileId, out FileMetadata metadata);
                return Task.FromResult(metadata);
            }
        }

        /// <summary>
        /// Gets all file metadata for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of file metadata for the user.</returns>
        public Task<IEnumerable<FileMetadata>> GetFileMetadataByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<IEnumerable<FileMetadata>>(Array.Empty<FileMetadata>());

            lock (_lock)
            {
                var userFiles = _metadata.Values
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.UpdatedAt)
                    .ToList();
                
                return Task.FromResult<IEnumerable<FileMetadata>>(userFiles);
            }
        }

        /// <summary>
        /// Adds new file metadata.
        /// </summary>
        /// <param name="fileMetadata">The file metadata to add.</param>
        /// <returns>True if the file metadata was added successfully, otherwise false.</returns>
        public async Task<bool> AddFileMetadata(FileMetadata fileMetadata)
        {
            if (fileMetadata == null)
                throw new ArgumentNullException(nameof(fileMetadata));
            
            if (string.IsNullOrEmpty(fileMetadata.Id))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileMetadata));

            lock (_lock)
            {
                // Check if the file ID already exists
                if (_metadata.ContainsKey(fileMetadata.Id))
                {
                    _logService.Warning($"Attempted to add file metadata with existing ID: {fileMetadata.Id}");
                    return false;
                }

                _metadata[fileMetadata.Id] = fileMetadata;
            }

            // Save changes to storage
            await SaveMetadata();
            
            _logService.Debug($"File metadata added: {fileMetadata.FileName} (ID: {fileMetadata.Id})");
            return true;
        }

        /// <summary>
        /// Updates existing file metadata.
        /// </summary>
        /// <param name="fileMetadata">The file metadata to update.</param>
        /// <returns>True if the file metadata was updated successfully, otherwise false.</returns>
        public async Task<bool> UpdateFileMetadata(FileMetadata fileMetadata)
        {
            if (fileMetadata == null)
                throw new ArgumentNullException(nameof(fileMetadata));
            
            if (string.IsNullOrEmpty(fileMetadata.Id))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileMetadata));

            lock (_lock)
            {
                if (!_metadata.ContainsKey(fileMetadata.Id))
                {
                    _logService.Warning($"Attempted to update non-existent file metadata: {fileMetadata.Id}");
                    return false;
                }

                _metadata[fileMetadata.Id] = fileMetadata;
            }

            // Save changes to storage
            await SaveMetadata();
            
            _logService.Debug($"File metadata updated: {fileMetadata.FileName} (ID: {fileMetadata.Id})");
            return true;
        }

        /// <summary>
        /// Deletes file metadata.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <returns>True if the file metadata was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteFileMetadata(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));

            FileMetadata metadata;
            lock (_lock)
            {
                if (!_metadata.TryGetValue(fileId, out metadata))
                {
                    _logService.Warning($"Attempted to delete non-existent file metadata: {fileId}");
                    return false;
                }

                _metadata.Remove(fileId);
            }

            // Save changes to storage
            await SaveMetadata();
            
            _logService.Info($"File metadata deleted: {metadata.FileName} (ID: {fileId})");
            return true;
        }

        /// <summary>
        /// Saves all file metadata to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task SaveMetadata()
        {
            try
            {
                string filePath = Path.Combine(_metadataPath, "files.json");
                
                // Create a copy of the metadata dictionary to avoid holding the lock during file I/O
                Dictionary<string, FileMetadata> metadataCopy;
                lock (_lock)
                {
                    metadataCopy = new Dictionary<string, FileMetadata>(_metadata);
                }
                
                // Convert to a list for serialization
                var metadataList = metadataCopy.Values.ToList();
                
                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string json = JsonSerializer.Serialize(metadataList, options);
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json);
                
                _logService.Debug($"File metadata saved to {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error saving file metadata: {ex.Message}", ex);
                throw new FileOperationException("Failed to save file metadata.", ex);
            }
        }

        /// <summary>
        /// Loads all file metadata from storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task LoadMetadata()
        {
            try
            {
                string filePath = Path.Combine(_metadataPath, "files.json");
                
                if (!File.Exists(filePath))
                {
                    _logService.Info($"File metadata file not found at {filePath}. Creating a new one.");
                    return;
                }
                
                // Read the file
                string json = await File.ReadAllTextAsync(filePath);
                
                // Deserialize from JSON
                var metadataList = JsonSerializer.Deserialize<List<FileMetadata>>(json);
                
                // Build the dictionary
                Dictionary<string, FileMetadata> metadataDict = new Dictionary<string, FileMetadata>();
                foreach (var metadata in metadataList)
                {
                    if (!string.IsNullOrEmpty(metadata.Id))
                    {
                        metadataDict[metadata.Id] = metadata;
                    }
                }
                
                // Update the metadata dictionary
                lock (_lock)
                {
                    _metadata = metadataDict;
                }
                
                _logService.Info($"Loaded {_metadata.Count} file metadata entries from {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error loading file metadata: {ex.Message}", ex);
                
                // If we can't load the metadata, just start with an empty dictionary
                lock (_lock)
                {
                    _metadata = new Dictionary<string, FileMetadata>();
                }
            }
        }
        
        /// <summary>
        /// Gets all file metadata for files in a specific directory.
        /// </summary>
        /// <param name="directoryId">The directory ID, or null for files in the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of file metadata for the specified directory and user.</returns>
        public Task<IEnumerable<FileMetadata>> GetFilesByDirectoryId(string directoryId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<IEnumerable<FileMetadata>>(Array.Empty<FileMetadata>());

            lock (_lock)
            {
                var files = _metadata.Values
                    .Where(m => m.UserId == userId && 
                              (directoryId == null ? 
                                    string.IsNullOrEmpty(m.DirectoryId) : 
                                    m.DirectoryId == directoryId))
                    .OrderByDescending(m => m.UpdatedAt)
                    .ToList();
                
                return Task.FromResult<IEnumerable<FileMetadata>>(files);
            }
        }

        /// <summary>
        /// Moves files to a different directory.
        /// </summary>
        /// <param name="fileIds">The IDs of the files to move.</param>
        /// <param name="directoryId">The ID of the target directory, or null for the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if all files were moved successfully, otherwise false.</returns>
        public async Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string directoryId, string userId)
        {
            if (string.IsNullOrEmpty(userId) || fileIds == null)
                return false;

            var fileIdList = fileIds.ToList();
            if (fileIdList.Count == 0)
                return true;

            // Validate directory if specified
            if (!string.IsNullOrEmpty(directoryId))
            {
                // This would require dependency injection of the DirectoryRepository
                // For now, assume this check is done at the service layer
            }

            bool allSuccessful = true;
            foreach (var fileId in fileIdList)
            {
                var file = await GetFileMetadataById(fileId);
                if (file == null || file.UserId != userId)
                {
                    _logService.Warning($"File not found or access denied when moving file {fileId} to directory {directoryId}");
                    allSuccessful = false;
                    continue;
                }

                // Update file metadata with new directory
                file.DirectoryId = directoryId;
                file.UpdatedAt = DateTime.Now;

                // Update storage metadata
                bool updated = await UpdateFileMetadata(file);
                if (!updated)
                {
                    _logService.Error($"Failed to update metadata for file {fileId} when moving to directory {directoryId}");
                    allSuccessful = false;
                }
            }

            return allSuccessful;
        }
        
        /// <summary>
        /// Gets a directory by its ID.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <returns>The directory metadata, or null if not found.</returns>
        public async Task<DirectoryMetadata> GetDirectoryById(string directoryId)
        {
            try
            {
                // This implementation depends on having access to the DirectoryRepository
                // We'll use the private field _directoryRepository
                if (string.IsNullOrEmpty(directoryId))
                    return null;
        
                return await _directoryRepository.GetDirectoryMetadataById(directoryId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directory by ID {directoryId}: {ex.Message}", ex);
                return null;
            }
        }
    }
}