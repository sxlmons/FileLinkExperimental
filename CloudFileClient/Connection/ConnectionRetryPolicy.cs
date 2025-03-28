using System;
using System.Threading.Tasks;
using CloudFileClient.Utils;

namespace CloudFileClient.Connection
{
    /// <summary>
    /// Provides retry policy for connection attempts.
    /// </summary>
    public class ConnectionRetryPolicy
    {
        private readonly LogService _logService;
        private readonly int _maxAttempts;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _backoffMultiplier;
        
        /// <summary>
        /// Initializes a new instance of the ConnectionRetryPolicy class.
        /// </summary>
        /// <param name="logService">The logging service.</param>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelayMs">The initial delay in milliseconds.</param>
        /// <param name="maxDelayMs">The maximum delay in milliseconds.</param>
        /// <param name="backoffMultiplier">The multiplier for exponential backoff.</param>
        public ConnectionRetryPolicy(
            LogService logService, 
            int maxAttempts = 5, 
            int initialDelayMs = 500, 
            int maxDelayMs = 30000, 
            double backoffMultiplier = 2.0)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _maxAttempts = maxAttempts;
            _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
            _backoffMultiplier = backoffMultiplier;
        }
        
        /// <summary>
        /// Executes an action with retry logic.
        /// </summary>
        /// <typeparam name="T">The return type of the action.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="actionName">The name of the action for logging.</param>
        /// <returns>The result of the action.</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string actionName)
        {
            int attempt = 0;
            TimeSpan delay = _initialDelay;
            Exception lastException = null;
            
            while (attempt < _maxAttempts)
            {
                try
                {
                    // If this is a retry, log the attempt
                    if (attempt > 0)
                    {
                        _logService.Info($"Retry attempt {attempt} for {actionName} after {delay.TotalMilliseconds}ms...");
                    }
                    
                    // Execute the action
                    return await action();
                }
                catch (Exception ex)
                {
                    // Store the exception
                    lastException = ex;
                    
                    // Log the failure
                    _logService.Warning($"Attempt {attempt + 1} for {actionName} failed: {ex.Message}");
                    
                    // Increase the attempt counter
                    attempt++;
                    
                    // If we've reached the maximum attempts, rethrow the exception
                    if (attempt >= _maxAttempts)
                    {
                        _logService.Error($"Maximum retry attempts ({_maxAttempts}) reached for {actionName}", ex);
                        throw;
                    }
                    
                    // Wait before retrying
                    await Task.Delay(delay);
                    
                    // Increase the delay for the next attempt (exponential backoff)
                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(_maxDelay.TotalMilliseconds, delay.TotalMilliseconds * _backoffMultiplier));
                }
            }
            
            // This should never happen, but just in case
            throw lastException ?? new Exception($"Failed to execute {actionName} after {_maxAttempts} attempts.");
        }
    }
}