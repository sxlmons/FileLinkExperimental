using System.Text.Json;
using CloudFileServer.Protocol;

namespace CloudFileClient.Protocol
{
    /// <summary>
    /// Factory for creating packets with specific command codes and payloads.
    /// </summary>
    public class PacketFactory
    {
        /// <summary>
        /// Creates an account creation request packet.
        /// </summary>
        /// <param name="username">The username for the new account.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="email">The email address for the new account.</param>
        /// <returns>An account creation request packet.</returns>
        public Packet CreateAccountCreationRequest(string username, string password, string email = "")
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
        /// Extracts login response data from a packet.
        /// </summary>
        /// <param name="packet">The login response packet.</param>
        /// <returns>Tuple containing success flag, message, and user ID.</returns>
        public (bool Success, string Message, string UserId) ExtractLoginResponse(Packet packet)
        {
            if (packet.CommandCode != Commands.CommandCode.LOGIN_RESPONSE)
                return (false, "Invalid packet type", "");

            if (packet.Payload == null || packet.Payload.Length == 0)
                return (false, "Empty response payload", "");

            try
            {
                var responseData = JsonSerializer.Deserialize<LoginResponse>(packet.Payload);
                return (responseData.Success, responseData.Message, packet.UserId);
            }
            catch
            {
                return (false, "Error parsing response", "");
            }
        }

        /// <summary>
        /// Extracts account creation response data from a packet.
        /// </summary>
        /// <param name="packet">The account creation response packet.</param>
        /// <returns>Tuple containing success flag, message, and user ID.</returns>
        public (bool Success, string Message, string UserId) ExtractAccountCreationResponse(Packet packet)
        {
            if (packet.CommandCode != Commands.CommandCode.CREATE_ACCOUNT_RESPONSE)
                return (false, "Invalid packet type", "");

            if (packet.Payload == null || packet.Payload.Length == 0)
                return (false, "Empty response payload", "");

            try
            {
                var responseData = JsonSerializer.Deserialize<AccountCreationResponse>(packet.Payload);
                return (responseData.Success, responseData.Message, responseData.UserId);
            }
            catch
            {
                return (false, "Error parsing response", "");
            }
        }

        /// <summary>
        /// Extracts logout response data from a packet.
        /// </summary>
        /// <param name="packet">The logout response packet.</param>
        /// <returns>Tuple containing success flag and message.</returns>
        public (bool Success, string Message) ExtractLogoutResponse(Packet packet)
        {
            if (packet.CommandCode != Commands.CommandCode.LOGOUT_RESPONSE)
                return (false, "Invalid packet type");

            if (packet.Payload == null || packet.Payload.Length == 0)
                return (false, "Empty response payload");

            try
            {
                var responseData = JsonSerializer.Deserialize<LogoutResponse>(packet.Payload);
                return (responseData.Success, responseData.Message);
            }
            catch
            {
                return (false, "Error parsing response");
            }
        }

        // Response object classes for deserialization
        private class LoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
        }

        private class AccountCreationResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string UserId { get; set; } = "";
        }

        private class LogoutResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
        }
    }
}