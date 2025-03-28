using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Interface for the file repository.
    /// Implements the Repository pattern for file metadata storage.
    /// </summary>
    public interface IFileRepository
    {
        /// <summary>
        /// Gets file metadata by file ID.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <returns>The file metadata, or null if not found.</returns>
        Task<FileMetadata> GetFileMetadataById(string fileId);

        /// <summary>
        /// Gets all file metadata for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of file metadata for the user.</returns>
        Task<IEnumerable<FileMetadata>> GetFileMetadataByUserId(string userId);

        /// <summary>
        /// Adds new file metadata.
        /// </summary>
        /// <param name="fileMetadata">The file metadata to add.</param>
        /// <returns>True if the file metadata was added successfully, otherwise false.</returns>
        Task<bool> AddFileMetadata(FileMetadata fileMetadata);

        /// <summary>
        /// Updates existing file metadata.
        /// </summary>
        /// <param name="fileMetadata">The file metadata to update.</param>
        /// <returns>True if the file metadata was updated successfully, otherwise false.</returns>
        Task<bool> UpdateFileMetadata(FileMetadata fileMetadata);

        /// <summary>
        /// Deletes file metadata.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <returns>True if the file metadata was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteFileMetadata(string fileId);
        
        /// <summary>
        /// Gets all file metadata for files in a specific directory.
        /// </summary>
        /// <param name="directoryId">The directory ID, or null for files in the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of file metadata for the specified directory and user.</returns>
        Task<IEnumerable<FileMetadata>> GetFilesByDirectoryId(string directoryId, string userId);

        /// <summary>
        /// Moves files to a different directory.
        /// </summary>
        /// <param name="fileIds">The IDs of the files to move.</param>
        /// <param name="directoryId">The ID of the target directory, or null for the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if all files were moved successfully, otherwise false.</returns>
        Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string directoryId, string userId);
        
        /// <summary>
        /// Gets a directory by its ID.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <returns>The directory metadata, or null if not found.</returns>
        Task<DirectoryMetadata> GetDirectoryById(string directoryId);
    }
}