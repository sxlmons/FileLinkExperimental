using CloudFileServer.Core.Exceptions;
using CloudFileServer.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Service that provides directory management functionality.
    /// </summary>
    public class DirectoryService
    {
        private readonly IDirectoryRepository _directoryRepository;
        private readonly IFileRepository _fileRepository;
        private readonly string _storagePath;
        private readonly LogService _logService;

        /// <summary>
        /// Initializes a new instance of the DirectoryService class.
        /// </summary>
        /// <param name="directoryRepository">The directory repository.</param>
        /// <param name="fileRepository">The file repository.</param>
        /// <param name="storagePath">The path to the directory where files and directories are stored.</param>
        /// <param name="logService">The logging service.</param>
        public DirectoryService(
            IDirectoryRepository directoryRepository,
            IFileRepository fileRepository,
            string storagePath,
            LogService logService)
        {
            _directoryRepository = directoryRepository ?? throw new ArgumentNullException(nameof(directoryRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Ensure the storage directory exists
            Directory.CreateDirectory(_storagePath);
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryName">The directory name.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for a root directory.</param>
        /// <returns>The metadata for the new directory, or null if creation failed.</returns>
        public async Task<DirectoryMetadata> CreateDirectory(string userId, string directoryName, string parentDirectoryId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID cannot be empty.", nameof(userId));
                
                if (string.IsNullOrEmpty(directoryName))
                    throw new ArgumentException("Directory name cannot be empty.", nameof(directoryName));
                
                // Sanitize the directory name
                directoryName = SanitizeDirectoryName(directoryName);
                
                // Validate parent directory if specified
                string parentDirectoryPath = null;
                if (!string.IsNullOrEmpty(parentDirectoryId))
                {
                    var parentDir = await _directoryRepository.GetDirectoryMetadataById(parentDirectoryId);
                    if (parentDir == null)
                    {
                        _logService.Warning($"Parent directory not found: {parentDirectoryId}");
                        return null;
                    }
                    
                    // Check if the parent directory belongs to the user
                    if (parentDir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to create directory in parent owned by {parentDir.UserId}");
                        return null;
                    }
                    
                    parentDirectoryPath = parentDir.DirectoryPath;
                }
                
                // Check if a directory with the same name already exists under the parent
                bool exists = await _directoryRepository.DirectoryExistsWithName(directoryName, parentDirectoryId, userId);
                if (exists)
                {
                    _logService.Warning($"Directory '{directoryName}' already exists under parent {parentDirectoryId}");
                    return null;
                }
                
                // Generate a unique directory ID
                string directoryId = Guid.NewGuid().ToString();
                
                // Create the physical path for the directory
                string physicalPath = CreatePhysicalDirectoryPath(userId, directoryId, directoryName, parentDirectoryPath);
                
                // Create directory metadata
                var metadata = new DirectoryMetadata(userId, directoryName, parentDirectoryId, physicalPath);
                
                // Create the physical directory
                Directory.CreateDirectory(physicalPath);
                
                // Add the metadata to the repository
                bool success = await _directoryRepository.AddDirectoryMetadata(metadata);
                
                if (!success)
                {
                    // Cleanup if metadata addition failed
                    try
                    {
                        if (Directory.Exists(physicalPath))
                        {
                            Directory.Delete(physicalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.Error($"Error cleaning up directory after failed metadata addition: {ex.Message}", ex);
                    }
                    
                    throw new FileOperationException("Failed to add directory metadata.");
                }
                
                _logService.Info($"Directory created: {directoryName} (ID: {directoryId}, User: {userId})");
                
                return metadata;
            }
            catch (Exception ex) when (!(ex is FileOperationException))
            {
                _logService.Error($"Error creating directory: {ex.Message}", ex);
                throw new FileOperationException("Failed to create directory.", ex);
            }
        }

        /// <summary>
        /// Gets all directories for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of directory metadata for the user.</returns>
        public async Task<IEnumerable<DirectoryMetadata>> GetAllDirectories(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                var directories = await _directoryRepository.GetDirectoriesByUserId(userId);
                
                _logService.Debug($"Retrieved {directories.Count()} directories for user {userId}");
                
                return directories;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting user directories: {ex.Message}", ex);
                throw new FileOperationException("Failed to get user directories.", ex);
            }
        }

        /// <summary>
        /// Gets directories in a specific parent directory.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <returns>A collection of directory metadata for the specified parent.</returns>
        public async Task<IEnumerable<DirectoryMetadata>> GetDirectoriesInDirectory(string userId, string parentDirectoryId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Validate parent directory if specified
                if (!string.IsNullOrEmpty(parentDirectoryId))
                {
                    var parentDir = await _directoryRepository.GetDirectoryMetadataById(parentDirectoryId);
                    if (parentDir == null)
                    {
                        _logService.Warning($"Parent directory not found: {parentDirectoryId}");
                        return Enumerable.Empty<DirectoryMetadata>();
                    }
                    
                    // Check if the parent directory belongs to the user
                    if (parentDir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to list directories in directory owned by {parentDir.UserId}");
                        return Enumerable.Empty<DirectoryMetadata>();
                    }
                }
                
                var directories = await _directoryRepository.GetDirectoriesByParentId(parentDirectoryId, userId);
                
                _logService.Debug($"Retrieved {directories.Count()} directories for user {userId} in parent {parentDirectoryId ?? "root"}");
                
                return directories;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directories in parent: {ex.Message}", ex);
                throw new FileOperationException("Failed to get directories.", ex);
            }
        }

        /// <summary>
        /// Gets files and directories in a specific directory.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The directory ID, or null for root directory.</param>
        /// <returns>A tuple containing collections of file and directory metadata.</returns>
        public async Task<(IEnumerable<FileMetadata> Files, IEnumerable<DirectoryMetadata> Directories)> GetDirectoryContents(string userId, string directoryId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Validate directory if specified
                if (!string.IsNullOrEmpty(directoryId))
                {
                    var dir = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                    if (dir == null)
                    {
                        _logService.Warning($"Directory not found: {directoryId}");
                        return (Enumerable.Empty<FileMetadata>(), Enumerable.Empty<DirectoryMetadata>());
                    }
                    
                    // Check if the directory belongs to the user
                    if (dir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to list contents of directory owned by {dir.UserId}");
                        return (Enumerable.Empty<FileMetadata>(), Enumerable.Empty<DirectoryMetadata>());
                    }
                }
                
                // Get files and directories in parallel
                var filesTask = _fileRepository.GetFilesByDirectoryId(directoryId, userId);
                var directoriesTask = _directoryRepository.GetDirectoriesByParentId(directoryId, userId);
                
                await Task.WhenAll(filesTask, directoriesTask);
                
                var files = await filesTask;
                var directories = await directoriesTask;
                
                _logService.Debug($"Retrieved {files.Count()} files and {directories.Count()} directories for user {userId} in directory {directoryId ?? "root"}");
                
                return (files, directories);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directory contents: {ex.Message}", ex);
                throw new FileOperationException("Failed to get directory contents.", ex);
            }
        }

        /// <summary>
        /// Renames a directory.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <param name="newName">The new name for the directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the directory was renamed successfully, otherwise false.</returns>
        public async Task<bool> RenameDirectory(string directoryId, string newName, string userId)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentException("New name cannot be empty.", nameof(newName));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the directory metadata
                var metadata = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Directory not found: {directoryId}");
                    return false;
                }
                
                // Check if the user owns the directory
                if (metadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to rename directory owned by {metadata.UserId}");
                    return false;
                }
                
                // Sanitize the new name
                newName = SanitizeDirectoryName(newName);
                
                // Check if a directory with the new name already exists under the same parent
                bool exists = await _directoryRepository.DirectoryExistsWithName(newName, metadata.ParentDirectoryId, userId);
                if (exists)
                {
                    _logService.Warning($"Directory with name '{newName}' already exists under the same parent");
                    return false;
                }
                
                // Update the metadata
                string oldName = metadata.Name;
                metadata.Rename(newName);
                
                // Update the physical directory path
                string oldPath = metadata.DirectoryPath;
                string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);
                
                // Handle physical path update if necessary
                if (Path.GetFileName(oldPath) != newName)
                {
                    // Check if the physical directory exists
                    if (!Directory.Exists(oldPath))
                    {
                        _logService.Warning($"Physical directory not found at {oldPath}");
                        return false;
                    }
                    
                    // Check if the target path already exists
                    if (Directory.Exists(newPath))
                    {
                        _logService.Warning($"Target path already exists: {newPath}");
                        return false;
                    }
                    
                    // Rename the physical directory
                    Directory.Move(oldPath, newPath);
                    
                    // Update the path in metadata
                    metadata.DirectoryPath = newPath;
                }
                
                // Update the metadata in the repository
                bool success = await _directoryRepository.UpdateDirectoryMetadata(metadata);
                
                if (success)
                {
                    _logService.Info($"Directory renamed: {oldName} to {newName} (ID: {directoryId})");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error renaming directory: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="recursive">Whether to recursively delete subdirectories and files.</param>
        /// <returns>True if the directory was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteDirectory(string directoryId, string userId, bool recursive = false)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the directory metadata
                var metadata = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Directory not found: {directoryId}");
                    return false;
                }
                
                // Check if the user owns the directory
                if (metadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to delete directory owned by {metadata.UserId}");
                    return false;
                }
                
                // Check if the directory contains files
                var files = await _fileRepository.GetFilesByDirectoryId(directoryId, userId);
                if (files.Any() && !recursive)
                {
                    _logService.Warning($"Directory {directoryId} is not empty and recursive deletion is not specified");
                    return false;
                }
                
                // Check if the directory has subdirectories
                var subdirectories = await _directoryRepository.GetDirectoriesByParentId(directoryId, userId);
                if (subdirectories.Any() && !recursive)
                {
                    _logService.Warning($"Directory {directoryId} has subdirectories and recursive deletion is not specified");
                    return false;
                }
                
                // If recursive, delete all files and subdirectories
                if (recursive)
                {
                    // Get all subdirectories recursively
                    var allSubdirectories = await _directoryRepository.GetAllSubdirectoriesRecursive(directoryId);
                    
                    // Delete files in all subdirectories starting from the deepest level
                    foreach (var subdir in allSubdirectories.OrderByDescending(d => d.DirectoryPath.Count(c => c == Path.DirectorySeparatorChar)))
                    {
                        var subFiles = await _fileRepository.GetFilesByDirectoryId(subdir.Id, userId);
                        foreach (var file in subFiles)
                        {
                            if (File.Exists(file.FilePath))
                            {
                                File.Delete(file.FilePath);
                            }
                            
                            await _fileRepository.DeleteFileMetadata(file.Id);
                        }
                    }
                    
                    // Delete files in the directory itself
                    foreach (var file in files)
                    {
                        if (File.Exists(file.FilePath))
                        {
                            File.Delete(file.FilePath);
                        }
                        
                        await _fileRepository.DeleteFileMetadata(file.Id);
                    }
                    
                    // Delete subdirectory metadata starting from the deepest level
                    foreach (var subdir in allSubdirectories.OrderByDescending(d => d.DirectoryPath.Count(c => c == Path.DirectorySeparatorChar)))
                    {
                        await _directoryRepository.DeleteDirectoryMetadata(subdir.Id);
                        
                        // Delete the physical directory if it exists
                        if (Directory.Exists(subdir.DirectoryPath))
                        {
                            Directory.Delete(subdir.DirectoryPath, true);
                        }
                    }
                }
                
                // Delete the directory metadata
                bool success = await _directoryRepository.DeleteDirectoryMetadata(directoryId);
                
                if (success)
                {
                    // Delete the physical directory if it exists
                    if (Directory.Exists(metadata.DirectoryPath))
                    {
                        Directory.Delete(metadata.DirectoryPath, recursive);
                    }
                    
                    _logService.Info($"Directory deleted: {metadata.Name} (ID: {directoryId})");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting directory: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Moves files to a directory.
        /// </summary>
        /// <param name="fileIds">The file IDs to move.</param>
        /// <param name="targetDirectoryId">The target directory ID, or null for root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if all files were moved successfully, otherwise false.</returns>
        public async Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string targetDirectoryId, string userId)
        {
            if (fileIds == null)
                throw new ArgumentNullException(nameof(fileIds));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            var fileIdList = fileIds.ToList();
            if (fileIdList.Count == 0)
                return true;
            
            try
            {
                // Validate target directory if specified
                string targetDirectoryPath = null;
                if (!string.IsNullOrEmpty(targetDirectoryId))
                {
                    var targetDir = await _directoryRepository.GetDirectoryMetadataById(targetDirectoryId);
                    if (targetDir == null)
                    {
                        _logService.Warning($"Target directory not found: {targetDirectoryId}");
                        return false;
                    }
                    
                    // Check if the target directory belongs to the user
                    if (targetDir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to move files to directory owned by {targetDir.UserId}");
                        return false;
                    }
                    
                    targetDirectoryPath = targetDir.DirectoryPath;
                }
                else
                {
                    // Root directory path
                    targetDirectoryPath = Path.Combine(_storagePath, userId);
                }
                
                bool allSuccessful = true;
                
                foreach (var fileId in fileIdList)
                {
                    // Get the file metadata
                    var file = await _fileRepository.GetFileMetadataById(fileId);
                    if (file == null)
                    {
                        _logService.Warning($"File not found: {fileId}");
                        allSuccessful = false;
                        continue;
                    }
                    
                    // Check if the file belongs to the user
                    if (file.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to move file owned by {file.UserId}");
                        allSuccessful = false;
                        continue;
                    }
                    
                    // Check if the file is already in the target directory
                    if ((targetDirectoryId == null && file.DirectoryId == null) || file.DirectoryId == targetDirectoryId)
                    {
                        _logService.Debug($"File {fileId} is already in the target directory");
                        continue;
                    }
                    
                    // Calculate new file path
                    string fileName = Path.GetFileName(file.FilePath);
                    string newFilePath = Path.Combine(targetDirectoryPath, fileName);
                    
                    // Check if a file with the same name already exists in the target directory
                    if (File.Exists(newFilePath))
                    {
                        // Generate a unique name by appending a timestamp
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string extension = Path.GetExtension(fileName);
                        string uniqueName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                        newFilePath = Path.Combine(targetDirectoryPath, uniqueName);
                    }
                    
                    // Move the physical file
                    if (File.Exists(file.FilePath))
                    {
                        File.Move(file.FilePath, newFilePath);
                    }
                    else
                    {
                        _logService.Warning($"Physical file not found at {file.FilePath}");
                        // We'll still update the metadata even if the physical file is missing
                    }
                    
                    // Update the file metadata
                    file.MoveToDirectory(targetDirectoryId, newFilePath);
                    
                    // Update the metadata in the repository
                    bool updated = await _fileRepository.UpdateFileMetadata(file);
                    if (!updated)
                    {
                        _logService.Error($"Failed to update metadata for file {fileId}");
                        allSuccessful = false;
                        
                        // Try to move the file back if possible
                        if (File.Exists(newFilePath) && !File.Exists(file.FilePath))
                        {
                            try
                            {
                                File.Move(newFilePath, file.FilePath);
                            }
                            catch (Exception ex)
                            {
                                _logService.Error($"Error restoring file after failed metadata update: {ex.Message}", ex);
                            }
                        }
                    }
                    else
                    {
                        _logService.Info($"File moved: {fileId} to directory {targetDirectoryId ?? "root"}");
                    }
                }
                
                return allSuccessful;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error moving files to directory: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a directory by its ID.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <param name="userId">The user ID for ownership validation.</param>
        /// <returns>The directory metadata, or null if not found or not owned by the user.</returns>
        public async Task<DirectoryMetadata> GetDirectoryById(string directoryId, string userId)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                var directory = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                
                // Check if the directory exists and belongs to the user
                if (directory == null || directory.UserId != userId)
                {
                    return null;
                }
                
                return directory;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directory: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Sanitizes a directory name to ensure it's safe for the file system.
        /// </summary>
        /// <param name="directoryName">The directory name to sanitize.</param>
        /// <returns>The sanitized directory name.</returns>
        private string SanitizeDirectoryName(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return "unnamed_directory";
            
            // Replace invalid characters with underscores
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                directoryName = directoryName.Replace(c, '_');
            }
            
            // Ensure the directory name isn't too long
            const int maxDirectoryNameLength = 100;
            if (directoryName.Length > maxDirectoryNameLength)
            {
                directoryName = directoryName.Substring(0, maxDirectoryNameLength);
            }
            
            return directoryName;
        }

        /// <summary>
        /// Creates a physical path for a directory.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The directory ID.</param>
        /// <param name="directoryName">The directory name.</param>
        /// <param name="parentDirectoryPath">The parent directory path, or null for root directories.</param>
        /// <returns>The physical path for the directory.</returns>
        private string CreatePhysicalDirectoryPath(string userId, string directoryId, string directoryName, string parentDirectoryPath)
        {
            if (string.IsNullOrEmpty(parentDirectoryPath))
            {
                // Root directory - place directly under user directory
                return Path.Combine(_storagePath, userId, directoryName);
            }
            else
            {
                // Subdirectory - place under parent directory
                return Path.Combine(parentDirectoryPath, directoryName);
            }
        }
    }
}