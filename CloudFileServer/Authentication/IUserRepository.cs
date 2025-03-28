using System.Threading.Tasks;

namespace CloudFileServer.Authentication
{
    /// <summary>
    /// Interface for the user repository.
    /// Implements the Repository pattern for user data storage.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user, or null if not found.</returns>
        Task<User> GetUserById(string userId);

        /// <summary>
        /// Gets a user by username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user, or null if not found.</returns>
        Task<User> GetUserByUsername(string username);

        /// <summary>
        /// Adds a new user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>True if the user was added successfully, otherwise false.</returns>
        Task<bool> AddUser(User user);

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>True if the user was updated successfully, otherwise false.</returns>
        Task<bool> UpdateUser(User user);

        /// <summary>
        /// Validates a user's credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The user if credentials are valid, otherwise null.</returns>
        Task<User> ValidateCredentials(string username, string password);
    }
}