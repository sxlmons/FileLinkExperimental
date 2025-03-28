using System;

namespace CloudFileServer.Authentication
{
    /// <summary>
    /// Represents a user in the system.
    /// Contains user identity and authentication information.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the hashed password.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the salt used for password hashing.
        /// </summary>
        public byte[] PasswordSalt { get; set; }

        /// <summary>
        /// Gets or sets the user's role (e.g., "Admin", "User").
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the user last logged in.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the User class.
        /// </summary>
        private User()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            Role = "User"; // Default role
        }

        /// <summary>
        /// Initializes a new instance of the User class with the specified parameters.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="email">The email address.</param>
        /// <param name="role">The user's role.</param>
        public User(string username, string email, string role) : this()
        {
            Username = username;
            Email = email;
            Role = role;
        }

        /// <summary>
        /// Updates the user's last login time to the current time.
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.Now;
        }
    }
}