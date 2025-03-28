using System;
using System.Collections.Generic;
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

        public static async Task Main(string[] args)
        {
            try
            {
                // Setup sample data
                await PopulateServerWithSampleData();
                
                // Start interactive file browser
                Console.WriteLine("\nStarting file browser...");
                Console.WriteLine("Enter username to browse files (or 'exit' to quit):");
                string username = Console.ReadLine();
                
                while (username.ToLower() != "exit")
                {
                    string password = "password123"; // For demo purposes
                    var browser = new FileBrowser(ServerAddress, ServerPort);
                    await browser.ConnectAndAuthenticate(username, password);
                    await browser.StartInteractiveBrowsing();
                    
                    Console.WriteLine("\nEnter username to browse files (or 'exit' to quit):");
                    username = Console.ReadLine();
                }
                
                Console.WriteLine("Goodbye!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task PopulateServerWithSampleData()
        {
            Console.WriteLine("Populating server with sample data...");
            
            // Create users
            var users = new[] { "alice", "bob", "charlie" };
            Dictionary<string, string> userCredentials = new Dictionary<string, string>();
            
            foreach (var user in users)
            {
                userCredentials[user] = "password123"; 
                await CreateUserIfNotExists(user, userCredentials[user], $"{user}@example.com");
            }
            
            // For each user, create a directory structure and upload files
            foreach (var user in users)
            {
                // Login
                var client = new TcpClient();
                await client.ConnectAsync(ServerAddress, ServerPort);
                var networkStream = client.GetStream();
                
                var session = new ClientSession(networkStream);
                await session.Login(user, userCredentials[user]);
                
                // Create directory structure based on user
                Dictionary<string, string> directories = new Dictionary<string, string>();
                
                // Root directories
                string docsDir = await session.CreateDirectory("Documents");
                string picsDir = await session.CreateDirectory("Pictures");
                string projDir = await session.CreateDirectory("Projects");
                
                directories["Documents"] = docsDir;
                directories["Pictures"] = picsDir;
                directories["Projects"] = projDir;
                
                // Subdirectories
                string personalsDir = await session.CreateDirectory("Personal", docsDir);
                string workDir = await session.CreateDirectory("Work", docsDir);
                
                directories["Personal"] = personalsDir;
                directories["Work"] = workDir;
                
                string vacationDir = await session.CreateDirectory("Vacation", picsDir);
                string familyDir = await session.CreateDirectory("Family", picsDir);
                
                directories["Vacation"] = vacationDir;
                directories["Family"] = familyDir;
                
                string projectADir = await session.CreateDirectory("ProjectA", projDir);
                string projectBDir = await session.CreateDirectory("ProjectB", projDir);
                
                directories["ProjectA"] = projectADir;
                directories["ProjectB"] = projectBDir;
                
                // Even deeper nesting for one user
                if (user == "alice")
                {
                    string hawaii2023Dir = await session.CreateDirectory("Hawaii2023", vacationDir);
                    directories["Hawaii2023"] = hawaii2023Dir;
                    
                    string italyDir = await session.CreateDirectory("Italy", vacationDir);
                    directories["Italy"] = italyDir;
                }
                
                // Upload some files to each directory
                await UploadSampleFiles(session, user, directories);
                
                // Close connection
                client.Close();
                Console.WriteLine($"Setup completed for user: {user}");
            }
            
            Console.WriteLine("Server populated with sample data!");
        }
        
        private static async Task UploadSampleFiles(ClientSession session, string user, Dictionary<string, string> directories)
        {
            // For each directory, create 1-3 sample files
            var random = new Random();
            
            foreach (var dirEntry in directories)
            {
                string dirName = dirEntry.Key;
                string dirId = dirEntry.Value;
                
                // Create 1-3 files
                int fileCount = random.Next(1, 4);
                
                for (int i = 1; i <= fileCount; i++)
                {
                    string fileName = $"{user}_{dirName}_file{i}.txt";
                    string filePath = CreateTempTextFile(fileName, $"This is a sample file {i} in {dirName} directory for user {user}.");
                    
                    try
                    {
                        await session.UploadFile(filePath, dirId);
                        Console.WriteLine($"Uploaded {fileName} to {dirName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error uploading {fileName}: {ex.Message}");
                    }
                    finally
                    {
                        // Clean up temp file
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }
            }
        }
        
        private static string CreateTempTextFile(string fileName, string content)
        {
            string filePath = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }
        
        private static async Task CreateUserIfNotExists(string username, string password, string email)
        {
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(ServerAddress, ServerPort);
                var networkStream = client.GetStream();
                
                var session = new ClientSession(networkStream);
                
                try
                {
                    // Try to login first to see if user exists
                    await session.Login(username, password);
                    Console.WriteLine($"User {username} already exists");
                }
                catch
                {
                    // If login fails, create the user
                    await session.CreateAccount(username, password, email);
                    Console.WriteLine($"Created user: {username}");
                }
                
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user {username}: {ex.Message}");
            }
        }
    }

    public class FileBrowser
    {
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private TcpClient _client;
        private NetworkStream _stream;
        private ClientSession _session;
        private string _currentDirectoryId;
        private string _currentDirectoryName;
        private Stack<(string id, string name)> _directoryHistory = new Stack<(string id, string name)>();
        
        public FileBrowser(string serverAddress, int serverPort)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
            _currentDirectoryId = null; // Root directory
            _currentDirectoryName = "root";
        }
        
        public async Task ConnectAndAuthenticate(string username, string password)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_serverAddress, _serverPort);
            _stream = _client.GetStream();
            
            _session = new ClientSession(_stream);
            await _session.Login(username, password);
            
            Console.WriteLine($"Connected and authenticated as {username}");
        }
        
        public async Task StartInteractiveBrowsing()
        {
            bool browsing = true;
            
            while (browsing)
            {
                await DisplayCurrentDirectory();
                
                Console.WriteLine("\nCommands:");
                Console.WriteLine("  cd <number> - Navigate to directory");
                Console.WriteLine("  up - Go to parent directory");
                Console.WriteLine("  back - Go to previous directory");
                Console.WriteLine("  view <number> - View file content");
                Console.WriteLine("  exit - Exit browser");
                
                Console.Write("\nEnter command: ");
                string command = Console.ReadLine();
                
                if (string.IsNullOrEmpty(command))
                    continue;
                
                string[] parts = command.Split(' ', 2);
                string cmd = parts[0].ToLower();
                
                switch (cmd)
                {
                    case "cd":
                        if (parts.Length > 1 && int.TryParse(parts[1], out int dirIndex))
                        {
                            await NavigateToDirectory(dirIndex);
                        }
                        else
                        {
                            Console.WriteLine("Invalid directory number");
                        }
                        break;
                        
                    case "up":
                        await NavigateUp();
                        break;
                        
                    case "back":
                        NavigateBack();
                        break;
                        
                    case "view":
                        if (parts.Length > 1 && int.TryParse(parts[1], out int fileIndex))
                        {
                            await ViewFile(fileIndex);
                        }
                        else
                        {
                            Console.WriteLine("Invalid file number");
                        }
                        break;
                        
                    case "exit":
                        browsing = false;
                        break;
                        
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
            
            // Close connection
            _client.Close();
        }
        
        private async Task DisplayCurrentDirectory()
        {
            Console.Clear();
            Console.WriteLine($"Current directory: {_currentDirectoryName}");
            Console.WriteLine(new string('-', 50));
            
            var contents = await _session.GetDirectoryContents(_currentDirectoryId);
            
            // Display directories
            Console.WriteLine("\nDirectories:");
            if (contents.Directories.Count == 0)
            {
                Console.WriteLine("  (none)");
            }
            else
            {
                for (int i = 0; i < contents.Directories.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {contents.Directories[i].Name}");
                }
            }
            
            // Display files
            Console.WriteLine("\nFiles:");
            if (contents.Files.Count == 0)
            {
                Console.WriteLine("  (none)");
            }
            else
            {
                for (int i = 0; i < contents.Files.Count; i++)
                {
                    var file = contents.Files[i];
                    Console.WriteLine($"  {i + 1}. {file.FileName} ({FormatFileSize(file.FileSize)})");
                }
            }
        }
        
        private async Task NavigateToDirectory(int index)
        {
            try
            {
                var contents = await _session.GetDirectoryContents(_currentDirectoryId);
                
                if (index <= 0 || index > contents.Directories.Count)
                {
                    Console.WriteLine("Invalid directory number");
                    return;
                }
                
                var selectedDir = contents.Directories[index - 1];
                
                // Save current directory in history
                _directoryHistory.Push((_currentDirectoryId, _currentDirectoryName));
                
                // Update current directory
                _currentDirectoryId = selectedDir.Id;
                _currentDirectoryName = selectedDir.Name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to directory: {ex.Message}");
            }
        }
        
        private async Task NavigateUp()
        {
            try
            {
                if (_currentDirectoryId == null)
                {
                    Console.WriteLine("Already at root directory");
                    return;
                }
                
                var contents = await _session.GetDirectoryContents(_currentDirectoryId);
                string parentId = null;
                
                // Find parent directory
                if (contents.Directories.Count > 0 && contents.Directories[0].ParentDirectoryId != null)
                {
                    parentId = contents.Directories[0].ParentDirectoryId;
                }
                
                // Save current directory in history
                _directoryHistory.Push((_currentDirectoryId, _currentDirectoryName));
                
                // Navigate to parent
                _currentDirectoryId = parentId;
                _currentDirectoryName = parentId == null ? "root" : "parent";
                
                // Get the actual parent directory name if available
                if (parentId != null)
                {
                    var parentContents = await _session.GetDirectoryContents(parentId);
                    foreach (var dir in parentContents.Directories)
                    {
                        if (dir.Id == parentId)
                        {
                            _currentDirectoryName = dir.Name;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating up: {ex.Message}");
            }
        }
        
        private void NavigateBack()
        {
            if (_directoryHistory.Count == 0)
            {
                Console.WriteLine("No previous directory");
                return;
            }
            
            var prevDir = _directoryHistory.Pop();
            _currentDirectoryId = prevDir.id;
            _currentDirectoryName = prevDir.name;
        }
        
        private async Task ViewFile(int index)
        {
            try
            {
                var contents = await _session.GetDirectoryContents(_currentDirectoryId);
                
                if (index <= 0 || index > contents.Files.Count)
                {
                    Console.WriteLine("Invalid file number");
                    return;
                }
                
                var selectedFile = contents.Files[index - 1];
                
                Console.WriteLine($"\nFile: {selectedFile.FileName}");
                Console.WriteLine($"Size: {FormatFileSize(selectedFile.FileSize)}");
                Console.WriteLine($"Created: {selectedFile.CreatedAt}");
                Console.WriteLine($"Updated: {selectedFile.UpdatedAt}");
                
                // We would need to implement file download and display content here
                Console.WriteLine("\nFile content not available in this demo");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error viewing file: {ex.Message}");
            }
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }

    public class ClientSession
    {
        private readonly NetworkStream _stream;
        private readonly PacketSerializer _packetSerializer = new PacketSerializer();
        private string _userId = "";
        
        public ClientSession(NetworkStream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }
        
        public async Task CreateAccount(string username, string password, string email)
        {
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
                
                if (!responseData.Success)
                {
                    throw new Exception($"Account creation failed: {responseData.Message}");
                }
            }
            else if (response.CommandCode == Commands.CommandCode.ERROR)
            {
                throw new Exception("Server returned an error for account creation request");
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }
        
        public async Task Login(string username, string password)
        {
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
                }
                else
                {
                    throw new Exception($"Login failed: {responseData.Message}");
                }
            }
            else if (response.CommandCode == Commands.CommandCode.ERROR)
            {
                throw new Exception("Server returned an error for login request");
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }
        
        public async Task<string> CreateDirectory(string directoryName, string parentDirectoryId = null)
        {
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
                    return responseData.DirectoryId;
                }
                else
                {
                    throw new Exception($"Directory creation failed: {responseData.Message}");
                }
            }
            else if (response.CommandCode == Commands.CommandCode.ERROR)
            {
                throw new Exception("Server returned an error for directory creation request");
            }
            else
            {
                throw new Exception($"Unexpected response: {Commands.CommandCode.GetCommandName(response.CommandCode)}");
            }
        }
        
        public async Task UploadFile(string filePath, string directoryId = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }
            
            string fileName = Path.GetFileName(filePath);
            long fileSize = new FileInfo(filePath).Length;
            string contentType = "application/octet-stream";
            
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
            
            // Add directory ID if specified
            if (!string.IsNullOrEmpty(directoryId))
            {
                initPacket.Metadata["DirectoryId"] = directoryId;
            }
            
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
            
            // Upload chunks
            const int chunkSize = 1024 * 1024; // 1MB
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);
                byte[] buffer = new byte[chunkSize];
                
                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    int bytesRead = await fs.ReadAsync(buffer, 0, chunkSize);
                    
                    bool isLastChunk = chunkIndex == totalChunks - 1;
                    
                    // Create chunk packet
                    var chunkPacket = new Packet
                    {
                        CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
                        UserId = _userId,
                        Payload = bytesRead < chunkSize ? buffer.AsSpan(0, bytesRead).ToArray() : buffer
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
        }
        
        public async Task<DirectoryContents> GetDirectoryContents(string directoryId = null)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST,
                UserId = _userId
            };
            
            if (!string.IsNullOrEmpty(directoryId))
            {
                packet.Metadata["DirectoryId"] = directoryId;
            }
            
            var response = await SendAndReceivePacket(packet);
            
            if (response.CommandCode == Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE)
            {
                return JsonSerializer.Deserialize<DirectoryContents>(response.Payload);
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
        
        private async Task<Packet> SendAndReceivePacket(Packet packet)
        {
            await SendPacket(packet);
            return await ReceivePacket();
        }
        
        private async Task SendPacket(Packet packet)
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
        
        private async Task<Packet> ReceivePacket()
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
    }

    // Response classes and shared types
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

    public class DirectoryContents
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

    // Packet classes
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
}