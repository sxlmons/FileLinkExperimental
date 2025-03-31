using CloudFileClient.Services;

namespace CloudFileClient.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthenticationService _authService;
        private readonly NetworkService _networkService;

        public LoginPage(AuthenticationService authService, NetworkService networkService)
        {
            InitializeComponent();
            _authService = authService;
            _networkService = networkService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Clear any previous credentials
            UsernameEntry.Text = "";
            PasswordEntry.Text = "";
            StatusLabel.Text = "";
            StatusLabel.IsVisible = false;
            
            // Return to login panel
            ShowLoginPanel();
            
            // Set focus to username entry
            UsernameEntry.Focus();
        }

        private async void LoginButton_Clicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please enter both username and password", "OK");
                return;
            }

            // Show loading indicator
            ActivitySpinner.IsVisible = true;
            ActivitySpinner.IsRunning = true;
            StatusLabel.Text = "Logging in...";
            StatusLabel.IsVisible = true;
            LoginButton.IsEnabled = false;
            CreateAccountButton.IsEnabled = false;

            try
            {
                var (success, message) = await _authService.LoginAsync(username, password);

                if (success)
                {
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    // Show error message
                    await DisplayAlert("Login Failed", message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicator
                ActivitySpinner.IsVisible = false;
                ActivitySpinner.IsRunning = false;
                StatusLabel.IsVisible = false;
                LoginButton.IsEnabled = true;
                CreateAccountButton.IsEnabled = true;
            }
        }

        private void CreateAccountButton_Clicked(object sender, EventArgs e)
        {
            // Switch to account creation panel
            ShowCreateAccountPanel();
        }

        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            // Switch back to login panel
            ShowLoginPanel();
        }

        private async void SubmitAccountButton_Clicked(object sender, EventArgs e)
        {
            string username = NewUsernameEntry.Text?.Trim() ?? "";
            string password = NewPasswordEntry.Text ?? "";
            string email = EmailEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please enter both username and password", "OK");
                return;
            }

            // Show loading indicator
            ActivitySpinner.IsVisible = true;
            ActivitySpinner.IsRunning = true;
            StatusLabel.Text = "Creating account...";
            StatusLabel.IsVisible = true;
            SubmitAccountButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            try
            {
                var (success, message, userId) = await _authService.CreateAccountAsync(username, password, email);

                if (success)
                {
                    // Account created successfully
                    await DisplayAlert("Success", "Account created successfully. You can now login.", "OK");
                    
                    // Switch back to login panel
                    ShowLoginPanel();
                    
                    // Pre-fill login fields with the new account info
                    UsernameEntry.Text = username;
                    PasswordEntry.Text = password;
                }
                else
                {
                    // Show error message
                    await DisplayAlert("Account Creation Failed", message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicator
                ActivitySpinner.IsVisible = false;
                ActivitySpinner.IsRunning = false;
                StatusLabel.IsVisible = false;
                SubmitAccountButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void SaveSettingsButton_Clicked(object sender, EventArgs e)
        {
            string serverAddress = ServerAddressEntry.Text?.Trim() ?? "localhost";
            string portText = ServerPortEntry.Text?.Trim() ?? "9000";

            if (string.IsNullOrEmpty(serverAddress))
            {
                serverAddress = "localhost";
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                port = 9000;
            }

            // Save the server settings
            _networkService.SetServer(serverAddress, port);

            // Update the UI
            ServerAddressEntry.Text = serverAddress;
            ServerPortEntry.Text = port.ToString();

            StatusLabel.Text = $"Server settings saved: {serverAddress}:{port}";
            StatusLabel.IsVisible = true;
            
            // Hide settings panel after saving
            ServerSettingsPanel.IsVisible = false;
            ToggleSettingsButton.Text = "▼";
        }
        
        private void ToggleSettingsButton_Clicked(object sender, EventArgs e)
        {
            // Toggle the visibility of the settings panel
            ServerSettingsPanel.IsVisible = !ServerSettingsPanel.IsVisible;
            
            // Update the button text based on the panel visibility
            ToggleSettingsButton.Text = ServerSettingsPanel.IsVisible ? "▲" : "▼";
        }
        
        // Helper methods to toggle between login and account creation panels
        private void ShowLoginPanel()
        {
            LoginPanel.IsVisible = true;
            CreateAccountPanel.IsVisible = false;
            
            // Clear account creation fields
            NewUsernameEntry.Text = "";
            NewPasswordEntry.Text = "";
            EmailEntry.Text = "";
        }
        
        private void ShowCreateAccountPanel()
        {
            LoginPanel.IsVisible = false;
            CreateAccountPanel.IsVisible = true;
            
            // Set focus to new username entry
            NewUsernameEntry.Focus();
        }
    }
}