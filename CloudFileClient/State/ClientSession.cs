using System;
using System.Threading.Tasks;
using CloudFileClient.Authentication;
using CloudFileClient.Commands;
using CloudFileClient.Connection;
using CloudFileClient.Utils;

namespace CloudFileClient.State
{
    /// <summary>
    /// Represents the client session and implements the state pattern context.
    /// </summary>
    public class ClientSession
    {
        private IClientSessionState _currentState;
        private readonly LogService _logService;
        
        /// <summary>
        /// Gets the client connection.
        /// </summary>
        public ClientConnection Connection { get; }
        
        /// <summary>
        /// Gets the user session.
        /// </summary>
        public UserSession UserSession { get; }
        
        /// <summary>
        /// Gets the state factory.
        /// </summary>
        public ClientStateFactory StateFactory { get; }
        
        /// <summary>
        /// Gets a value indicating whether the client is connected to the server.
        /// </summary>
        public bool IsConnected => Connection.IsConnected;
        
        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated => UserSession.IsAuthenticated;
        
        /// <summary>
        /// Event raised when the state changes.
        /// </summary>
        public event EventHandler<ClientStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Initializes a new instance of the ClientSession class.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        /// <param name="userSession">The user session.</param>
        /// <param name="stateFactory">The state factory.</param>
        /// <param name="logService">The logging service.</param>
        public ClientSession(
            ClientConnection connection,
            UserSession userSession,
            ClientStateFactory stateFactory,
            LogService logService)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            UserSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            StateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Subscribe to connection status changes
            Connection.ConnectionStatusChanged += Connection_ConnectionStatusChanged;
            
            // Subscribe to authentication changes
            UserSession.AuthenticationChanged += UserSession_AuthenticationChanged;
            
            // Set initial state to disconnected
            _currentState = StateFactory.CreateDisconnectedState(this);
            Task.Run(() => _currentState.OnEnter()).Wait();
        }

        /// <summary>
        /// Handles a command by delegating to the current state.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
        public async Task<CommandResult> HandleCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            
            // Record activity to prevent session timeout
            UserSession.RecordActivity();
            
            // Delegate to the current state
            return await _currentState.HandleCommand(command);
        }

        /// <summary>
        /// Transitions to a new state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public async Task TransitionToState(IClientSessionState newState)
        {
            if (newState == null)
                throw new ArgumentNullException(nameof(newState));
            
            var oldState = _currentState;
            string oldStateName = oldState?.GetType().Name ?? "null";
            string newStateName = newState.GetType().Name;
            
            _logService.Info($"Transitioning from {oldStateName} to {newStateName}");
            
            // Exit the current state
            if (oldState != null)
            {
                await oldState.OnExit();
            }
            
            // Set the new state
            _currentState = newState;
            
            // Enter the new state
            await newState.OnEnter();
            
            // Raise the state changed event
            OnStateChanged(new ClientStateChangedEventArgs(oldState, newState));
        }

        /// <summary>
        /// Handles connection status changes.
        /// </summary>
        private async void Connection_ConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            if (!e.IsConnected && _currentState is not DisconnectedState)
            {
                // Transition to disconnected state when connection is lost
                await TransitionToState(StateFactory.CreateDisconnectedState(this));
            }
        }

        /// <summary>
        /// Handles authentication status changes.
        /// </summary>
        private async void UserSession_AuthenticationChanged(object sender, AuthenticationEventArgs e)
        {
            // Handle authentication changes based on current state
            if (e.IsAuthenticated && _currentState is AuthRequiredState)
            {
                // Transition to authenticated state when user is authenticated
                await TransitionToState(StateFactory.CreateAuthenticatedState(this));
            }
            else if (!e.IsAuthenticated && _currentState is AuthenticatedState)
            {
                // Transition to authentication required state when user logs out
                await TransitionToState(StateFactory.CreateAuthRequiredState(this));
            }
        }

        /// <summary>
        /// Raises the StateChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnStateChanged(ClientStateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Provides data for the StateChanged event.
    /// </summary>
    public class ClientStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the old state.
        /// </summary>
        public IClientSessionState OldState { get; }
        
        /// <summary>
        /// Gets the new state.
        /// </summary>
        public IClientSessionState NewState { get; }
        
        /// <summary>
        /// Gets the old state type name.
        /// </summary>
        public string OldStateName => OldState?.GetType().Name ?? "null";
        
        /// <summary>
        /// Gets the new state type name.
        /// </summary>
        public string NewStateName => NewState?.GetType().Name ?? "null";
        
        /// <summary>
        /// Gets the time when the state changed.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the ClientStateChangedEventArgs class.
        /// </summary>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        public ClientStateChangedEventArgs(IClientSessionState oldState, IClientSessionState newState)
        {
            OldState = oldState;
            NewState = newState;
            Timestamp = DateTime.Now;
        }
    }
}