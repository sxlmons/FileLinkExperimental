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
    /// Repository for directory metadata storage.
    /// Implements the Repository pattern with file-based storage.
    /// </summary>
    public class DirectoryRepository : IDirectoryRepository
    {
        private readonly string _metadataPath;
        private readonly object _lock = new object();
        private Dictionary<string, DirectoryMetadata> _metadata = new Dictionary<string, DirectoryMetadata>();
        private readonly LogService _logService;

        /// <summary>
        /// Initializes a new instance of the DirectoryRepository class.
        /// </summary>
        /// <param name="metadataPath">The path to the directory where directory metadata is stored.</param>
        /// <param name="logService">The logging service.</param>
        public DirectoryRepository(string metadataPath, LogService logService)
        {
            _metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Ensure directories exist
            Directory.CreateDirectory(_metadataPath);
            
            // Load metadata from storage
            LoadMetadata().Wait();
        }

        /// <summary>
        /// Gets directory metadata by directory ID.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <returns>The directory metadata, or null if not found.</returns>
        public Task<DirectoryMetadata> GetDirectoryMetadataById(string directoryId)
        {
            if (string.IsNullOrEmpty(directoryId))
                return Task.FromResult<DirectoryMetadata>(null);

            lock (_lock)
            {
                _metadata.TryGetValue(directoryId, out DirectoryMetadata metadata);
                return Task.FromResult(metadata);
            }
        }

        /// <summary>
        /// Gets all directory metadata for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of directory metadata for the user.</returns>
        public Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<IEnumerable<DirectoryMetadata>>(Array.Empty<DirectoryMetadata>());

            lock (_lock)
            {
                var userDirectories = _metadata.Values
                    .Where(m => m.UserId == userId)
                    .OrderBy(m => m.Name)
                    .ToList();
                
                return Task.FromResult<IEnumerable<DirectoryMetadata>>(userDirectories);
            }
        }

        /// <summary>
        /// Gets directory metadata for directories with a specific parent.
        /// </summary>
        /// <param name="parentDirectoryId">The parent directory ID, or null to get root directories.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of directory metadata for the specified parent and user.</returns>
        public Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByParentId(string parentDirectoryId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<IEnumerable<DirectoryMetadata>>(Array.Empty<DirectoryMetadata>());

            lock (_lock)
            {
                var directories = _metadata.Values
                    .Where(m => m.UserId == userId && 
                               (parentDirectoryId == null ? 
                                    string.IsNullOrEmpty(m.ParentDirectoryId) : 
                                    m.ParentDirectoryId == parentDirectoryId))
                    .OrderBy(m => m.Name)
                    .ToList();
                
                return Task.FromResult<IEnumerable<DirectoryMetadata>>(directories);
            }
        }

        /// <summary>
        /// Gets root directory metadata for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of root directory metadata for the user.</returns>
        public Task<IEnumerable<DirectoryMetadata>> GetRootDirectories(string userId)
        {
            return GetDirectoriesByParentId(null, userId);
        }

        /// <summary>
        /// Adds new directory metadata.
        /// </summary>
        /// <param name="directoryMetadata">The directory metadata to add.</param>
        /// <returns>True if the directory metadata was added successfully, otherwise false.</returns>
        public async Task<bool> AddDirectoryMetadata(DirectoryMetadata directoryMetadata)
        {
            if (directoryMetadata == null)
                throw new ArgumentNullException(nameof(directoryMetadata));
            
            if (string.IsNullOrEmpty(directoryMetadata.Id))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryMetadata));

            // Validate parent directory if specified
            if (!string.IsNullOrEmpty(directoryMetadata.ParentDirectoryId))
            {
                var parentDir = await GetDirectoryMetadataById(directoryMetadata.ParentDirectoryId);
                if (parentDir == null)
                {
                    _logService.Warning($"Attempted to add directory with non-existent parent: {directoryMetadata.ParentDirectoryId}");
                    return false;
                }

                // Ensure the parent directory belongs to the same user
                if (parentDir.UserId != directoryMetadata.UserId)
                {
                    _logService.Warning($"Attempted to add directory to a parent owned by a different user");
                    return false;
                }
            }

            // Check if directory with the same name already exists in the parent
            bool exists = await DirectoryExistsWithName(directoryMetadata.Name, directoryMetadata.ParentDirectoryId, directoryMetadata.UserId);
            if (exists)
            {
                _logService.Warning($"Directory with name '{directoryMetadata.Name}' already exists in the parent directory");
                return false;
            }

            lock (_lock)
            {
                // Check if the directory ID already exists
                if (_metadata.ContainsKey(directoryMetadata.Id))
                {
                    _logService.Warning($"Attempted to add directory metadata with existing ID: {directoryMetadata.Id}");
                    return false;
                }

                _metadata[directoryMetadata.Id] = directoryMetadata;
            }

            // Save changes to storage
            await SaveMetadata();
            
            _logService.Debug($"Directory metadata added: {directoryMetadata.Name} (ID: {directoryMetadata.Id})");
            return true;
        }

        /// <summary>
        /// Updates existing directory metadata.
        /// </summary>
        /// <param name="directoryMetadata">The directory metadata to update.</param>
        /// <returns>True if the directory metadata was updated successfully, otherwise false.</returns>
        public async Task<bool> UpdateDirectoryMetadata(DirectoryMetadata directoryMetadata)
        {
            if (directoryMetadata == null)
                throw new ArgumentNullException(nameof(directoryMetadata));
            
            if (string.IsNullOrEmpty(directoryMetadata.Id))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryMetadata));

            lock (_lock)
            {
                if (!_metadata.ContainsKey(directoryMetadata.Id))
                {
                    _logService.Warning($"Attempted to update non-existent directory metadata: {directoryMetadata.Id}");
                    return false;
                }

                _metadata[directoryMetadata.Id] = directoryMetadata;
            }

            // Save changes to storage
            await SaveMetadata();
            
            _logService.Debug($"Directory metadata updated: {directoryMetadata.Name} (ID: {directoryMetadata.Id})");
            return true;
        }

        /// <summary>
        /// Deletes directory metadata.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <returns>True if the directory metadata was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteDirectoryMetadata(string directoryId)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));

            DirectoryMetadata metadata;
            lock (_lock)
            {
                if (!_metadata.TryGetValue(directoryId, out metadata))
                {
                    _logService.Warning($"Attempted to delete non-existent directory metadata: {directoryId}");
                    return false;
                }

                // Check if there are subdirectories
                bool hasSubdirectories = _metadata.Values.Any(d => d.ParentDirectoryId == directoryId);
                if (hasSubdirectories)
                {
                    _logService.Warning($"Cannot delete directory {directoryId} because it has subdirectories.");
                    return false;
                }

                _metadata.Remove(directoryId);
            }

            // Save changes to storage
            await SaveMetadata();
            
            _logService.Info($"Directory metadata deleted: {metadata.Name} (ID: {directoryId})");
            return true;
        }

        /// <summary>
        /// Checks if a directory exists with the given name and parent.
        /// </summary>
        /// <param name="name">The directory name to check.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if a directory with this name already exists under the specified parent, otherwise false.</returns>
        public Task<bool> DirectoryExistsWithName(string name, string parentDirectoryId, string userId)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userId))
                return Task.FromResult(false);

            lock (_lock)
            {
                bool exists = _metadata.Values.Any(d => 
                    d.UserId == userId && 
                    d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                    ((parentDirectoryId == null && string.IsNullOrEmpty(d.ParentDirectoryId)) || 
                     (d.ParentDirectoryId == parentDirectoryId)));
                
                return Task.FromResult(exists);
            }
        }

        /// <summary>
        /// Gets all subdirectories for a given directory recursively.
        /// </summary>
        /// <param name="directoryId">The parent directory ID.</param>
        /// <returns>A collection of all subdirectory metadata.</returns>
        public Task<IEnumerable<DirectoryMetadata>> GetAllSubdirectoriesRecursive(string directoryId)
        {
            if (string.IsNullOrEmpty(directoryId))
                return Task.FromResult<IEnumerable<DirectoryMetadata>>(Array.Empty<DirectoryMetadata>());

            lock (_lock)
            {
                var result = new List<DirectoryMetadata>();
                var directQueue = new Queue<string>();
                
                // Start with immediate children
                var immediateChildren = _metadata.Values
                    .Where(d => d.ParentDirectoryId == directoryId)
                    .ToList();
                
                foreach (var child in immediateChildren)
                {
                    result.Add(child);
                    directQueue.Enqueue(child.Id);
                }
                
                // Process all descendants in breadth-first order
                while (directQueue.Count > 0)
                {
                    string currentId = directQueue.Dequeue();
                    var children = _metadata.Values.Where(d => d.ParentDirectoryId == currentId);
                    
                    foreach (var child in children)
                    {
                        result.Add(child);
                        directQueue.Enqueue(child.Id);
                    }
                }
                
                return Task.FromResult<IEnumerable<DirectoryMetadata>>(result);
            }
        }

        /// <summary>
        /// Saves all directory metadata to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task SaveMetadata()
        {
            try
            {
                string filePath = Path.Combine(_metadataPath, "directories.json");
                
                // Create a copy of the metadata dictionary to avoid holding the lock during file I/O
                Dictionary<string, DirectoryMetadata> metadataCopy;
                lock (_lock)
                {
                    metadataCopy = new Dictionary<string, DirectoryMetadata>(_metadata);
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
                
                _logService.Debug($"Directory metadata saved to {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error saving directory metadata: {ex.Message}", ex);
                throw new FileOperationException("Failed to save directory metadata.", ex);
            }
        }

        /// <summary>
        /// Loads all directory metadata from storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task LoadMetadata()
        {
            try
            {
                string filePath = Path.Combine(_metadataPath, "directories.json");
                
                if (!File.Exists(filePath))
                {
                    _logService.Info($"Directory metadata file not found at {filePath}. Creating a new one.");
                    return;
                }
                
                // Read the file
                string json = await File.ReadAllTextAsync(filePath);
                
                // Deserialize from JSON
                var metadataList = JsonSerializer.Deserialize<List<DirectoryMetadata>>(json);
                
                // Build the dictionary
                Dictionary<string, DirectoryMetadata> metadataDict = new Dictionary<string, DirectoryMetadata>();
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
                
                _logService.Info($"Loaded {_metadata.Count} directory metadata entries from {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error loading directory metadata: {ex.Message}", ex);
                
                // If we can't load the metadata, just start with an empty dictionary
                lock (_lock)
                {
                    _metadata = new Dictionary<string, DirectoryMetadata>();
                }
            }
        }
    }
}