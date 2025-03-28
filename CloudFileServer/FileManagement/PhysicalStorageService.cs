using CloudFileServer.Services.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Service that handles physical storage operations for both files and directories.
    /// This separates physical storage concerns from logical organization.
    /// </summary>
    public class PhysicalStorageService
    {
        private readonly string _storagePath;
        private readonly LogService _logService;

        /// <summary>
        /// Initializes a new instance of the PhysicalStorageService class.
        /// </summary>
        /// <param name="storagePath">Root storage path for all user files and directories.</param>
        /// <param name="logService">The logging service.</param>
        public PhysicalStorageService(string storagePath, LogService logService)
        {
            _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Ensure root storage path exists
            Directory.CreateDirectory(_storagePath);
        }

        /// <summary>
        /// Gets the physical path for a file in the root directory.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileName">File name</param>
        /// <param name="fileId">File ID</param>
        /// <returns>The physical file path</returns>
        public string GetRootFilePath(string userId, string fileName, string fileId)
        {
            string userDirectory = GetUserDirectory(userId);
            return Path.Combine(userDirectory, $"{fileId}_{fileName}");
        }
        
        /// <summary>
        /// Gets the physical path for a directory.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="directoryName">Directory name</param>
        /// <param name="parentPath">Optional parent directory path</param>
        /// <returns>The physical directory path</returns>
        public string GetDirectoryPath(string userId, string directoryName, string parentPath = null)
        {
            if (string.IsNullOrEmpty(parentPath))
            {
                // Root-level directory
                string userDirectory = GetUserDirectory(userId);
                return Path.Combine(userDirectory, directoryName);
            }
            else
            {
                // Nested directory
                return Path.Combine(parentPath, directoryName);
            }
        }
        
        /// <summary>
        /// Gets the physical path for a file in a specific directory.
        /// </summary>
        /// <param name="directoryPath">Physical directory path</param>
        /// <param name="fileName">File name</param>
        /// <param name="fileId">File ID</param>
        /// <returns>The physical file path</returns>
        public string GetFilePathInDirectory(string directoryPath, string fileName, string fileId)
        {
            return Path.Combine(directoryPath, $"{fileId}_{fileName}");
        }
        
        /// <summary>
        /// Gets the user's root directory path.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>The user's root directory path</returns>
        public string GetUserDirectory(string userId)
        {
            string userDirectory = Path.Combine(_storagePath, userId);
            
            // Ensure user directory exists
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }
            
            return userDirectory;
        }
        
        /// <summary>
        /// Creates a physical directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to create</param>
        /// <returns>True if created successfully, false if already exists</returns>
        public bool CreateDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    return false; // Already exists
                }
                
                Directory.CreateDirectory(directoryPath);
                _logService.Debug($"Created physical directory at {directoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating directory {directoryPath}: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Deletes a physical directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to delete</param>
        /// <param name="recursive">Whether to delete contents recursively</param>
        /// <returns>True if deleted successfully</returns>
        public bool DeleteDirectory(string directoryPath, bool recursive = false)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return false; // Doesn't exist
                }
                
                Directory.Delete(directoryPath, recursive);
                _logService.Debug($"Deleted physical directory at {directoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting directory {directoryPath}: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Creates an empty file or overwrites an existing file.
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>True if created successfully</returns>
        public bool CreateEmptyFile(string filePath)
        {
            try
            {
                using (var fs = File.Create(filePath))
                {
                    // Just create the file
                }
                
                _logService.Debug($"Created empty file at {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating file {filePath}: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Moves a file from one location to another.
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <returns>True if moved successfully</returns>
        public bool MoveFile(string sourcePath, string destinationPath)
        {
            try
            {
                // Ensure destination directory exists
                string destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                // Move the file
                if (File.Exists(sourcePath))
                {
                    // Only move if paths are different
                    if (sourcePath != destinationPath)
                    {
                        File.Move(sourcePath, destinationPath);
                        _logService.Debug($"Moved file from {sourcePath} to {destinationPath}");
                    }
                    return true;
                }
                else
                {
                    _logService.Warning($"Source file not found at {sourcePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error moving file from {sourcePath} to {destinationPath}: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filePath">The file path to delete</param>
        /// <returns>True if deleted successfully</returns>
        public bool DeleteFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logService.Warning($"File not found at {filePath}");
                    return false;
                }
                
                File.Delete(filePath);
                _logService.Debug($"Deleted file at {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting file {filePath}: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Writes data to a file.
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="data">The data to write</param>
        /// <param name="offset">The offset at which to write</param>
        /// <returns>True if written successfully</returns>
        public async Task<bool> WriteFileChunk(string filePath, byte[] data, long offset)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logService.Warning($"File not found at {filePath}");
                    return false;
                }
                
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    await fileStream.WriteAsync(data, 0, data.Length);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error writing to file {filePath}: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Reads data from a file.
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="buffer">The buffer to read into</param>
        /// <param name="offset">The offset from which to read</param>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>The number of bytes read, or -1 if failed</returns>
        public async Task<int> ReadFileChunk(string filePath, byte[] buffer, long offset, int count)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logService.Warning($"File not found at {filePath}");
                    return -1;
                }
                
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    return await fileStream.ReadAsync(buffer, 0, count);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error reading from file {filePath}: {ex.Message}", ex);
                return -1;
            }
        }
    }
}