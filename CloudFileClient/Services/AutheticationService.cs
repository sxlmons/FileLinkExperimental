using CloudFileClient.Models;
using CloudFileClient.Protocol;
using System;
using System.Threading.Tasks;
using CloudFileServer.Protocol;

namespace CloudFileClient.Services
{
    /// <summary>
    /// Provides authentication services for the client.
    /// </summary>
    public class AuthenticationService
    {
        private readonly NetworkService _networkService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        private User? _currentUser;

        /// <summary>
        /// Gets the currently logged in user, if any.
        /// </summary>
        public User? CurrentUser => _currentUser;

        /// <summary>
        /// Gets a value indicating whether the user is logged in.
        /// </summary>
        public bool IsLoggedIn => _currentUser != null;

        /// <summary>
        /// Initializes a new instance of the AuthenticationService class.
        /// </summary>
        /// <param name="networkService">The network service to use for communication.</param>
        public AuthenticationService(NetworkService networkService)
        {
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        }

        /// <summary>
        /// Attempts to create a new user account.
        /// </summary>
        /// <param name="username">The username for the new account.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="email">The email address for the new account.</param>
        /// <returns>A tuple containing the success flag, a message, and the user ID if successful.</returns>
        public async Task<(bool Success, string Message, string UserId)> CreateAccountAsync(string username, string password, string email = "")
        {
            try
            {
                // Create the account creation request packet
                var packet = _packetFactory.CreateAccountCreationRequest(username, password, email);
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);
                
                if (response == null)
                    return (false, "No response from server", "");
                
                // Extract the response data
                var (success, message, userId) = _packetFactory.ExtractAccountCreationResponse(response);
                
                return (success, message, userId);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating account: {ex.Message}", "");
            }
        }

        /// <summary>
        /// Attempts to log in with the provided credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A tuple containing the success flag and a message.</returns>
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            try
            {
                // Create the login request packet
                var packet = _packetFactory.CreateLoginRequest(username, password);
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);
                
                if (response == null)
                    return (false, "No response from server");
                
                // Extract the response data
                var (success, message, userId) = _packetFactory.ExtractLoginResponse(response);
                
                if (success && !string.IsNullOrEmpty(userId))
                {
                    // Store the current user
                    _currentUser = new User(userId, username);
                }
                
                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error during login: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to log out the current user.
        /// </summary>
        /// <returns>A tuple containing the success flag and a message.</returns>
        public async Task<(bool Success, string Message)> LogoutAsync()
        {
            try
            {
                if (!IsLoggedIn)
                    return (false, "Not logged in");
                
                // Create the logout request packet
                var packet = _packetFactory.CreateLogoutRequest(_currentUser!.Id);
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);
                
                if (response == null)
                    return (false, "No response from server");
                
                // Extract the response data
                var (success, message) = _packetFactory.ExtractLogoutResponse(response);
                
                if (success)
                {
                    // Clear the current user
                    _currentUser = null;
                }
                
                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error during logout: {ex.Message}");
            }
        }
    }
}