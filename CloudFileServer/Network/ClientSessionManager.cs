using CloudFileServer.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudFileServer.Network
{
    /// <summary>
    /// Manages all active client sessions connected to the server.
    /// Provides functionality for tracking, monitoring, and cleaning up sessions.
    /// </summary>
    public class ClientSessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new ConcurrentDictionary<Guid, ClientSession>();
        private readonly LogService _logService;
        private readonly ServerConfiguration _config;
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the ClientSessionManager class.
        /// </summary>
        /// <param name="logService">The logging service.</param>
        /// <param name="config">The server configuration.</param>
        public ClientSessionManager(LogService logService, ServerConfiguration config)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // Start a timer to periodically clean up inactive sessions
            _cleanupTimer = new Timer(
                CleanupTimerCallback, 
                null, 
                TimeSpan.FromMinutes(1), 
                TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Gets the current number of active sessions.
        /// </summary>
        public int SessionCount => _sessions.Count;

        /// <summary>
        /// Adds a client session to the manager.
        /// </summary>
        /// <param name="session">The client session to add.</param>
        /// <returns>True if the session was added successfully, otherwise false.</returns>
        public bool AddSession(ClientSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Check if we've reached the maximum number of concurrent clients
            if (_sessions.Count >= _config.MaxConcurrentClients)
            {
                _logService.Warning($"Maximum number of concurrent clients reached ({_config.MaxConcurrentClients}). Rejecting new connection.");
                return false;
            }

            // Add the session to the dictionary
            bool added = _sessions.TryAdd(session.SessionId, session);
            
            if (added)
            {
                _logService.Info($"Session added: {session.SessionId}, Total active sessions: {_sessions.Count}");
            }
            else
            {
                _logService.Warning($"Failed to add session {session.SessionId} to the manager.");
            }
            
            return added;
        }

        /// <summary>
        /// Removes a client session from the manager.
        /// </summary>
        /// <param name="sessionId">The ID of the session to remove.</param>
        /// <returns>True if the session was removed successfully, otherwise false.</returns>
        public bool RemoveSession(Guid sessionId)
        {
            bool removed = _sessions.TryRemove(sessionId, out ClientSession session);
            
            if (removed)
            {
                _logService.Info($"Session removed: {sessionId}, Total active sessions: {_sessions.Count}");
                
                // Dispose the session
                session?.Dispose();
            }
            
            return removed;
        }

        /// <summary>
        /// Gets a client session by ID.
        /// </summary>
        /// <param name="sessionId">The ID of the session to get.</param>
        /// <returns>The client session, or null if not found.</returns>
        public ClientSession GetSession(Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out ClientSession session);
            return session;
        }

        /// <summary>
        /// Gets all active client sessions.
        /// </summary>
        /// <returns>An enumerable collection of all active client sessions.</returns>
        public IEnumerable<ClientSession> GetAllSessions()
        {
            return _sessions.Values;
        }

        /// <summary>
        /// Gets all active client sessions for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An enumerable collection of client sessions for the user.</returns>
        public IEnumerable<ClientSession> GetSessionsByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Enumerable.Empty<ClientSession>();

            return _sessions.Values.Where(s => s.UserId == userId);
        }

        /// <summary>
        /// Cleans up inactive sessions that have timed out.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CleanupInactiveSessions()
        {
            var sessionTimeoutMinutes = _config.SessionTimeoutMinutes;
            var timedOutSessions = _sessions.Values
                .Where(s => s.HasTimedOut(sessionTimeoutMinutes))
                .ToList();

            foreach (var session in timedOutSessions)
            {
                _logService.Info($"Session {session.SessionId} timed out after {sessionTimeoutMinutes} minutes of inactivity");
                
                // Disconnect the session
                await session.Disconnect("Session timeout");
                
                // Remove the session from the manager
                RemoveSession(session.SessionId);
            }

            if (timedOutSessions.Count > 0)
            {
                _logService.Info($"Cleaned up {timedOutSessions.Count} inactive sessions. Remaining sessions: {_sessions.Count}");
            }
        }

        /// <summary>
        /// Callback method for the cleanup timer.
        /// </summary>
        /// <param name="state">The timer state.</param>
        private async void CleanupTimerCallback(object state)
        {
            try
            {
                await CleanupInactiveSessions();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error in session cleanup timer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnects all active sessions.
        /// </summary>
        /// <param name="reason">The reason for disconnection.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task DisconnectAllSessions(string reason)
        {
            _logService.Info($"Disconnecting all sessions: {reason}");
            
            var tasks = new List<Task>();
            
            foreach (var session in _sessions.Values)
            {
                tasks.Add(session.Disconnect(reason));
            }
            
            await Task.WhenAll(tasks);
            
            // Clear the sessions dictionary
            _sessions.Clear();
        }

        /// <summary>
        /// Disposes resources used by the client session manager.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Stop the cleanup timer
                _cleanupTimer?.Dispose();
                
                // Disconnect all sessions
                DisconnectAllSessions("Server shutting down").Wait();
                
                // Dispose all sessions
                foreach (var session in _sessions.Values)
                {
                    session.Dispose();
                }
                
                // Clear the sessions dictionary
                _sessions.Clear();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error disposing session manager: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}