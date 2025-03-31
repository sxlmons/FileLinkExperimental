using System;

namespace CloudFileClient.Models
{
    /// <summary>
    /// Represents a directory in the cloud storage.
    /// </summary>
    public class DirectoryItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the directory.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the name of the directory.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the ID of the parent directory.
        /// Null for root directories.
        /// </summary>
        public string? ParentDirectoryId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the directory was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the directory was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets a value indicating whether this directory is a root directory.
        /// </summary>
        public bool IsRoot => ParentDirectoryId == null;
    }
}