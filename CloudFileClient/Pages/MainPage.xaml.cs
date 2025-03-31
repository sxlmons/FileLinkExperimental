using CloudFileClient.Services;

namespace CloudFileClient.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly AuthenticationService _authService;

        public MainPage(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Check if user is logged in
            if (!_authService.IsLoggedIn)
            {
                // If not logged in, redirect to login page
                Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            // Update UI with current user's information
            UserInfoLabel.Text = $"User: {_authService.CurrentUser?.Username}";
        }

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            // Show loading indicator
            ActivitySpinner.IsVisible = true;
            ActivitySpinner.IsRunning = true;
            StatusLabel.Text = "Logging out...";
            StatusLabel.IsVisible = true;
            LogoutButton.IsEnabled = false;

            try
            {
                var (success, message) = await _authService.LogoutAsync();

                if (success)
                {
                    // Navigate back to login page
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    // Show error message
                    await DisplayAlert("Logout Failed", message, "OK");
                    
                    // Hide loading indicator
                    ActivitySpinner.IsVisible = false;
                    ActivitySpinner.IsRunning = false;
                    StatusLabel.IsVisible = false;
                    LogoutButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                
                // Hide loading indicator
                ActivitySpinner.IsVisible = false;
                ActivitySpinner.IsRunning = false;
                StatusLabel.IsVisible = false;
                LogoutButton.IsEnabled = true;
            }
        }
    }
}