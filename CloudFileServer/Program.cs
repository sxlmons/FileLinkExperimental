using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudFileServer
{
    /// <summary>
    /// Entry point class for the Cloud File Server application.
    /// </summary>
    class Program
    {
        private static CloudFileServerApp _app;
        private static ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

        /// <summary>
        /// Entry point method for the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static async Task Main(string[] args)
        {
            Console.WriteLine("Cloud File Server starting...");
            
            try
            {
                // Create server configuration
                var config = new ServerConfiguration
                {
                    Port = GetPortFromArgs(args, 9000),
                    // Other configuration settings can be adjusted here
                };

                // Create and initialize the application
                _app = new CloudFileServerApp(config);
                _app.Initialize();

                // Set up console event handlers for graceful shutdown
                Console.CancelKeyPress += OnCancelKeyPress;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                // Start the server
                await _app.Start();

                // Wait for shutdown signal
                _shutdownEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                await ShutdownAsync();
            }
        }

        /// <summary>
        /// Handles Ctrl+C key press.
        /// </summary>
        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Shutdown requested (Ctrl+C)...");
            e.Cancel = true;
            _shutdownEvent.Set();
        }

        /// <summary>
        /// Handles process exit.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Process exit detected...");
            _shutdownEvent.Set();
            
            // Ensure synchronous shutdown on process exit
            ShutdownAsync().Wait();
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        private static async Task ShutdownAsync()
        {
            try
            {
                if (_app != null)
                {
                    Console.WriteLine("Shutting down server...");
                    await _app.Stop();
                    Console.WriteLine("Server shutdown complete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the server port from command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <param name="defaultPort">Default port to use if not specified.</param>
        /// <returns>The server port.</returns>
        private static int GetPortFromArgs(string[] args, int defaultPort)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int port) && port > 0 && port < 65536)
            {
                return port;
            }
            return defaultPort;
        }
    }
}