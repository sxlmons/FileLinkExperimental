using System;

namespace CloudFileClient.Models
{
    /// <summary>
    /// Represents a file in the cloud storage.
    /// </summary>
    public class FileItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the file.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        /// Gets or sets the ID of the directory containing this file.
        /// Null for files in the root directory.
        /// </summary>
        public string? DirectoryId { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the content type (MIME type) of the file.
        /// </summary>
        public string ContentType { get; set; } = "";

        /// <summary>
        /// Gets or sets the date and time when the file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets the formatted file size as a string (KB, MB, GB).
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                else
                    return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        public string Extension
        {
            get
            {
                return Path.GetExtension(FileName).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets the file type icon based on its extension.
        /// </summary>
        public string FileTypeIcon
        {
            get
            {
                string ext = Extension;
                
                return ext switch
                {
                    ".pdf" => "pdf_icon.png",
                    ".doc" or ".docx" => "doc_icon.png",
                    ".xls" or ".xlsx" => "xls_icon.png",
                    ".ppt" or ".pptx" => "ppt_icon.png",
                    ".txt" => "txt_icon.png",
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "image_icon.png",
                    ".mp3" or ".wav" or ".ogg" or ".flac" => "audio_icon.png",
                    ".mp4" or ".avi" or ".mov" or ".wmv" => "video_icon.png",
                    ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "archive_icon.png",
                    _ => "generic_file_icon.png",
                };
            }
        }
    }
}