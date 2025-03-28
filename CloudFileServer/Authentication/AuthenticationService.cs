using CloudFileServer.Core.Exceptions;
using CloudFileServer.Services.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudFileServer.Authentication
{
    /// <summary>
    /// Service that provides authentication and user management functionality.
    /// </summary>
    public class AuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly LogService _logService;

        /// <summary>
        /// Initializes a new instance of the AuthenticationService class.
        /// </summary>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="logService">The logging service.</param>
        public AuthenticationService(IUserRepository userRepository, LogService logService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// Authenticates a user with a username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The authenticated user, or null if authentication failed.</returns>
        public async Task<User> Authenticate(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logService.Warning("Authentication attempt with empty username or password");
                    return null;
                }

                _logService.Debug($"Authentication attempt for username: {username}");
                
                // Validate credentials
                var user = await _userRepository.ValidateCredentials(username, password);
                
                if (user != null)
                {
                    _logService.Info($"User authenticated successfully: {username} (ID: {user.Id})");
                    
                    // Create user directory for file storage if it doesn't exist
                    EnsureUserDirectoryExists(user.Id);
                    
                    return user;
                }
                else
                {
                    _logService.Warning($"Authentication failed for username: {username}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during authentication: {ex.Message}", ex);
                throw new AuthenticationException("Authentication failed.", ex);
            }
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="role">The user's role.</param>
        /// <param name="email">The email address (optional).</param>
        /// <returns>The registered user, or null if registration failed.</returns>
        public async Task<User> RegisterUser(string username, string password, string role, string email = "")
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logService.Warning("Registration attempt with empty username or password");
                    return null;
                }

                // Check if a user with this username already exists
                var existingUser = await _userRepository.GetUserByUsername(username);
                if (existingUser != null)
                {
                    _logService.Warning($"Registration failed. Username already exists: {username}");
                    return null;
                }

                _logService.Info($"Registering new user: {username}");
                
                // Create the user
                var userRepository = _userRepository as UserRepository;
                if (userRepository == null)
                {
                    throw new AuthenticationException("User repository does not support user creation.");
                }
                
                var user = await userRepository.CreateUser(username, password, email, role);
                
                if (user != null)
                {
                    _logService.Info($"User registered successfully: {username} (ID: {user.Id})");
                    
                    // Create user directory for file storage
                    EnsureUserDirectoryExists(user.Id);
                    
                    return user;
                }
                else
                {
                    _logService.Warning($"Registration failed for username: {username}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during user registration: {ex.Message}", ex);
                throw new AuthenticationException("User registration failed.", ex);
            }
        }

        /// <summary>
        /// Ensures that the user's directory for file storage exists.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        private void EnsureUserDirectoryExists(string userId)
        {
            try
            {
                // Get the configuration
                var config = CloudFileServerApp.Configuration;
                if (config == null)
                {
                    _logService.Warning("Server configuration not available. Cannot create user directory.");
                    return;
                }

                // Create the user's directory
                string userDirectory = Path.Combine(config.FileStoragePath, userId);
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                    _logService.Debug($"Created user directory: {userDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error ensuring user directory exists: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a user by ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user, or null if not found.</returns>
        public Task<User> GetUserById(string userId)
        {
            return _userRepository.GetUserById(userId);
        }

        /// <summary>
        /// Gets a user by username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user, or null if not found.</returns>
        public Task<User> GetUserByUsername(string username)
        {
            return _userRepository.GetUserByUsername(username);
        }

        /// <summary>
        /// Updates a user.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>True if the user was updated successfully, otherwise false.</returns>
        public Task<bool> UpdateUser(User user)
        {
            return _userRepository.UpdateUser(user);
        }
    }
}