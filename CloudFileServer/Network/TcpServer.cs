using CloudFileServer.Commands;
using CloudFileServer.Services.Logging;
using CloudFileServer.SessionState;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CloudFileServer.Network
{
    /// <summary>
    /// TCP server that listens for client connections and manages the client sessions.
    /// </summary>
    public class TcpServer : IDisposable
    {
        private readonly int _port;
        private readonly TcpListener _listener;
        private readonly ClientSessionManager _clientManager;
        private readonly LogService _logService;
        private readonly CommandHandlerFactory _commandHandlerFactory;
        private readonly SessionStateFactory _sessionStateFactory;
        private readonly ServerConfiguration _config;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the TcpServer class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="logService">The logging service.</param>
        /// <param name="clientManager">The client session manager.</param>
        /// <param name="commandHandlerFactory">The command handler factory.</param>
        /// <param name="sessionStateFactory">The session state factory.</param>
        /// <param name="config">The server configuration.</param>
        public TcpServer(
            int port,
            LogService logService,
            ClientSessionManager clientManager,
            CommandHandlerFactory commandHandlerFactory,
            SessionStateFactory sessionStateFactory,
            ServerConfiguration config)
        {
            _port = port;
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _commandHandlerFactory = commandHandlerFactory ?? throw new ArgumentNullException(nameof(commandHandlerFactory));
            _sessionStateFactory = sessionStateFactory ?? throw new ArgumentNullException(nameof(sessionStateFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // Create the TCP listener
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        /// <summary>
        /// Starts the TCP server to listen for client connections.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Start()
        {
            if (_isRunning)
                throw new InvalidOperationException("Server is already running.");

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                _listener.Start();
                _logService.Info($"Server started on port {_port}");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _logService.Debug("Waiting for client connection...");
                        
                        // Wait for a client connection
                        var client = await _listener.AcceptTcpClientAsync();
                        
                        // Configure the client
                        client.ReceiveBufferSize = _config.NetworkBufferSize;
                        client.SendBufferSize = _config.NetworkBufferSize;
                        client.NoDelay = true; // Disable Nagle's algorithm for responsiveness
                        
                        _logService.Info($"Client connected from {client.Client.RemoteEndPoint}");
                        
                        // Handle the client connection in a separate task
                        _ = Task.Run(() => HandleClientConnection(client, token), token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Server is shutting down
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logService.Error($"Error accepting client connection: {ex.Message}", ex);
                    }
                }
            }
            finally
            {
                _isRunning = false;
                _listener.Stop();
                _logService.Info("Server stopped");
            }
        }

        /// <summary>
        /// Stops the TCP server.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                _logService.Info("Stopping server...");
                
                // Cancel all operations
                _cancellationTokenSource?.Cancel();
                
                // Wait for all clients to disconnect
                await _clientManager.DisconnectAllSessions("Server shutting down");
                
                // Stop the listener
                _listener.Stop();
                
                _isRunning = false;
                _logService.Info("Server stopped");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error stopping server: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles a client connection.
        /// </summary>
        /// <param name="client">The TCP client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task HandleClientConnection(TcpClient client, CancellationToken cancellationToken)
        {
            // Create a new client session
            var session = new ClientSession(
                client,
                _logService,
                _sessionStateFactory,
                _commandHandlerFactory,
                _config,
                cancellationToken);

            try
            {
                // Add the session to the manager
                if (!_clientManager.AddSession(session))
                {
                    _logService.Warning("Failed to add session to manager. Closing connection.");
                    await session.Disconnect("Session manager rejected the connection");
                    return;
                }

                // Start the session processing loop
                await session.StartSession();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling client session: {ex.Message}", ex);
            }
            finally
            {
                // Remove the session from the manager
                _clientManager.RemoveSession(session.SessionId);
                
                // Dispose the session
                session.Dispose();
            }
        }

        /// <summary>
        /// Disposes resources used by the TCP server.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Stop the server
                if (_isRunning)
                {
                    Stop().Wait();
                }
                
                // Dispose the cancellation token source
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error disposing server: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}