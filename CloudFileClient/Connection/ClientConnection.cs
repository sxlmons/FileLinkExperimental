using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CloudFileClient.Core.Exceptions;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Connection
{
    /// <summary>
    /// Manages the TCP connection to the server and handles packet sending and receiving.
    /// </summary>
    public class ClientConnection : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly LogService _logService;
        private readonly PacketSerializer _packetSerializer = new PacketSerializer();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);
        private readonly ConnectionRetryPolicy _retryPolicy;
        private string _currentHost;
        private int _currentPort;
        private bool _disposed = false;
        
        /// <summary>
        /// Gets a value indicating whether the client is connected.
        /// </summary>
        public bool IsConnected => _client?.Connected ?? false;
        
        /// <summary>
        /// Event raised when the connection status changes.
        /// </summary>
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
        
        /// <summary>
        /// Maximum allowed packet size in bytes.
        /// </summary>
        private const int MaxPacketSize = 25 * 1024 * 1024; // 25MB
        
        /// <summary>
        /// Network buffer size for read/write operations.
        /// </summary>
        private const int NetworkBufferSize = 8192; // 8KB

        /// <summary>
        /// Initializes a new instance of the ClientConnection class.
        /// </summary>
        /// <param name="logService">The logging service.</param>
        public ClientConnection(LogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _retryPolicy = new ConnectionRetryPolicy(logService);
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <param name="host">The server host.</param>
        /// <param name="port">The server port.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ConnectAsync(string host, int port)
        {
            if (IsConnected)
            {
                _logService.Warning("Already connected to server. Disconnecting first.");
                await DisconnectAsync();
            }

            try
            {
                _logService.Info($"Connecting to server {host}:{port}...");
                
                // Create a new TcpClient
                _client = new TcpClient();
                _client.ReceiveBufferSize = NetworkBufferSize;
                _client.SendBufferSize = NetworkBufferSize;
                _client.NoDelay = true; // Disable Nagle's algorithm for responsiveness
                
                // Connect to the server
                await _client.ConnectAsync(host, port);
                
                // Get the network stream
                _stream = _client.GetStream();
                
                // Store the connection details
                _currentHost = host;
                _currentPort = port;
                
                _logService.Info($"Connected to server {host}:{port}");
                
                // Raise the connection status changed event
                OnConnectionStatusChanged(new ConnectionStatusEventArgs(true));
            }
            catch (Exception ex)
            {
                _logService.Error($"Error connecting to server {host}:{port}: {ex.Message}", ex);
                
                // Clean up resources
                _client?.Dispose();
                _client = null;
                _stream = null;
                
                // Raise the connection status changed event
                OnConnectionStatusChanged(new ConnectionStatusEventArgs(false, $"Failed to connect to server: {ex.Message}", ex));
                
                // Rethrow the exception
                throw;
            }
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                _logService.Info("Disconnecting from server...");
                
                // Close the connection
                _client.Close();
                
                // Dispose resources
                _stream?.Dispose();
                _stream = null;
                _client?.Dispose();
                _client = null;
                
                _logService.Info("Disconnected from server");
                
                // Raise the connection status changed event
                OnConnectionStatusChanged(new ConnectionStatusEventArgs(false));
                
                // Make sure we release any pending locks
                await Task.Delay(100);
                if (_sendLock.CurrentCount == 0)
                    _sendLock.Release();
                if (_receiveLock.CurrentCount == 0)
                    _receiveLock.Release();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error disconnecting from server: {ex.Message}", ex);
                
                // Still need to clean up resources
                _stream?.Dispose();
                _stream = null;
                _client?.Dispose();
                _client = null;
                
                // Raise the connection status changed event
                OnConnectionStatusChanged(new ConnectionStatusEventArgs(false, $"Error during disconnect: {ex.Message}", ex));
                
                // Rethrow the exception
                throw;
            }
        }

        /// <summary>
        /// Attempts to reconnect to the server.
        /// </summary>
        /// <param name="attempts">The number of reconnection attempts.</param>
        /// <returns>True if reconnection was successful, otherwise false.</returns>
        public async Task<bool> TryReconnectAsync(int attempts = 3)
        {
            if (string.IsNullOrEmpty(_currentHost) || _currentPort <= 0)
            {
                _logService.Warning("Cannot reconnect without previous connection details.");
                return false;
            }

            try
            {
                // Disconnect first if still connected
                if (IsConnected)
                {
                    await DisconnectAsync();
                }

                // Try to reconnect with retry policy
                await _retryPolicy.ExecuteWithRetryAsync(
                    async () =>
                    {
                        await ConnectAsync(_currentHost, _currentPort);
                        return true;
                    },
                    "Reconnect");
                
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Failed to reconnect after {attempts} attempts: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Sends a packet to the server and receives a response.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>The response packet.</returns>
        public async Task<Packet> SendAndReceiveAsync(Packet packet)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to server.");
            
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            try
            {
                // Send the packet
                await SendPacketAsync(packet);
                
                // Receive the response
                return await ReceivePacketAsync();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error in SendAndReceive: {ex.Message}", ex);
                
                // Check if the connection is lost
                if (!IsConnected)
                {
                    _logService.Warning("Connection lost during SendAndReceive.");
                    
                    // Raise the connection status changed event
                    OnConnectionStatusChanged(new ConnectionStatusEventArgs(false, "Connection lost during operation.", ex));
                }
                
                throw;
            }
        }

        /// <summary>
        /// Sends a packet to the server.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendPacketAsync(Packet packet)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to server.");
            
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            await _sendLock.WaitAsync();
            try
            {
                // Log the packet
                _logService.LogPacket(packet, true);
                
                // Serialize the packet
                byte[] packetData = _packetSerializer.Serialize(packet);
                
                // Create a buffer with the packet length prefix
                byte[] lengthPrefix = BitConverter.GetBytes(packetData.Length);
                
                // Send the length prefix
                await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                
                // Send the packet data
                await _stream.WriteAsync(packetData, 0, packetData.Length);
                
                // Flush the stream
                await _stream.FlushAsync();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Receives a packet from the server.
        /// </summary>
        /// <returns>The received packet.</returns>
        public async Task<Packet> ReceivePacketAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to server.");

            await _receiveLock.WaitAsync();
            try
            {
                // Read the packet length (4 bytes)
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4);
                if (bytesRead < 4)
                {
                    _logService.Warning($"Connection closed while reading packet length: {bytesRead} bytes read");
                    await DisconnectAsync();
                    throw new IOException("Connection closed by server.");
                }

                // Convert to integer (packet length)
                int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (packetLength <= 0 || packetLength > MaxPacketSize)
                {
                    _logService.Warning($"Invalid packet length received: {packetLength}");
                    throw new ProtocolException($"Invalid packet length: {packetLength}");
                }

                // Read the packet data
                byte[] packetBuffer = new byte[packetLength];
                int totalBytesRead = 0;
                while (totalBytesRead < packetLength)
                {
                    int bytesRemaining = packetLength - totalBytesRead;
                    int readSize = Math.Min(bytesRemaining, NetworkBufferSize);
                    
                    bytesRead = await _stream.ReadAsync(
                        packetBuffer, 
                        totalBytesRead, 
                        readSize);
                    
                    if (bytesRead == 0)
                    {
                        _logService.Warning("Connection closed while reading packet data");
                        await DisconnectAsync();
                        throw new IOException("Connection closed by server while reading packet data.");
                    }
                    
                    totalBytesRead += bytesRead;
                }

                // Deserialize the packet
                var packet = _packetSerializer.Deserialize(packetBuffer);
                
                // Log the packet
                _logService.LogPacket(packet, false);
                
                return packet;
            }
            catch (Exception ex) when (!(ex is ProtocolException || ex is IOException))
            {
                _logService.Error($"Error receiving packet: {ex.Message}", ex);
                
                // Check if we're still connected
                if (IsConnected)
                {
                    // Try to disconnect cleanly
                    await DisconnectAsync();
                }
                
                throw;
            }
            finally
            {
                _receiveLock.Release();
            }
        }

        /// <summary>
        /// Raises the ConnectionStatusChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Disposes resources used by the client connection.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Disconnect if connected
                if (IsConnected)
                {
                    DisconnectAsync().Wait();
                }
                
                // Dispose resources
                _stream?.Dispose();
                _client?.Dispose();
                _sendLock?.Dispose();
                _receiveLock?.Dispose();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error disposing client connection: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}