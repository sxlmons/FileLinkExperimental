using CloudFileServer.Core.Exceptions;
using CloudFileServer.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFileServer.Authentication
{
    /// <summary>
    /// Repository for user data storage.
    /// Implements the Repository pattern with file-based storage.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly string _usersPath;
        private readonly object _lock = new object();
        private Dictionary<string, User> _users = new Dictionary<string, User>();
        private readonly LogService _logService;

        /// <summary>
        /// Initializes a new instance of the UserRepository class.
        /// </summary>
        /// <param name="usersPath">The path to the directory where user data is stored.</param>
        /// <param name="logService">The logging service.</param>
        public UserRepository(string usersPath, LogService logService)
        {
            _usersPath = usersPath ?? throw new ArgumentNullException(nameof(usersPath));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Ensure the users directory exists
            Directory.CreateDirectory(_usersPath);
            
            // Load users from storage
            LoadUsers().Wait();
        }

        /// <summary>
        /// Gets a user by ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user, or null if not found.</returns>
        public Task<User> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<User>(null);

            lock (_lock)
            {
                _users.TryGetValue(userId, out User user);
                return Task.FromResult(user);
            }
        }

        /// <summary>
        /// Gets a user by username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user, or null if not found.</returns>
        public Task<User> GetUserByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Task.FromResult<User>(null);

            lock (_lock)
            {
                var user = _users.Values.FirstOrDefault(u => 
                    string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(user);
            }
        }

        /// <summary>
        /// Adds a new user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>True if the user was added successfully, otherwise false.</returns>
        public async Task<bool> AddUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username cannot be empty.", nameof(user));

            // Check if a user with the same username already exists
            var existingUser = await GetUserByUsername(user.Username);
            if (existingUser != null)
            {
                _logService.Warning($"Attempted to add user with existing username: {user.Username}");
                return false;
            }

            lock (_lock)
            {
                _users[user.Id] = user;
            }

            // Save changes to storage
            await SaveUsers();
            
            _logService.Info($"User added: {user.Username} (ID: {user.Id})");
            return true;
        }

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>True if the user was updated successfully, otherwise false.</returns>
        public async Task<bool> UpdateUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (string.IsNullOrEmpty(user.Id))
                throw new ArgumentException("User ID cannot be empty.", nameof(user));

            lock (_lock)
            {
                if (!_users.ContainsKey(user.Id))
                {
                    _logService.Warning($"Attempted to update non-existent user: {user.Id}");
                    return false;
                }

                _users[user.Id] = user;
            }

            // Save changes to storage
            await SaveUsers();
            
            _logService.Debug($"User updated: {user.Username} (ID: {user.Id})");
            return true;
        }

        /// <summary>
        /// Validates a user's credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The user if credentials are valid, otherwise null.</returns>
        public async Task<User> ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = await GetUserByUsername(username);
            if (user == null)
                return null;

            // Verify the password
            if (VerifyPassword(password, user.PasswordSalt, user.PasswordHash))
            {
                // Update last login time
                user.UpdateLastLogin();
                await UpdateUser(user);
                
                return user;
            }

            return null;
        }

        /// <summary>
        /// Creates a new user with the specified username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="email">The email address.</param>
        /// <param name="role">The user's role.</param>
        /// <returns>The created user, or null if creation failed.</returns>
        public async Task<User> CreateUser(string username, string password, string email, string role)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new ArgumentException("Username and password cannot be empty.");
            
            // Check if the username is already taken
            var existingUser = await GetUserByUsername(username);
            if (existingUser != null)
            {
                _logService.Warning($"Attempted to create user with existing username: {username}");
                return null;
            }

            // Create a new user
            var user = new User(username, email, role);
            
            // Generate a salt and hash the password
            user.PasswordSalt = GenerateSalt();
            user.PasswordHash = HashPassword(password, user.PasswordSalt);
            
            // Add the user to the repository
            bool success = await AddUser(user);
            
            if (success)
            {
                _logService.Info($"User created: {username} (ID: {user.Id})");
                return user;
            }
            
            return null;
        }

        /// <summary>
        /// Saves all users to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task SaveUsers()
        {
            try
            {
                string filePath = Path.Combine(_usersPath, "users.json");
                
                // Create a copy of the users dictionary to avoid holding the lock during file I/O
                Dictionary<string, User> usersCopy;
                lock (_lock)
                {
                    usersCopy = new Dictionary<string, User>(_users);
                }
                
                // Convert to a list for serialization
                var usersList = usersCopy.Values.ToList();
                
                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string json = JsonSerializer.Serialize(usersList, options);
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json);
                
                _logService.Debug($"Users saved to {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error saving users: {ex.Message}", ex);
                throw new AuthenticationException("Failed to save users.", ex);
            }
        }

        /// <summary>
        /// Loads all users from storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task LoadUsers()
        {
            try
            {
                string filePath = Path.Combine(_usersPath, "users.json");
                
                if (!File.Exists(filePath))
                {
                    _logService.Info($"Users file not found at {filePath}. Creating a new one.");
                    
                    // Create an admin user if no users file exists
                    await CreateDefaultAdminUser();
                    
                    return;
                }
                
                // Read the file
                string json = await File.ReadAllTextAsync(filePath);
                
                // Deserialize from JSON
                var usersList = JsonSerializer.Deserialize<List<User>>(json);
                
                // Build the dictionary
                Dictionary<string, User> usersDict = new Dictionary<string, User>();
                foreach (var user in usersList)
                {
                    if (!string.IsNullOrEmpty(user.Id))
                    {
                        usersDict[user.Id] = user;
                    }
                }
                
                // Update the users dictionary
                lock (_lock)
                {
                    _users = usersDict;
                }
                
                _logService.Info($"Loaded {_users.Count} users from {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error loading users: {ex.Message}", ex);
                
                // If we can't load the users, create a default admin user
                if (_users.Count == 0)
                {
                    await CreateDefaultAdminUser();
                }
            }
        }

        /// <summary>
        /// Creates a default admin user.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task CreateDefaultAdminUser()
        {
            try
            {
                // Create a default admin user
                var adminUser = new User("admin", "admin@example.com", "Admin");
                adminUser.PasswordSalt = GenerateSalt();
                adminUser.PasswordHash = HashPassword("admin", adminUser.PasswordSalt);
                
                // Add the admin user
                lock (_lock)
                {
                    _users[adminUser.Id] = adminUser;
                }
                
                // Save changes to storage
                await SaveUsers();
                
                _logService.Info($"Created default admin user: {adminUser.Username} (ID: {adminUser.Id})");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating default admin user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates a random salt for password hashing.
        /// </summary>
        /// <returns>The generated salt.</returns>
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Hashes a password with a salt using PBKDF2.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="salt">The salt to use.</param>
        /// <returns>The hashed password.</returns>
        private string HashPassword(string password, byte[] salt)
        {
            const int iterations = 10000;
            const int hashSize = 32; // 256 bits
            
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(hashSize);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifies a password against a hash.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="salt">The salt that was used to hash the password.</param>
        /// <param name="storedHash">The stored hash to compare against.</param>
        /// <returns>True if the password is correct, otherwise false.</returns>
        private bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || salt == null || string.IsNullOrEmpty(storedHash))
                return false;
            
            string computedHash = HashPassword(password, salt);
            return string.Equals(computedHash, storedHash);
        }
    }
}