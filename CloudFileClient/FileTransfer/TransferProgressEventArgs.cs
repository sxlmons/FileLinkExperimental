using CloudFileClient.Models;

namespace CloudFileClient.FileTransfer;

/// <summary>
/// Provides data for the TransferProgressChanged event.
/// </summary>
public class TransferProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the transfer progress.
    /// </summary>
    public TransferProgress Progress { get; }
        
    /// <summary>
    /// Gets the time when the progress was reported.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the TransferProgressEventArgs class.
    /// </summary>
    /// <param name="progress">The transfer progress.</param>
    public TransferProgressEventArgs(TransferProgress progress)
    {
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        Timestamp = DateTime.Now;
    }
}