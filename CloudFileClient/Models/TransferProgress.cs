using System;

namespace CloudFileClient.Models
{
    /// <summary>
    /// Represents the progress of a file transfer.
    /// </summary>
    public class TransferProgress
    {
        /// <summary>
        /// Gets or sets the ID of the file being transferred.
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Gets or sets the name of the file being transferred.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the total size of the file in bytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes transferred so far.
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Gets or sets the index of the current chunk.
        /// </summary>
        public int CurrentChunk { get; set; }

        /// <summary>
        /// Gets or sets the total number of chunks.
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// Gets or sets the start time of the transfer.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time of the last update.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the transfer.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether the transfer is complete.
        /// </summary>
        public bool IsComplete => BytesTransferred >= TotalBytes;

        /// <summary>
        /// Gets the percentage completion of the transfer.
        /// </summary>
        public double PercentComplete => (double)BytesTransferred / TotalBytes * 100;

        /// <summary>
        /// Gets the transfer speed in bytes per second.
        /// </summary>
        public double BytesPerSecond
        {
            get
            {
                var elapsedSeconds = (DateTime.Now - StartTime).TotalSeconds;
                return elapsedSeconds > 0 ? BytesTransferred / elapsedSeconds : 0;
            }
        }

        /// <summary>
        /// Gets a formatted string representing the transfer speed.
        /// </summary>
        /// <returns>A human-readable transfer speed string.</returns>
        public string GetFormattedSpeed()
        {
            var bytesPerSecond = BytesPerSecond;
            
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond:F2} B/s";
            
            if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024:F2} KB/s";
            
            return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
        }

        /// <summary>
        /// Gets the estimated time remaining for the transfer.
        /// </summary>
        /// <returns>A TimeSpan representing the estimated time remaining.</returns>
        public TimeSpan GetEstimatedTimeRemaining()
        {
            if (BytesTransferred <= 0 || BytesPerSecond <= 0)
                return TimeSpan.MaxValue;

            var remainingBytes = TotalBytes - BytesTransferred;
            var remainingSeconds = remainingBytes / BytesPerSecond;
            
            return TimeSpan.FromSeconds(remainingSeconds);
        }

        /// <summary>
        /// Gets a formatted string representing the estimated time remaining.
        /// </summary>
        /// <returns>A human-readable time remaining string.</returns>
        public string GetFormattedTimeRemaining()
        {
            var timeRemaining = GetEstimatedTimeRemaining();
            
            if (timeRemaining == TimeSpan.MaxValue)
                return "Calculating...";
            
            if (timeRemaining.TotalHours >= 1)
                return $"{timeRemaining.TotalHours:F1} hours";
            
            if (timeRemaining.TotalMinutes >= 1)
                return $"{timeRemaining.TotalMinutes:F1} minutes";
            
            return $"{timeRemaining.TotalSeconds:F0} seconds";
        }

        /// <summary>
        /// Updates the progress with a new chunk.
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="chunkSize">The size of the chunk in bytes.</param>
        public void UpdateProgress(int chunkIndex, int chunkSize)
        {
            CurrentChunk = chunkIndex;
            BytesTransferred = Math.Min(TotalBytes, (long)(chunkIndex + 1) * chunkSize);
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// Marks the transfer as complete.
        /// </summary>
        public void Complete()
        {
            BytesTransferred = TotalBytes;
            EndTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
        }
    }
}