using CloudFileServer;
using CloudFileServer.Authentication;
using CloudFileServer.Commands;
using CloudFileServer.FileManagement;
using CloudFileServer.Network;
using CloudFileServer.Services.Logging;
using CloudFileServer.SessionState;
using System;
using System.Threading.Tasks;

namespace CloudFileServer
{
    /// <summary>
    /// Main application class that initializes and coordinates all components of the Cloud File Server.
    /// </summary>
    public class CloudFileServerApp
    {
        private FileRepository _fileRepository;
        private FileService _fileService;
        private DirectoryRepository _directoryRepository;
        private DirectoryService _directoryService;
        private TcpServer _tcpServer;
        private ClientSessionManager _clientSessionManager;
        private LogService _logService;
        private FileLogger _fileLogger;
        private IUserRepository _userRepository;
        private AuthenticationService _authService;
        private SessionStateFactory _sessionStateFactory;
        private CommandHandlerFactory _commandHandlerFactory;
        private bool _initialized = false;

        /// <summary>
        /// Gets the server configuration.
        /// </summary>
        public static ServerConfiguration Configuration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the CloudFileServerApp class.
        /// </summary>
        /// <param name="config">The server configuration.</param>
        public CloudFileServerApp(ServerConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Initializes all components of the application.
        /// </summary>
       public void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                // Ensure directories exist
                Configuration.EnsureDirectoriesExist();

                // Initialize logging
                _fileLogger = new FileLogger(Configuration.LogFilePath);
                _logService = new LogService(_fileLogger);
                _logService.Info("Cloud File Server starting...");
                _logService.Info($"Server configuration: Port={Configuration.Port}, MaxConcurrentClients={Configuration.MaxConcurrentClients}");

                // Initialize the physical storage service first
                var storageService = new PhysicalStorageService(Configuration.FileStoragePath, _logService);

                // Initialize repositories in correct order
                _directoryRepository = new DirectoryRepository(Configuration.FileMetadataPath, _logService);
                
                // FileRepository now depends on DirectoryRepository
                _fileRepository = new FileRepository(Configuration.FileMetadataPath, Configuration.FileStoragePath, _directoryRepository, _logService);
                _userRepository = new UserRepository(Configuration.UsersDataPath, _logService);

                // Initialize authentication service
                _authService = new AuthenticationService(_userRepository, _logService);

                // Initialize directory and file services with PhysicalStorageService
                _directoryService = new DirectoryService(_directoryRepository, _fileRepository, storageService, _logService);
                _fileService = new FileService(_fileRepository, storageService, _logService, Configuration.ChunkSize);

                // Initialize client session management
                _clientSessionManager = new ClientSessionManager(_logService, Configuration);

                // Initialize state and command factories
                _sessionStateFactory = new SessionStateFactory(_authService, _fileService, _directoryService, _logService);
                _commandHandlerFactory = new CommandHandlerFactory(_authService, _fileService, _directoryService, _logService);

                // Initialize TCP server
                _tcpServer = new TcpServer(Configuration.Port, _logService, _clientSessionManager, _commandHandlerFactory, _sessionStateFactory, Configuration);

                _initialized = true;
                _logService.Info("Cloud File Server initialized successfully");
            }
            catch (Exception ex)
            {
                _logService?.Fatal("Failed to initialize Cloud File Server", ex);
                throw;
            }
}

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Start()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Server must be initialized before starting.");
            }

            try
            {
                _logService.Info("Starting Cloud File Server...");
                await _tcpServer.Start();
            }
            catch (Exception ex)
            {
                _logService.Fatal("Failed to start Cloud File Server", ex);
                throw;
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Stop()
        {
            try
            {
                _logService.Info("Stopping Cloud File Server...");
                
                // Stop the server components in reverse order
                if (_tcpServer != null)
                {
                    await _tcpServer.Stop();
                }
                
                if (_clientSessionManager != null)
                {
                    await _clientSessionManager.DisconnectAllSessions("Server shutting down");
                }
                
                _logService.Info("Cloud File Server stopped");
            }
            catch (Exception ex)
            {
                _logService.Error("Error stopping Cloud File Server", ex);
                throw;
            }
        }
    }
}