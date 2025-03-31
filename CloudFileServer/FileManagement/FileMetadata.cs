using System;

namespace CloudFileServer.FileManagement
{
    /// <summary>
    /// Represents metadata for a file stored in the system.
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
        /// Gets or sets the physical path where the file is stored.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file upload is complete.
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Gets or sets the number of chunks received for the file.
        /// </summary>
        public int ChunksReceived { get; set; }

        /// <summary>
        /// Gets or sets the total number of chunks for the file.
        /// </summary>
        public int TotalChunks { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the directory containing this file.
        /// Null for files in the root directory.
        /// </summary>
        public string DirectoryId { get; set; }

        /// <summary>
        /// Initializes a new instance of the FileMetadata class.
        /// </summary>
        public FileMetadata()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            IsComplete = false;
            ChunksReceived = 0;
        }

        /// <summary>
        /// Initializes a new instance of the FileMetadata class with the specified parameters.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the file.</param>
        /// <param name="directoryId"></param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="filePath">The path where the file is stored.</param>
        public FileMetadata(string userId, string fileName, long fileSize, string contentType, string filePath) : this()
        {
            UserId = userId;
            FileName = fileName;
            FileSize = fileSize;
            ContentType = contentType;
            FilePath = filePath;
        }

        /// <summary>
        /// Updates the metadata to mark a chunk as received.
        /// </summary>
        public void AddChunk()
        {
            ChunksReceived++;
            UpdatedAt = DateTime.Now;
            
            // Check if all chunks have been received
            if (ChunksReceived >= TotalChunks)
            {
                IsComplete = true;
            }
        }
        
        /// <summary>
        /// Updates the metadata when the file is moved to a different directory.
        /// </summary>
        /// <param name="directoryId">The ID of the directory the file is moved to, or null for root directory.</param>
        /// <param name="newFilePath">The new file path.</param>
        public void MoveToDirectory(string directoryId, string newFilePath)
        {
            DirectoryId = directoryId;
            FilePath = newFilePath;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Marks the file as complete.
        /// </summary>
        public void MarkComplete()
        {
            IsComplete = true;
            UpdatedAt = DateTime.Now;
        }
    }
}