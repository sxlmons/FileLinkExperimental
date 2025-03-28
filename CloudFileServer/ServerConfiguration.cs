namespace CloudFileServer
{
    /// <summary>
    /// Contains configuration settings for the CloudFileServer.
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        /// Gets or sets the TCP port the server will listen on.
        /// </summary>
        public int Port { get; set; } = 9000;

        /// <summary>
        /// Gets or sets the path to the directory where user data is stored.
        /// </summary>
        public string UsersDataPath { get; set; } = "data/users";

        /// <summary>
        /// Gets or sets the path to the directory where file metadata is stored.
        /// </summary>
        public string FileMetadataPath { get; set; } = "data/metadata";

        /// <summary>
        /// Gets or sets the path to the directory where uploaded files are stored.
        /// </summary>
        public string FileStoragePath { get; set; } = "data/files";

        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        public string LogFilePath { get; set; } = "logs/server.log";

        /// <summary>
        /// Gets or sets the maximum number of concurrent client connections.
        /// </summary>
        public int MaxConcurrentClients { get; set; } = 100;

        /// <summary>
        /// Gets or sets the size of file chunks in bytes.
        /// </summary>
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Gets or sets the session timeout in minutes.
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets the buffer size for network operations.
        /// </summary>
        public int NetworkBufferSize { get; set; } = 8192; // 8KB

        /// <summary>
        /// Gets or sets whether debug logging is enabled.
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log packet contents.
        /// </summary>
        public bool LogPacketContents { get; set; } = false;

        /// <summary>
        /// Creates a new instance of the ServerConfiguration class with default values.
        /// </summary>
        public ServerConfiguration()
        {
        }

        /// <summary>
        /// Validates the configuration settings to ensure they are valid.
        /// </summary>
        /// <returns>True if the configuration is valid, otherwise false.</returns>
        public bool Validate()
        {
            // Port must be a valid TCP port
            if (Port < 1 || Port > 65535)
                return false;

            // Paths must not be empty
            if (string.IsNullOrWhiteSpace(UsersDataPath) ||
                string.IsNullOrWhiteSpace(FileMetadataPath) ||
                string.IsNullOrWhiteSpace(FileStoragePath) ||
                string.IsNullOrWhiteSpace(LogFilePath))
                return false;

            // MaxConcurrentClients must be positive
            if (MaxConcurrentClients <= 0)
                return false;

            // ChunkSize must be positive
            if (ChunkSize <= 0)
                return false;

            // SessionTimeoutMinutes must be positive
            if (SessionTimeoutMinutes <= 0)
                return false;

            // NetworkBufferSize must be positive
            if (NetworkBufferSize <= 0)
                return false;

            return true;
        }

        /// <summary>
        /// Ensures all required directories exist.
        /// </summary>
        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(UsersDataPath);
            Directory.CreateDirectory(FileMetadataPath);
            Directory.CreateDirectory(FileStoragePath);
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
        }
    }
}