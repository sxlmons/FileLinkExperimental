using System;
using System.Text.Json;
using System.Threading.Tasks;
using CloudFileClient.Connection;
using CloudFileClient.Protocol;
using CloudFileClient.Utils;

namespace CloudFileClient.Authentication
{
    /// <summary>
    /// Provides authentication services for the client.
    /// </summary>
    public class ClientAuthenticationService
    {
        private readonly ClientConnection _connection;
        private readonly UserSession _userSession;
        private readonly LogService _logService;
        private readonly ClientPacketFactory _packetFactory;
        private readonly ResponseParser _responseParser;

        /// <summary>
        /// Initializes a new instance of the ClientAuthenticationService class.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        /// <param name="userSession">The user session.</param>
        /// <param name="logService">The logging service.</param>
        public ClientAuthenticationService(
            ClientConnection connection,
            UserSession userSession,
            LogService logService)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            _packetFactory = new ClientPacketFactory();
            _responseParser = new ResponseParser();
        }

        /// <summary>
        /// Attempts to login with the specified credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A tuple containing the success flag and error message if failed.</returns>
        public async Task<(bool Success, string ErrorMessage)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return (false, "Username and password are required.");

            try
            {
                // Create the login request packet
                var packet = _packetFactory.CreateLoginRequest(username, password);
                
                // Send the packet and get the response
                var response = await _connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var (success, userId, message) = _responseParser.ParseLoginResponse(response);
                
                if (success)
                {
                    // Authenticate the user
                    _userSession.Authenticate(userId, username);
                    return (true, null);
                }
                else
                {
                    return (false, message);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during login: {ex.Message}", ex);
                return (false, $"Login failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        /// <returns>A tuple containing the success flag and error message if failed.</returns>
        public async Task<(bool Success, string ErrorMessage)> LogoutAsync()
        {
            if (!_userSession.IsAuthenticated)
                return (true, null); // Already logged out

            try
            {
                // Create the logout request packet
                var packet = _packetFactory.CreateLogoutRequest(_userSession.UserId);
                
                // Send the packet and get the response
                var response = await _connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var (success, message) = _responseParser.ParseBasicResponse(response);
                
                // Log the user out locally regardless of server response
                _userSession.Logout();
                
                return (success, message);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during logout: {ex.Message}", ex);
                
                // Still log out locally in case of error
                _userSession.Logout();
                
                return (false, $"Logout failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="email">The email address.</param>
        /// <returns>A tuple containing the success flag, user ID, and error message if failed.</returns>
        public async Task<(bool Success, string UserId, string ErrorMessage)> CreateAccountAsync(
            string username, string password, string email = "")
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return (false, null, "Username and password are required.");

            try
            {
                // Create the account creation request packet
                var packet = _packetFactory.CreateAccountCreationRequest(username, password, email);
                
                // Send the packet and get the response
                var response = await _connection.SendAndReceiveAsync(packet);
                
                // Parse the response
                var (success, userId, message) = _responseParser.ParseAccountCreationResponse(response);
                
                return (success, userId, message);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating account: {ex.Message}", ex);
                return (false, null, $"Account creation failed: {ex.Message}");
            }
        }
    }
}