using System;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Represents metadata for a directory stored in the system.
    /// </summary>
    public class DirectoryMetadata
    {
        /// <summary>
        /// Gets or sets the unique identifier for the directory.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who owns the directory.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the directory.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ID of the parent directory.
        /// Null for root directories.
        /// </summary>
        public string ParentDirectoryId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the directory was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the directory was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the physical path where the directory is stored.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Initializes a new instance of the DirectoryMetadata class.
        /// </summary>
        public DirectoryMetadata()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the DirectoryMetadata class with the specified parameters.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the directory.</param>
        /// <param name="name">The name of the directory.</param>
        /// <param name="parentDirectoryId">The ID of the parent directory, or null for root directories.</param>
        /// <param name="directoryPath">The path where the directory is stored.</param>
        public DirectoryMetadata(string userId, string name, string parentDirectoryId, string directoryPath)
            : this()
        {
            UserId = userId;
            Name = name;
            ParentDirectoryId = parentDirectoryId;
            DirectoryPath = directoryPath;
        }

        /// <summary>
        /// Updates the metadata when the directory is renamed.
        /// </summary>
        /// <param name="newName">The new name for the directory.</param>
        public void Rename(string newName)
        {
            Name = newName;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Updates the metadata when the directory is moved.
        /// </summary>
        /// <param name="newParentDirectoryId">The ID of the new parent directory.</param>
        /// <param name="newDirectoryPath">The new physical path.</param>
        public void Move(string newParentDirectoryId, string newDirectoryPath)
        {
            ParentDirectoryId = newParentDirectoryId;
            DirectoryPath = newDirectoryPath;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Checks if this directory is a root directory.
        /// </summary>
        /// <returns>True if this is a root directory, otherwise false.</returns>
        public bool IsRoot()
        {
            return string.IsNullOrEmpty(ParentDirectoryId);
        }
    }
}