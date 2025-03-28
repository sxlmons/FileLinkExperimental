using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServerClient
{
    public class Program
    {
        // Configuration
        private const string ServerAddress = "localhost";
        private const int ServerPort = 9000;
        private const int ChunkSize = 1024 * 1024; // 1MB chunks

        // Client state
        private static TcpClient _client;
        private static NetworkStream _stream;
        private static string _userId = "";
        private static PacketFactory _packetFactory = new PacketFactory();
        private static PacketSerializer _packetSerializer = new PacketSerializer();

        public static async Task Main(string[] args)
        {
            try
            {
                // Connect to the server
                Console.WriteLine("Connecting to server...");
                _client = new TcpClient();
                await _client.ConnectAsync(ServerAddress, ServerPort);
                _stream = _client.GetStream();
                Console.WriteLine("Connected to server");

                // Create account
                await CreateAccount("testuser", "password12345", "test@example.com");

                // Login
                await Login("testuser", "password12345");

                // Create a directory
                string directoryId = await CreateDirectory("My Test Directory");
                Console.WriteLine($"Created directory with ID: {directoryId}");

                // Upload a file to the directory
                string testFilePath = "testfile.txt";
                CreateTestFile(testFilePath, 1024 * 1024 * 2); // 2MB test file
                await UploadFile(testFilePath, directoryId);

                // List directory contents to verify
                await ListDirectoryContents(directoryId);

                // Cleanup
                File.Delete(testFilePath);
                _client.Close();
                Console.WriteLine("Test completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task CreateAccount(string username, string password, string email)
        {
            Console.WriteLine($"Creating account for {username}...");
            
            var accountInfo = new
            {
                Username = username,
                Password = password,
                Email = email
            };
            
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(accountInfo);
            
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_REQUEST,
                Payload = payload
            };
            
            var response = await SendAndReceivePacket(packet);
            
            if (response.CommandCode == Commands.CommandCode.CREATE_ACCOUNT_RESPONSE)
            {
                var responseData = JsonSerializer.Deserialize<CreateAccountResponse>(response.Payload);
                
                if (responseData.Success)
                {
                    Console.WriteLine("Account created successfully");
                }
                else
                {
                    throw new Exception($"Account creation failed: {responseData.Message}");
                }
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }

        private static async Task Login(string username, string password)
        {
            Console.WriteLine($"Logging in as {username}...");
            
            var loginInfo = new
            {
                Username = username,
                Password = password
            };
            
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(loginInfo);
            
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_REQUEST,
                Payload = payload
            };
            
            var response = await SendAndReceivePacket(packet);
            
            if (response.CommandCode == Commands.CommandCode.LOGIN_RESPONSE)
            {
                var responseData = JsonSerializer.Deserialize<LoginResponse>(response.Payload);
                
                if (responseData.Success)
                {
                    _userId = response.UserId;
                    Console.WriteLine($"Login successful. User ID: {_userId}");
                }
                else
                {
                    throw new Exception($"Login failed: {responseData.Message}");
                }
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }

        private static async Task<string> CreateDirectory(string directoryName, string parentDirectoryId = null)
        {
            Console.WriteLine($"Creating directory '{directoryName}'...");
            
            var directoryInfo = new
            {
                DirectoryName = directoryName,
                ParentDirectoryId = parentDirectoryId
            };
            
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(directoryInfo);
            
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CREATE_REQUEST,
                UserId = _userId,
                Payload = payload
            };
            
            var response = await SendAndReceivePacket(packet);
            
            if (response.CommandCode == Commands.CommandCode.DIRECTORY_CREATE_RESPONSE)
            {
                var responseData = JsonSerializer.Deserialize<DirectoryCreateResponse>(response.Payload);
                
                if (responseData.Success)
                {
                    Console.WriteLine($"Directory created: {responseData.DirectoryName} (ID: {responseData.DirectoryId})");
                    return responseData.DirectoryId;
                }
                else
                {
                    throw new Exception($"Directory creation failed: {responseData.Message}");
                }
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }

        private static async Task UploadFile(string filePath, string directoryId)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }
            
            string fileName = Path.GetFileName(filePath);
            long fileSize = new FileInfo(filePath).Length;
            string contentType = "application/octet-stream";
            
            Console.WriteLine($"Uploading file '{fileName}' ({fileSize} bytes) to directory {directoryId}...");
            
            // Initialize upload
            var initInfo = new
            {
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType
            };
            
            byte[] initPayload = JsonSerializer.SerializeToUtf8Bytes(initInfo);
            
            var initPacket = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST,
                UserId = _userId,
                Payload = initPayload
            };
            
            // Important: Add the directory ID to the metadata
            initPacket.Metadata["DirectoryId"] = directoryId;
            
            var initResponse = await SendAndReceivePacket(initPacket);
            
            if (initResponse.CommandCode != Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE)
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(initResponse.CommandCode)}");
            }
            
            var initResponseData = JsonSerializer.Deserialize<FileUploadInitResponse>(initResponse.Payload);
            
            if (!initResponseData.Success)
            {
                throw new Exception($"Upload initialization failed: {initResponseData.Message}");
            }
            
            string fileId = initResponseData.FileId;
            Console.WriteLine($"Upload initialized. File ID: {fileId}");
            
            // Upload chunks
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int totalChunks = (int)Math.Ceiling((double)fileSize / ChunkSize);
                byte[] buffer = new byte[ChunkSize];
                
                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    int bytesRead = await fs.ReadAsync(buffer, 0, ChunkSize);
                    
                    bool isLastChunk = chunkIndex == totalChunks - 1;
                    
                    // Create chunk packet
                    var chunkPacket = new Packet
                    {
                        CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
                        UserId = _userId,
                        Payload = bytesRead < ChunkSize ? buffer.AsSpan(0, bytesRead).ToArray() : buffer
                    };
                    
                    chunkPacket.Metadata["FileId"] = fileId;
                    chunkPacket.Metadata["ChunkIndex"] = chunkIndex.ToString();
                    chunkPacket.Metadata["IsLastChunk"] = isLastChunk.ToString();
                    
                    var chunkResponse = await SendAndReceivePacket(chunkPacket);
                    
                    if (chunkResponse.CommandCode != Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE)
                    {
                        throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(chunkResponse.CommandCode)}");
                    }
                    
                    var chunkResponseData = JsonSerializer.Deserialize<FileUploadChunkResponse>(chunkResponse.Payload);
                    
                    if (!chunkResponseData.Success)
                    {
                        throw new Exception($"Chunk upload failed: {chunkResponseData.Message}");
                    }
                    
                    Console.WriteLine($"Uploaded chunk {chunkIndex + 1}/{totalChunks} ({bytesRead} bytes)");
                }
            }
            
            // Complete upload
            var completePacket = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST,
                UserId = _userId
            };
            
            completePacket.Metadata["FileId"] = fileId;
            
            var completeResponse = await SendAndReceivePacket(completePacket);
            
            if (completeResponse.CommandCode != Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE)
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(completeResponse.CommandCode)}");
            }
            
            var completeResponseData = JsonSerializer.Deserialize<FileUploadCompleteResponse>(completeResponse.Payload);
            
            if (!completeResponseData.Success)
            {
                throw new Exception($"Upload completion failed: {completeResponseData.Message}");
            }
            
            Console.WriteLine("File uploaded successfully");
        }

        private static async Task ListDirectoryContents(string directoryId)
        {
            Console.WriteLine($"Listing contents of directory {directoryId}...");
            
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST,
                UserId = _userId
            };
            
            packet.Metadata["DirectoryId"] = directoryId;
            
            var response = await SendAndReceivePacket(packet);
            
            if (response.CommandCode == Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE)
            {
                var responseData = JsonSerializer.Deserialize<DirectoryContentsResponse>(response.Payload);
                
                Console.WriteLine($"Directory contents:");
                Console.WriteLine($"Files: {responseData.Files.Count}");
                foreach (var file in responseData.Files)
                {
                    Console.WriteLine($"  - {file.FileName} ({file.FileSize} bytes)");
                }
                
                Console.WriteLine($"Subdirectories: {responseData.Directories.Count}");
                foreach (var dir in responseData.Directories)
                {
                    Console.WriteLine($"  - {dir.Name}");
                }
            }
            else if (response.CommandCode == Commands.CommandCode.ERROR)
            {
                var errorData = JsonSerializer.Deserialize<ErrorResponse>(response.Payload);
                throw new Exception($"Error listing directory contents: {errorData.Message}");
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }

        private static async Task<Packet> SendAndReceivePacket(Packet packet)
        {
            await SendPacket(packet);
            return await ReceivePacket();
        }

        private static async Task SendPacket(Packet packet)
        {
            // Serialize the packet
            byte[] packetData = _packetSerializer.Serialize(packet);
            
            // Send the length prefix
            byte[] lengthPrefix = BitConverter.GetBytes(packetData.Length);
            await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            
            // Send the packet data
            await _stream.WriteAsync(packetData, 0, packetData.Length);
            await _stream.FlushAsync();
        }

        private static async Task<Packet> ReceivePacket()
        {
            // Read the packet length
            byte[] lengthBuffer = new byte[4];
            int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4);
            if (bytesRead < 4)
            {
                throw new Exception("Connection closed while reading packet length");
            }

            // Convert to integer
            int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (packetLength <= 0 || packetLength > 25 * 1024 * 1024) // 25MB max
            {
                throw new Exception($"Invalid packet length: {packetLength}");
            }

            // Read the packet data
            byte[] packetBuffer = new byte[packetLength];
            int totalBytesRead = 0;
            while (totalBytesRead < packetLength)
            {
                int bytesRemaining = packetLength - totalBytesRead;
                int readSize = Math.Min(bytesRemaining, 8192); // 8KB buffer
                
                bytesRead = await _stream.ReadAsync(packetBuffer, totalBytesRead, readSize);
                if (bytesRead == 0)
                {
                    throw new Exception("Connection closed while reading packet data");
                }
                
                totalBytesRead += bytesRead;
            }

            // Deserialize the packet
            return _packetSerializer.Deserialize(packetBuffer);
        }

        private static void CreateTestFile(string filePath, int size)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                byte[] buffer = new byte[4096];
                Random rng = new Random();
                
                int bytesWritten = 0;
                while (bytesWritten < size)
                {
                    rng.NextBytes(buffer);
                    int bytesToWrite = Math.Min(buffer.Length, size - bytesWritten);
                    fs.Write(buffer, 0, bytesToWrite);
                    bytesWritten += bytesToWrite;
                }
            }
            
            Console.WriteLine($"Created test file: {filePath} ({size} bytes)");
        }
    }

    // Response classes
    public class CreateAccountResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class DirectoryCreateResponse
    {
        public bool Success { get; set; }
        public string DirectoryId { get; set; }
        public string DirectoryName { get; set; }
        public string Message { get; set; }
    }

    public class FileUploadInitResponse
    {
        public bool Success { get; set; }
        public string FileId { get; set; }
        public string Message { get; set; }
    }

    public class FileUploadChunkResponse
    {
        public bool Success { get; set; }
        public string FileId { get; set; }
        public int ChunkIndex { get; set; }
        public string Message { get; set; }
    }

    public class FileUploadCompleteResponse
    {
        public bool Success { get; set; }
        public string FileId { get; set; }
        public string Message { get; set; }
    }

    public class DirectoryContentsResponse
    {
        public List<FileMetadata> Files { get; set; }
        public List<DirectoryMetadata> Directories { get; set; }
        public string DirectoryId { get; set; }
    }

    public class FileMetadata
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsComplete { get; set; }
        public string DirectoryId { get; set; }
    }

    public class DirectoryMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentDirectoryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsRoot { get; set; }
    }

    public class ErrorResponse
    {
        public int OriginalCommandCode { get; set; }
        public string Message { get; set; }
    }

    // Packet class and helpers (simplified for brevity)
    public class Packet
    {
        public int CommandCode { get; set; }
        public Guid PacketId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public byte[] Payload { get; set; }
    }

    public class PacketSerializer
    {
        // Simplified implementation
        public byte[] Serialize(Packet packet)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Write protocol version
            writer.Write((byte)1);

            // Write command code
            writer.Write(packet.CommandCode);

            // Write packet ID
            writer.Write(packet.PacketId.ToByteArray());

            // Write user ID
            byte[] userIdBytes = Encoding.UTF8.GetBytes(packet.UserId ?? string.Empty);
            writer.Write(userIdBytes.Length);
            writer.Write(userIdBytes);

            // Write timestamp (as ticks)
            writer.Write(packet.Timestamp.Ticks);

            // Write metadata
            writer.Write(packet.Metadata?.Count ?? 0);
            if (packet.Metadata != null)
            {
                foreach (var kvp in packet.Metadata)
                {
                    byte[] keyBytes = Encoding.UTF8.GetBytes(kvp.Key);
                    byte[] valueBytes = Encoding.UTF8.GetBytes(kvp.Value);

                    writer.Write(keyBytes.Length);
                    writer.Write(keyBytes);
                    writer.Write(valueBytes.Length);
                    writer.Write(valueBytes);
                }
            }

            // Write payload
            if (packet.Payload != null)
            {
                writer.Write(packet.Payload.Length);
                writer.Write(packet.Payload);
            }
            else
            {
                writer.Write(0);
            }

            return ms.ToArray();
        }

        public Packet Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var packet = new Packet();

            // Read protocol version
            byte version = reader.ReadByte();
            if (version != 1)
            {
                throw new Exception($"Unsupported protocol version: {version}");
            }

            // Read command code
            packet.CommandCode = reader.ReadInt32();

            // Read packet ID
            byte[] packetIdBytes = reader.ReadBytes(16);
            packet.PacketId = new Guid(packetIdBytes);

            // Read user ID
            int userIdLength = reader.ReadInt32();
            byte[] userIdBytes = reader.ReadBytes(userIdLength);
            packet.UserId = Encoding.UTF8.GetString(userIdBytes);

            // Read timestamp
            long timestampTicks = reader.ReadInt64();
            packet.Timestamp = new DateTime(timestampTicks);

            // Read metadata
            int metadataCount = reader.ReadInt32();
            packet.Metadata = new Dictionary<string, string>(metadataCount);
            for (int i = 0; i < metadataCount; i++)
            {
                int keyLength = reader.ReadInt32();
                byte[] keyBytes = reader.ReadBytes(keyLength);
                string key = Encoding.UTF8.GetString(keyBytes);

                int valueLength = reader.ReadInt32();
                byte[] valueBytes = reader.ReadBytes(valueLength);
                string value = Encoding.UTF8.GetString(valueBytes);

                packet.Metadata[key] = value;
            }

            // Read payload
            int payloadLength = reader.ReadInt32();
            if (payloadLength > 0)
            {
                packet.Payload = reader.ReadBytes(payloadLength);
            }

            return packet;
        }
    }

    public static class Commands
    {
        public static class CommandCode
        {
            // Authentication Commands (100-199)
            public const int LOGIN_REQUEST = 100;
            public const int LOGIN_RESPONSE = 101;
            public const int LOGOUT_REQUEST = 102;
            public const int LOGOUT_RESPONSE = 103;
            public const int CREATE_ACCOUNT_REQUEST = 110;
            public const int CREATE_ACCOUNT_RESPONSE = 111;

            // File Operations (200-299)
            public const int FILE_LIST_REQUEST = 200;
            public const int FILE_LIST_RESPONSE = 201;
            public const int FILE_UPLOAD_INIT_REQUEST = 210;
            public const int FILE_UPLOAD_INIT_RESPONSE = 211;
            public const int FILE_UPLOAD_CHUNK_REQUEST = 212;
            public const int FILE_UPLOAD_CHUNK_RESPONSE = 213;
            public const int FILE_UPLOAD_COMPLETE_REQUEST = 214;
            public const int FILE_UPLOAD_COMPLETE_RESPONSE = 215;
            public const int FILE_DOWNLOAD_INIT_REQUEST = 220;
            public const int FILE_DOWNLOAD_INIT_RESPONSE = 221;
            public const int FILE_DOWNLOAD_CHUNK_REQUEST = 222;
            public const int FILE_DOWNLOAD_CHUNK_RESPONSE = 223;
            public const int FILE_DOWNLOAD_COMPLETE_REQUEST = 224;
            public const int FILE_DOWNLOAD_COMPLETE_RESPONSE = 225;
            public const int FILE_DELETE_REQUEST = 230;
            public const int FILE_DELETE_RESPONSE = 231;

            // Directory Operations (240-249)
            public const int DIRECTORY_CREATE_REQUEST = 240;
            public const int DIRECTORY_CREATE_RESPONSE = 241;
            public const int DIRECTORY_LIST_REQUEST = 242;
            public const int DIRECTORY_LIST_RESPONSE = 243;
            public const int DIRECTORY_RENAME_REQUEST = 244;
            public const int DIRECTORY_RENAME_RESPONSE = 245;
            public const int DIRECTORY_DELETE_REQUEST = 246;
            public const int DIRECTORY_DELETE_RESPONSE = 247;
            public const int FILE_MOVE_REQUEST = 248;
            public const int FILE_MOVE_RESPONSE = 249;
            public const int DIRECTORY_CONTENTS_REQUEST = 250;
            public const int DIRECTORY_CONTENTS_RESPONSE = 251;

            // Status Responses (300-399)
            public const int SUCCESS = 300;
            public const int ERROR = 301;

            public static string GetCommandName(int code)
            {
                return code switch
                {
                    LOGIN_REQUEST => "LOGIN_REQUEST",
                    LOGIN_RESPONSE => "LOGIN_RESPONSE",
                    LOGOUT_REQUEST => "LOGOUT_REQUEST",
                    LOGOUT_RESPONSE => "LOGOUT_RESPONSE",
                    CREATE_ACCOUNT_REQUEST => "CREATE_ACCOUNT_REQUEST",
                    CREATE_ACCOUNT_RESPONSE => "CREATE_ACCOUNT_RESPONSE",
                    FILE_LIST_REQUEST => "FILE_LIST_REQUEST",
                    FILE_LIST_RESPONSE => "FILE_LIST_RESPONSE",
                    FILE_UPLOAD_INIT_REQUEST => "FILE_UPLOAD_INIT_REQUEST",
                    FILE_UPLOAD_INIT_RESPONSE => "FILE_UPLOAD_INIT_RESPONSE",
                    FILE_UPLOAD_CHUNK_REQUEST => "FILE_UPLOAD_CHUNK_REQUEST",
                    FILE_UPLOAD_CHUNK_RESPONSE => "FILE_UPLOAD_CHUNK_RESPONSE",
                    FILE_UPLOAD_COMPLETE_REQUEST => "FILE_UPLOAD_COMPLETE_REQUEST",
                    FILE_UPLOAD_COMPLETE_RESPONSE => "FILE_UPLOAD_COMPLETE_RESPONSE",
                    FILE_DOWNLOAD_INIT_REQUEST => "FILE_DOWNLOAD_INIT_REQUEST",
                    FILE_DOWNLOAD_INIT_RESPONSE => "FILE_DOWNLOAD_INIT_RESPONSE",
                    FILE_DOWNLOAD_CHUNK_REQUEST => "FILE_DOWNLOAD_CHUNK_REQUEST",
                    FILE_DOWNLOAD_CHUNK_RESPONSE => "FILE_DOWNLOAD_CHUNK_RESPONSE",
                    FILE_DOWNLOAD_COMPLETE_REQUEST => "FILE_DOWNLOAD_COMPLETE_REQUEST",
                    FILE_DOWNLOAD_COMPLETE_RESPONSE => "FILE_DOWNLOAD_COMPLETE_RESPONSE",
                    FILE_DELETE_REQUEST => "FILE_DELETE_REQUEST",
                    FILE_DELETE_RESPONSE => "FILE_DELETE_RESPONSE",
                    DIRECTORY_CREATE_REQUEST => "DIRECTORY_CREATE_REQUEST",
                    DIRECTORY_CREATE_RESPONSE => "DIRECTORY_CREATE_RESPONSE",
                    DIRECTORY_LIST_REQUEST => "DIRECTORY_LIST_REQUEST",
                    DIRECTORY_LIST_RESPONSE => "DIRECTORY_LIST_RESPONSE",
                    DIRECTORY_RENAME_REQUEST => "DIRECTORY_RENAME_REQUEST",
                    DIRECTORY_RENAME_RESPONSE => "DIRECTORY_RENAME_RESPONSE",
                    DIRECTORY_DELETE_REQUEST => "DIRECTORY_DELETE_REQUEST",
                    DIRECTORY_DELETE_RESPONSE => "DIRECTORY_DELETE_RESPONSE",
                    FILE_MOVE_REQUEST => "FILE_MOVE_REQUEST",
                    FILE_MOVE_RESPONSE => "FILE_MOVE_RESPONSE",
                    DIRECTORY_CONTENTS_REQUEST => "DIRECTORY_CONTENTS_REQUEST",
                    DIRECTORY_CONTENTS_RESPONSE => "DIRECTORY_CONTENTS_RESPONSE",
                    SUCCESS => "SUCCESS",
                    ERROR => "ERROR",
                    _ => $"UNKNOWN({code})"
                };
            }
        }
    }

    public class PacketFactory
    {
 /// <summary>
        /// Creates an account creation request packet.
        /// </summary>
        /// <param name="username">The username for the new account.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="email">The email address for the new account.</param>
        /// <returns>An account creation request packet.</returns>
        public Packet CreateAccountCreationRequest(string username, string password, string email)
        {
            var accountInfo = new { Username = username, Password = password, Email = email };
            var payload = JsonSerializer.SerializeToUtf8Bytes(accountInfo);

            return new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_REQUEST,
                Payload = payload
            };
        }
        
        /// <summary>
        /// Creates an account creation response packet.
        /// </summary>
        /// <param name="success">Whether the account creation was successful.</param>
        /// <param name="message">A message about the account creation result.</param>
        /// <param name="userId">The ID of the newly created user, if successful.</param>
        /// <returns>An account creation response packet.</returns>
        public Packet CreateAccountCreationResponse(bool success, string message, string userId = "")
        {
            var response = new 
            { 
                Success = success, 
                Message = message,
                UserId = userId
            };
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            
            return packet;
        }

        /// <summary>
        /// Creates a login request packet.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A login request packet.</returns>
        public Packet CreateLoginRequest(string username, string password)
        {
            var credentials = new { Username = username, Password = password };
            var payload = JsonSerializer.SerializeToUtf8Bytes(credentials);

            return new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_REQUEST,
                Payload = payload
            };
        }

        /// <summary>
        /// Creates a login response packet.
        /// </summary>
        /// <param name="success">Whether the login was successful.</param>
        /// <param name="message">A message about the login result.</param>
        /// <param name="userId">The user ID, if login was successful.</param>
        /// <returns>A login response packet.</returns>
        public Packet CreateLoginResponse(bool success, string message, string userId = "")
        {
            var response = new 
            { 
                Success = success, 
                Message = message
            };
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            
            return packet;
        }

        /// <summary>
        /// Creates a logout request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A logout request packet.</returns>
        public Packet CreateLogoutRequest(string userId)
        {
            return new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_REQUEST,
                UserId = userId
            };
        }

        /// <summary>
        /// Creates a logout response packet.
        /// </summary>
        /// <param name="success">Whether the logout was successful.</param>
        /// <param name="message">A message about the logout result.</param>
        /// <returns>A logout response packet.</returns>
        public Packet CreateLogoutResponse(bool success, string message)
        {
            var response = new 
            { 
                Success = success, 
                Message = message
            };
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            
            return packet;
        }

        /// <summary>
        /// Creates a file list request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file list request packet.</returns>
        public Packet CreateFileListRequest(string userId)
        {
            return new Packet
            {
                CommandCode = Commands.CommandCode.FILE_LIST_REQUEST,
                UserId = userId
            };
        }

        /// <summary>
        /// Creates a file list response packet.
        /// </summary>
        /// <param name="files">The list of file metadata.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file list response packet.</returns>
        public Packet CreateFileListResponse(IEnumerable<object> files, string userId)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(files);

            return new Packet
            {
                CommandCode = Commands.CommandCode.FILE_LIST_RESPONSE,
                UserId = userId,
                Payload = payload
            };
        }

        /// <summary>
        /// Creates a file upload initialization request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <returns>A file upload initialization request packet.</returns>
        public Packet CreateFileUploadInitRequest(string userId, string fileName, long fileSize, string contentType)
        {
            var initData = new
            {
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(initData);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST,
                UserId = userId,
                Payload = payload,
                Metadata =
                {
                    ["FileName"] = fileName,
                    ["FileSize"] = fileSize.ToString(),
                    ["ContentType"] = contentType
                }
            };

            return packet;
        }

        /// <summary>
        /// Creates a file upload initialization response packet.
        /// </summary>
        /// <param name="success">Whether the initialization was successful.</param>
        /// <param name="fileId">The ID assigned to the file.</param>
        /// <param name="message">A message about the initialization result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file upload initialization response packet.</returns>
        public Packet CreateFileUploadInitResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file upload chunk request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="isLastChunk">Whether this is the last chunk.</param>
        /// <param name="data">The chunk data.</param>
        /// <returns>A file upload chunk request packet.</returns>
        public Packet CreateFileUploadChunkRequest(string userId, string fileId, int chunkIndex, bool isLastChunk, byte[] data)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
                UserId = userId,
                Payload = data
            };

            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();
            packet.Metadata["IsLastChunk"] = isLastChunk.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a file upload chunk response packet.
        /// </summary>
        /// <param name="success">Whether the chunk was successfully processed.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="message">A message about the chunk processing result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file upload chunk response packet.</returns>
        public Packet CreateFileUploadChunkResponse(bool success, string fileId, int chunkIndex, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                ChunkIndex = chunkIndex,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a file upload complete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file upload complete request packet.</returns>
        public Packet CreateFileUploadCompleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file upload complete response packet.
        /// </summary>
        /// <param name="success">Whether the upload was successfully completed.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="message">A message about the completion result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file upload complete response packet.</returns>
        public Packet CreateFileUploadCompleteResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file download initialization request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file download initialization request packet.</returns>
        public Packet CreateFileDownloadInitRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file download initialization response packet.
        /// </summary>
        /// <param name="success">Whether the initialization was successful.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="totalChunks">The total number of chunks.</param>
        /// <param name="message">A message about the initialization result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file download initialization response packet.</returns>
        public Packet CreateFileDownloadInitResponse(bool success, string fileId, string fileName, 
            long fileSize, string contentType, int totalChunks, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                TotalChunks = totalChunks,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_RESPONSE,
                UserId = userId,
                Payload = payload,
                Metadata =
                {
                    ["Success"] = success.ToString(),
                    ["FileId"] = fileId,
                    ["FileName"] = fileName,
                    ["FileSize"] = fileSize.ToString(),
                    ["ContentType"] = contentType,
                    ["TotalChunks"] = totalChunks.ToString()
                }
            };

            return packet;
        }

        /// <summary>
        /// Creates a file download chunk request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk to download.</param>
        /// <returns>A file download chunk request packet.</returns>
        public Packet CreateFileDownloadChunkRequest(string userId, string fileId, int chunkIndex)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a file download chunk response packet.
        /// </summary>
        /// <param name="success">Whether the chunk was successfully retrieved.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="isLastChunk">Whether this is the last chunk.</param>
        /// <param name="data">The chunk data.</param>
        /// <param name="message">A message about the chunk retrieval result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file download chunk response packet.</returns>
        public Packet CreateFileDownloadChunkResponse(bool success, string fileId, int chunkIndex, 
            bool isLastChunk, byte[] data, string message, string userId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_RESPONSE,
                UserId = userId,
                Payload = data
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();
            packet.Metadata["IsLastChunk"] = isLastChunk.ToString();
            packet.Metadata["Message"] = message;

            return packet;
        }

        /// <summary>
        /// Creates a file download complete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file download complete request packet.</returns>
        public Packet CreateFileDownloadCompleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file download complete response packet.
        /// </summary>
        /// <param name="success">Whether the download was successfully completed.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="message">A message about the completion result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file download complete response packet.</returns>
        public Packet CreateFileDownloadCompleteResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file delete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileId">The file ID.</param>
        /// <returns>A file delete request packet.</returns>
        public Packet CreateFileDeleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DELETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates a file delete response packet.
        /// </summary>
        /// <param name="success">Whether the file was successfully deleted.</param>
        /// <param name="fileId">The file ID.</param>
        /// <param name="message">A message about the deletion result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file delete response packet.</returns>
        public Packet CreateFileDeleteResponse(bool success, string fileId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DELETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileId"] = fileId;

            return packet;
        }

        /// <summary>
        /// Creates an error response packet.
        /// </summary>
        /// <param name="originalCommandCode">The command code of the request that caused the error.</param>
        /// <param name="message">The error message.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>An error response packet.</returns>
        public Packet CreateErrorResponse(int originalCommandCode, string message, string userId = "")
        {
            var response = new
            {
                OriginalCommandCode = originalCommandCode,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.ERROR,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["OriginalCommandCode"] = originalCommandCode.ToString();
            packet.Metadata["OriginalCommandName"] = Commands.CommandCode.GetCommandName(originalCommandCode);

            return packet;
        }
        
        /// <summary>
        /// Creates a directory creation request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryName">The name of the directory to create.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for a root directory.</param>
        /// <returns>A directory creation request packet.</returns>
        public Packet CreateDirectoryCreateRequest(string userId, string directoryName, string parentDirectoryId = null)
        {
            var directoryInfo = new
            {
                DirectoryName = directoryName,
                ParentDirectoryId = parentDirectoryId
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(directoryInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CREATE_REQUEST,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["DirectoryName"] = directoryName;
            if (!string.IsNullOrEmpty(parentDirectoryId))
            {
                packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory creation response packet.
        /// </summary>
        /// <param name="success">Whether the directory creation was successful.</param>
        /// <param name="directoryId">The ID of the new directory, if successful.</param>
        /// <param name="directoryName">The name of the new directory.</param>
        /// <param name="message">A message about the directory creation result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory creation response packet.</returns>
        public Packet CreateDirectoryCreateResponse(bool success, string directoryId, string directoryName, string message, string userId)
        {
            var response = new
            {
                Success = success,
                DirectoryId = directoryId,
                DirectoryName = directoryName,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CREATE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["DirectoryName"] = directoryName;

            return packet;
        }

        /// <summary>
        /// Creates a directory list request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <returns>A directory list request packet.</returns>
        public Packet CreateDirectoryListRequest(string userId, string parentDirectoryId = null)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_LIST_REQUEST,
                UserId = userId
            };

            if (!string.IsNullOrEmpty(parentDirectoryId))
            {
                packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory list response packet.
        /// </summary>
        /// <param name="directories">The list of directory metadata.</param>
        /// <param name="parentDirectoryId">The parent directory ID, or null for root directories.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory list response packet.</returns>
        public Packet CreateDirectoryListResponse(IEnumerable<object> directories, string parentDirectoryId, string userId)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(directories);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_LIST_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            if (!string.IsNullOrEmpty(parentDirectoryId))
            {
                packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
            }

            packet.Metadata["Count"] = directories is ICollection<object> collection ? collection.Count.ToString() : "unknown";

            return packet;
        }

        /// <summary>
        /// Creates a directory rename request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The ID of the directory to rename.</param>
        /// <param name="newName">The new name for the directory.</param>
        /// <returns>A directory rename request packet.</returns>
        public Packet CreateDirectoryRenameRequest(string userId, string directoryId, string newName)
        {
            var renameInfo = new
            {
                DirectoryId = directoryId,
                NewName = newName
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(renameInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_RENAME_REQUEST,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["NewName"] = newName;

            return packet;
        }

        /// <summary>
        /// Creates a directory rename response packet.
        /// </summary>
        /// <param name="success">Whether the rename operation was successful.</param>
        /// <param name="directoryId">The ID of the renamed directory.</param>
        /// <param name="newName">The new name of the directory.</param>
        /// <param name="message">A message about the rename result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory rename response packet.</returns>
        public Packet CreateDirectoryRenameResponse(bool success, string directoryId, string newName, string message, string userId)
        {
            var response = new
            {
                Success = success,
                DirectoryId = directoryId,
                NewName = newName,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_RENAME_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["NewName"] = newName;

            return packet;
        }

        /// <summary>
        /// Creates a directory delete request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The ID of the directory to delete.</param>
        /// <param name="recursive">Whether to delete the directory recursively.</param>
        /// <returns>A directory delete request packet.</returns>
        public Packet CreateDirectoryDeleteRequest(string userId, string directoryId, bool recursive)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_DELETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["DirectoryId"] = directoryId;
            packet.Metadata["Recursive"] = recursive.ToString();

            return packet;
        }

        /// <summary>
        /// Creates a directory delete response packet.
        /// </summary>
        /// <param name="success">Whether the delete operation was successful.</param>
        /// <param name="directoryId">The ID of the deleted directory.</param>
        /// <param name="message">A message about the delete result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory delete response packet.</returns>
        public Packet CreateDirectoryDeleteResponse(bool success, string directoryId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                DirectoryId = directoryId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_DELETE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["DirectoryId"] = directoryId;

            return packet;
        }

        /// <summary>
        /// Creates a file move request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileIds">The IDs of the files to move.</param>
        /// <param name="targetDirectoryId">The ID of the target directory, or null for the root directory.</param>
        /// <returns>A file move request packet.</returns>
        public Packet CreateFileMoveRequest(string userId, IEnumerable<string> fileIds, string targetDirectoryId)
        {
            var moveInfo = new
            {
                FileIds = fileIds,
                TargetDirectoryId = targetDirectoryId
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(moveInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_MOVE_REQUEST,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["FileCount"] = fileIds is ICollection<string> collection ? collection.Count.ToString() : "unknown";
            if (!string.IsNullOrEmpty(targetDirectoryId))
            {
                packet.Metadata["TargetDirectoryId"] = targetDirectoryId;
            }
            else
            {
                packet.Metadata["TargetDirectoryId"] = "root";
            }

            return packet;
        }

        /// <summary>
        /// Creates a file move response packet.
        /// </summary>
        /// <param name="success">Whether the move operation was successful.</param>
        /// <param name="fileCount">The number of files moved.</param>
        /// <param name="targetDirectoryId">The ID of the target directory, or null for the root directory.</param>
        /// <param name="message">A message about the move result.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A file move response packet.</returns>
        public Packet CreateFileMoveResponse(bool success, int fileCount, string targetDirectoryId, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileCount = fileCount,
                TargetDirectoryId = targetDirectoryId,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_MOVE_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["Success"] = success.ToString();
            packet.Metadata["FileCount"] = fileCount.ToString();
            if (!string.IsNullOrEmpty(targetDirectoryId))
            {
                packet.Metadata["TargetDirectoryId"] = targetDirectoryId;
            }
            else
            {
                packet.Metadata["TargetDirectoryId"] = "root";
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory contents request packet.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <returns>A directory contents request packet.</returns>
        public Packet CreateDirectoryContentsRequest(string userId, string directoryId = null)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST,
                UserId = userId
            };

            if (!string.IsNullOrEmpty(directoryId))
            {
                packet.Metadata["DirectoryId"] = directoryId;
            }
            else
            {
                packet.Metadata["DirectoryId"] = "root";
            }

            return packet;
        }

        /// <summary>
        /// Creates a directory contents response packet.
        /// </summary>
        /// <param name="files">The list of file metadata in the directory.</param>
        /// <param name="directories">The list of subdirectory metadata in the directory.</param>
        /// <param name="directoryId">The directory ID, or null for the root directory.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>A directory contents response packet.</returns>
        public Packet CreateDirectoryContentsResponse(IEnumerable<object> files, IEnumerable<object> directories, string directoryId, string userId)
        {
            var contentsInfo = new
            {
                Files = files,
                Directories = directories,
                DirectoryId = directoryId
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(contentsInfo);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE,
                UserId = userId,
                Payload = payload
            };

            packet.Metadata["FileCount"] = files is ICollection<object> fileCollection ? fileCollection.Count.ToString() : "unknown";
            packet.Metadata["DirectoryCount"] = directories is ICollection<object> dirCollection ? dirCollection.Count.ToString() : "unknown";
            
            if (!string.IsNullOrEmpty(directoryId))
            {
                packet.Metadata["DirectoryId"] = directoryId;
            }
            else
            {
                packet.Metadata["DirectoryId"] = "root";
            }

            return packet;
        }    }
}