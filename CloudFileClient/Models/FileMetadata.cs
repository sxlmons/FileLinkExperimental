using System;

namespace CloudFileClient.Models
{
    /// <summary>
    /// Represents metadata for a file stored on the server.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Gets or sets the unique identifier for the file.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who owns the file.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the content type (MIME type) of the file.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file upload is complete.
        /// </summary>
        public bool IsComplete { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the directory containing this file.
        /// Null for files in the root directory.
        /// </summary>
        public string DirectoryId { get; set; }

        /// <summary>
        /// Gets a formatted string representing the file size.
        /// </summary>
        /// <returns>A human-readable file size string.</returns>
        public string GetFormattedSize()
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            
            if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F2} KB";
            
            if (FileSize < 1024 * 1024 * 1024)
                return $"{FileSize / (1024.0 * 1024.0):F2} MB";
            
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}