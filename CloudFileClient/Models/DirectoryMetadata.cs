using System;

namespace CloudFileClient.Models
{
    /// <summary>
    /// Represents metadata for a directory stored on the server.
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
        /// Gets or sets a value indicating whether this is a root directory.
        /// </summary>
        public bool IsRoot { get; set; }
    }
}