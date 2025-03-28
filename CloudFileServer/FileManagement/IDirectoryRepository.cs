using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Interface for the directory repository.
    /// Implements the Repository pattern for directory metadata storage.
    /// </summary>
    public interface IDirectoryRepository
    {
        /// <summary>
        /// Gets directory metadata by directory ID.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <returns>The directory metadata, or null if not found.</returns>
        Task<DirectoryMetadata> GetDirectoryMetadataById(string directoryId);

        /// <summary>
        /// Gets all directory metadata for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of directory metadata for the user.</returns>
        Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByUserId(string userId);

        /// <summary>
        /// Gets directory metadata for directories with a specific parent.
        /// </summary>
        /// <param name="parentDirectoryId">The parent directory ID, or null to get root directories.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of directory metadata for the specified parent and user.</returns>
        Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByParentId(string parentDirectoryId, string userId);

        /// <summary>
        /// Gets root directory metadata for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of root directory metadata for the user.</returns>
        Task<IEnumerable<DirectoryMetadata>> GetRootDirectories(string userId);

        /// <summary>
        /// Adds new directory metadata.
        /// </summary>
        /// <param name="directoryMetadata">The directory metadata to add.</param>
        /// <returns>True if the directory metadata was added successfully, otherwise false.</returns>
        Task<bool> AddDirectoryMetadata(DirectoryMetadata directoryMetadata);

        /// <summary>
        /// Updates existing directory metadata.
        /// </summary>
        /// <param name="directoryMetadata">The directory metadata to update.</param>
        /// <returns>True if the directory metadata was updated successfully, otherwise false.</returns>
        Task<bool> UpdateDirectoryMetadata(DirectoryMetadata directoryMetadata);

        /// <summary>
        /// Deletes directory metadata.
        /// </summary>
        /// <param name="directoryId">The directory ID.</param>
        /// <returns>True if the directory metadata was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteDirectoryMetadata(string directoryId);

        /// <summary>
        /// Checks if a directory exists with the given name and parent.
        /// </summary>
        /// <param name="name">The directory name to check.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if a directory with this name already exists under the specified parent, otherwise false.</returns>
        Task<bool> DirectoryExistsWithName(string name, string parentDirectoryId, string userId);

        /// <summary>
        /// Gets all subdirectories for a given directory recursively.
        /// </summary>
        /// <param name="directoryId">The parent directory ID.</param>
        /// <returns>A collection of all subdirectory metadata.</returns>
        Task<IEnumerable<DirectoryMetadata>> GetAllSubdirectoriesRecursive(string directoryId);
    }
}