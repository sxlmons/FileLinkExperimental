using CloudFileServer.Authentication;
using CloudFileServer.FileManagement;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using System;
using System.Collections.Generic;

namespace CloudFileServer.Commands
{
    /// <summary>
    /// Factory for creating command handlers.
    /// Implements the Factory pattern to create the appropriate handler for each command code.
    /// </summary>
    public class CommandHandlerFactory
    {
        private readonly AuthenticationService _authService;
        private readonly FileService _fileService;
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly List<ICommandHandler> _handlers = new List<ICommandHandler>();

        /// <summary>
        /// Initializes a new instance of the CommandHandlerFactory class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="fileService">The file service.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="logService">The logging service.</param>
        public CommandHandlerFactory(
            AuthenticationService authService, 
            FileService fileService, 
            DirectoryService directoryService,
            LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Register all command handlers
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// Registers the default set of command handlers.
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            // Authentication handlers
            RegisterHandler(new LoginCommandHandler(_authService, _logService));
            RegisterHandler(new LogoutCommandHandler(_authService, _logService));
            RegisterHandler(new CreateAccountCommandHandler(_authService, _logService));
            
            // File operation handlers
            RegisterHandler(new FileListCommandHandler(_fileService, _logService));
            
            // Updated to pass DirectoryService to FileUploadCommandHandler
            RegisterHandler(new FileUploadCommandHandler(_fileService, _directoryService, _logService));
            RegisterHandler(new FileDownloadCommandHandler(_fileService, _logService));
            RegisterHandler(new FileDeleteCommandHandler(_fileService, _logService));
            RegisterHandler(new FileMoveCommandHandler(_directoryService, _logService));
            
            // Directory operation handlers
            RegisterHandler(new DirectoryCreateCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryListCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryRenameCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryDeleteCommandHandler(_directoryService, _logService));
            RegisterHandler(new FileMoveCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryContentsCommandHandler(_directoryService, _logService));
        }

        /// <summary>
        /// Creates a command handler for the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <returns>A command handler that can handle the specified command code, or null if none is found.</returns>
        public ICommandHandler CreateHandler(int commandCode)
        {
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(commandCode))
                {
                    return handler;
                }
            }

            _logService.Warning($"No handler found for command code: {CloudFileServer.Protocol.Commands.CommandCode.GetCommandName(commandCode)} ({commandCode})");
            return null;
        }

        /// <summary>
        /// Registers a command handler.
        /// </summary>
        /// <param name="handler">The command handler to register.</param>
        public void RegisterHandler(ICommandHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.Add(handler);
            _logService.Debug($"Registered command handler: {handler.GetType().Name}");
        }
    }
}