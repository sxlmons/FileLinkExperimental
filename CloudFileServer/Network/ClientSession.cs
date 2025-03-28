using CloudFileServer.Commands;
using CloudFileServer.Protocol;
using CloudFileServer.Services.Logging;
using CloudFileServer.SessionState;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CloudFileServer.Network
{
    /// <summary>
    /// Represents a client connection to the server.
    /// Manages the communication with the client and implements the session state machine.
    /// </summary>
    public class ClientSession : IDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private SessionState.SessionState _currentState;
        private readonly PacketSerializer _packetSerializer = new PacketSerializer();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);
        private bool _disposed = false;
        private readonly CancellationToken _cancellationToken;
        private readonly ServerConfiguration _config;

        /// <summary>
        /// Gets the unique identifier for this session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets or sets the ID of the authenticated user for this session.
        /// Empty if the session is not authenticated.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the session is authenticated.
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

        /// <summary>
        /// Gets the logging service.
        /// </summary>
        public LogService LogService { get; }

        /// <summary>
        /// Gets the command handler factory.
        /// </summary>
        public CommandHandlerFactory CommandHandlerFactory { get; }

        /// <summary>
        /// Gets the session state factory.
        /// </summary>
        public SessionStateFactory StateFactory { get; }

        /// <summary>
        /// Gets the time when the session was last active.
        /// </summary>
        public DateTime LastActivityTime { get; private set; }

        /// <summary>
        /// Maximum allowed packet size in bytes (5MB to accommodate large file chunks)
        /// </summary>
        private const int MaxPacketSize = 25 * 1024 * 1024;

        /// <summary>
        /// Initializes a new instance of the ClientSession class.
        /// </summary>
        /// <param name="client">The TCP client for this session.</param>
        /// <param name="logService">The logging service.</param>
        /// <param name="stateFactory">The session state factory.</param>
        /// <param name="commandHandlerFactory">The command handler factory.</param>
        /// <param name="config">The server configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ClientSession(
            TcpClient client,
            LogService logService,
            SessionStateFactory stateFactory,
            CommandHandlerFactory commandHandlerFactory,
            ServerConfiguration config,
            CancellationToken cancellationToken)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            LogService = logService ?? throw new ArgumentNullException(nameof(logService));
            StateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
            CommandHandlerFactory = commandHandlerFactory ?? throw new ArgumentNullException(nameof(commandHandlerFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cancellationToken = cancellationToken;
            
            _stream = client.GetStream();
            SessionId = Guid.NewGuid();
            LastActivityTime = DateTime.Now;
            
            // Set initial state to authentication required
            _currentState = StateFactory.CreateAuthRequiredState(this);
        }

        /// <summary>
        /// Starts the session processing loop.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StartSession()
        {
            try
            {
                // Enter the initial state
                await _currentState.OnEnter();
                
                LogService.Info($"Session started: {SessionId} from {GetClientAddress()}");
                
                // Process packets until cancelled or disconnected
                while (!_cancellationToken.IsCancellationRequested && _client.Connected)
                {
                    try
                    {
                        // Receive a packet
                        var packet = await ReceivePacket();
                        if (packet == null)
                        {
                            LogService.Debug($"Null packet received, client may have disconnected: {SessionId}");
                            break;
                        }

                        // Update last activity time
                        LastActivityTime = DateTime.Now;
                        
                        // Log the packet
                        LogService.LogPacket(packet, false, SessionId);
                        
                        // Process the packet
                        var response = await _currentState.HandlePacket(packet);
                        
                        // Send the response
                        if (response != null)
                        {
                            LogService.LogPacket(response, true, SessionId);
                            await SendPacket(response);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogService.Info($"Session operation cancelled: {SessionId}");
                        break;
                    }
                    catch (IOException ex)
                    {
                        LogService.Error($"IO error in session {SessionId}: {ex.Message}", ex);
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"Error processing packet in session {SessionId}: {ex.Message}", ex);
                        // Continue processing next packet unless disconnected
                        if (!_client.Connected)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Session loop terminated with error: {ex.Message}", ex);
            }
            finally
            {
                await Disconnect("Session loop terminated");
            }
        }

        /// <summary>
        /// Receives a packet from the client.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the received packet, or null if the connection is closed.</returns>
        public async Task<Packet> ReceivePacket()
        {
            await _receiveLock.WaitAsync(_cancellationToken);
            try
            {
                // Read the packet length (4 bytes)
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, _cancellationToken);
                if (bytesRead < 4)
                {
                    LogService.Debug($"Connection closed while reading packet length: {bytesRead} bytes read");
                    return null;
                }

                // Convert to integer (packet length)
                int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (packetLength <= 0 || packetLength > MaxPacketSize)
                {
                    LogService.Warning($"Invalid packet length received: {packetLength}");
                    return null;
                }

                // Read the packet data
                byte[] packetBuffer = new byte[packetLength];
                int totalBytesRead = 0;
                while (totalBytesRead < packetLength)
                {
                    int bytesRemaining = packetLength - totalBytesRead;
                    int readSize = Math.Min(bytesRemaining, _config.NetworkBufferSize);
                    
                    bytesRead = await _stream.ReadAsync(
                        packetBuffer, 
                        totalBytesRead, 
                        readSize,
                        _cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        LogService.Debug("Connection closed while reading packet data");
                        return null;
                    }
                    
                    totalBytesRead += bytesRead;
                }

                // Deserialize the packet
                var packet = _packetSerializer.Deserialize(packetBuffer);
                return packet;
            }
            finally
            {
                _receiveLock.Release();
            }
        }

        /// <summary>
        /// Sends a packet to the client.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendPacket(Packet packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            await _sendLock.WaitAsync(_cancellationToken);
            try
            {
                // Serialize the packet
                byte[] packetData = _packetSerializer.Serialize(packet);
                
                // Create a buffer with the packet length prefix
                byte[] lengthPrefix = BitConverter.GetBytes(packetData.Length);
                
                // Send the length prefix
                await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, _cancellationToken);
                
                // Send the packet data
                await _stream.WriteAsync(packetData, 0, packetData.Length, _cancellationToken);
                
                // Flush the stream
                await _stream.FlushAsync(_cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Transitions the session to a new state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void TransitionToState(SessionState.SessionState newState)
        {
            if (newState == null)
                throw new ArgumentNullException(nameof(newState));

            LogService.Debug($"Session {SessionId} transitioning from {_currentState.GetType().Name} to {newState.GetType().Name}");
            
            // Exit the current state
            _currentState.OnExit().Wait();
            
            // Set the new state
            _currentState = newState;
            
            // Enter the new state
            _currentState.OnEnter().Wait();
        }

        /// <summary>
        /// Disconnects the client session.
        /// </summary>
        /// <param name="reason">The reason for disconnection.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Disconnect(string reason)
        {
            if (_disposed)
                return;

            try
            {
                LogService.Info($"Disconnecting session {SessionId}: {reason}");
                
                // Ensure we're in the disconnecting state
                if (!(_currentState is DisconnectingState))
                {
                    TransitionToState(StateFactory.CreateDisconnectingState(this));
                }
                
                // Close the connection
                _client.Close();
            }
            catch (Exception ex)
            {
                LogService.Error($"Error during disconnect: {ex.Message}", ex);
            }
            finally
            {
                // Dispose resources
                Dispose();
            }
        }

        /// <summary>
        /// Gets the client's IP address and port.
        /// </summary>
        /// <returns>The client's IP address and port as a string.</returns>
        private string GetClientAddress()
        {
            try
            {
                return _client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Checks if the session has timed out.
        /// </summary>
        /// <param name="timeoutMinutes">The timeout period in minutes.</param>
        /// <returns>True if the session has timed out, otherwise false.</returns>
        public bool HasTimedOut(int timeoutMinutes)
        {
            return (DateTime.Now - LastActivityTime).TotalMinutes > timeoutMinutes;
        }

        /// <summary>
        /// Disposes resources used by the client session.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _stream?.Dispose();
                _client?.Dispose();
                _sendLock?.Dispose();
                _receiveLock?.Dispose();
            }
            catch (Exception ex)
            {
                LogService.Error($"Error disposing session: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}