namespace CloudFileClient.Models
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// Creates a new user instance with the specified parameters.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <param name="username">The username.</param>
        /// <param name="email">The email address.</param>
        public User(string id, string username, string email = "")
        {
            Id = id;
            Username = username;
            Email = email;
        }
    }
}