using CloudFileClient.Models;
using CloudFileClient.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CloudFileClient.Pages
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        private readonly FileService _fileService;
        private readonly DirectoryService _directoryService;

        private ObservableCollection<FileItem> _files = new();
        private ObservableCollection<DirectoryItem> _directories = new();
        private string? _currentDirectoryId;
        private Stack<(string? DirectoryId, string Name)> _navigationHistory = new();

        // Properties for binding
        public ObservableCollection<FileItem> Files
        {
            get => _files;
            set
            {
                _files = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasFiles));
            }
        }

        public ObservableCollection<DirectoryItem> Directories
        {
            get => _directories;
            set
            {
                _directories = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDirectories));
            }
        }

        public bool HasFiles => Files.Count > 0;
        public bool HasDirectories => Directories.Count > 0;

        public MainPage(
            AuthenticationService authService,
            FileService fileService,
            DirectoryService directoryService)
        {
            InitializeComponent();
            _authService = authService;
            _fileService = fileService;
            _directoryService = directoryService;

            // Set the binding context
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Check if user is logged in
            if (!_authService.IsLoggedIn)
            {
                // If not logged in, redirect to login page
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            // Update UI with current user's information
            UserNameLabel.Text = $"Welcome, {_authService.CurrentUser?.Username}";

            // Load the root directory contents
            await LoadDirectoryContents(null);
        }

        private async Task LoadDirectoryContents(string? directoryId)
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUser == null)
                return;

            try
            {
                // Show loading indicator
                ShowLoading("Loading contents...");

                // Store the current directory ID
                _currentDirectoryId = directoryId;

                // Update back button state
                BackButton.IsEnabled = directoryId != null;

                // Update the breadcrumb navigation
                UpdateBreadcrumb();

                // Get directory contents
                var (files, directories) = await _directoryService.GetDirectoryContentsAsync(
                    directoryId, _authService.CurrentUser.Id);

                // Update the UI collections
                Files = new ObservableCollection<FileItem>(files);
                Directories = new ObservableCollection<DirectoryItem>(directories);

                // Show empty state if no files or directories
                EmptyStateLayout.IsVisible = !HasFiles && !HasDirectories;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load directory contents: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicator
                HideLoading();
            }
        }

        private void ShowLoading(string message)
        {
            StatusLabel.Text = message;
            ActivitySpinner.IsRunning = true;
            LoadingGrid.IsVisible = true;
        }

        private void HideLoading()
        {
            LoadingGrid.IsVisible = false;
            ActivitySpinner.IsRunning = false;
        }

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirm Logout", "Are you sure you want to log out?", "Yes", "No");
            if (!confirm)
                return;

            try
            {
                ShowLoading("Logging out...");

                var (success, message) = await _authService.LogoutAsync();

                if (success)
                {
                    // Navigate back to login page
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    await DisplayAlert("Logout Failed", message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                HideLoading();
            }
        }

        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            await LoadDirectoryContents(_currentDirectoryId);
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            if (_navigationHistory.Count > 0)
            {
                var previous = _navigationHistory.Pop();
                await LoadDirectoryContents(previous.DirectoryId);
            }
        }

        private async void DirectoriesCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is DirectoryItem selectedDirectory)
            {
                // Clear the selection
                DirectoriesCollection.SelectedItem = null;

                // Add current directory to navigation history
                if (_currentDirectoryId != null || _navigationHistory.Count == 0)
                {
                    string currentName = GetCurrentDirectoryName();
                    _navigationHistory.Push((_currentDirectoryId, currentName));
                }

                // Navigate to the selected directory
                await LoadDirectoryContents(selectedDirectory.Id);
            }
        }

        private string GetCurrentDirectoryName()
        {
            if (_currentDirectoryId == null)
                return "Home";

            // Try to find the directory name in the current list
            var directory = Directories.FirstOrDefault(d => d.Id == _currentDirectoryId);
            return directory?.Name ?? "Directory";
        }

        private async void FilesCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is FileItem selectedFile)
            {
                // Clear the selection
                FilesCollection.SelectedItem = null;

                // Show file options
                string action = await DisplayActionSheet(
                    selectedFile.FileName,
                    "Cancel",
                    "Delete",
                    "Download", "View Details");

                switch (action)
                {
                    case "Download":
                        await DownloadFile(selectedFile.Id, selectedFile.FileName);
                        break;

                    case "Delete":
                        await DeleteFile(selectedFile);
                        break;

                    case "View Details":
                        await DisplayAlert("File Details", 
                            $"Name: {selectedFile.FileName}\n" +
                            $"Size: {selectedFile.FormattedSize}\n" +
                            $"Type: {selectedFile.ContentType}\n" +
                            $"Created: {selectedFile.CreatedAt:g}\n" +
                            $"Modified: {selectedFile.UpdatedAt:g}", 
                            "OK");
                        break;
                }
            }
        }

        private async void DownloadButton_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string fileId)
            {
                var file = Files.FirstOrDefault(f => f.Id == fileId);
                if (file != null)
                {
                    await DownloadFile(fileId, file.FileName);
                }
            }
        }

        private async Task DownloadFile(string fileId, string fileName)
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUser == null)
                return;

            try
            {
                // Choose download location
                var downloadFolder = await GetDownloadFolderPath();
                if (string.IsNullOrEmpty(downloadFolder))
                    return;

                string filePath = Path.Combine(downloadFolder, fileName);

                // Show loading indicator
                ShowLoading($"Downloading {fileName}...");

                // Download the file
                bool success = await _fileService.DownloadFileAsync(
                    fileId, 
                    filePath, 
                    _authService.CurrentUser.Id,
                    (current, total) => 
                    {
                        MainThread.BeginInvokeOnMainThread(() => 
                        {
                            StatusLabel.Text = $"Downloading {fileName}... ({current}/{total} chunks)";
                        });
                    });

                if (success)
                {
                    await DisplayAlert("Download Complete", $"{fileName} has been downloaded successfully.", "OK");
                }
                else
                {
                    await DisplayAlert("Download Failed", $"Failed to download {fileName}.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while downloading: {ex.Message}", "OK");
            }
            finally
            {
                HideLoading();
            }
        }

        private async Task<string?> GetDownloadFolderPath()
        {
            // On iOS, use the Documents folder
            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Downloads");
            }
            // On Windows, use the Downloads folder
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // Use the Downloads folder for simplicity in this example
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            // For other platforms, use a folder in the app's documents directory
            else
            {
                return Path.Combine(FileSystem.AppDataDirectory, "Downloads");
            }
        }

        private async Task DeleteFile(FileItem file)
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUser == null)
                return;

            // Ask for confirmation
            bool confirm = await DisplayAlert(
                "Confirm Deletion",
                $"Are you sure you want to delete {file.FileName}?",
                "Delete",
                "Cancel");

            if (!confirm)
                return;

            try
            {
                ShowLoading($"Deleting {file.FileName}...");

                // Delete the file
                bool success = await _fileService.DeleteFileAsync(
                    file.Id,
                    _authService.CurrentUser.Id);

                if (success)
                {
                    // Remove the file from the list
                    Files.Remove(file);
                    OnPropertyChanged(nameof(HasFiles));
                    
                    // Update empty state visibility
                    EmptyStateLayout.IsVisible = !HasFiles && !HasDirectories;
                }
                else
                {
                    await DisplayAlert("Deletion Failed", $"Failed to delete {file.FileName}.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while deleting: {ex.Message}", "OK");
            }
            finally
            {
                HideLoading();
            }
        }

        private async void CreateDirectoryFrame_Tapped(object sender, EventArgs e)
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUser == null)
                return;

            // Prompt for directory name
            string dirName = await DisplayPromptAsync(
                "Create New Folder",
                "Enter folder name:",
                placeholder: "New Folder");

            if (string.IsNullOrWhiteSpace(dirName))
                return;

            try
            {
                ShowLoading("Creating folder...");

                // Create the directory
                var newDirectory = await _directoryService.CreateDirectoryAsync(
                    dirName,
                    _currentDirectoryId,
                    _authService.CurrentUser.Id);

                if (newDirectory != null)
                {
                    // Add the new directory to the list
                    Directories.Add(newDirectory);
                    OnPropertyChanged(nameof(HasDirectories));
                    
                    // Update empty state visibility
                    EmptyStateLayout.IsVisible = !HasFiles && !HasDirectories;
                }
                else
                {
                    await DisplayAlert("Creation Failed", $"Failed to create folder '{dirName}'.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while creating folder: {ex.Message}", "OK");
            }
            finally
            {
                HideLoading();
            }
        }

        private async void UploadFileFrame_Tapped(object sender, EventArgs e)
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUser == null)
                return;

            try
            {
                // Use MediaPicker
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select a file to upload"
                });

                if (result == null)
                    return;
                
                // Get the file stream
                var stream = await result.OpenReadAsync();
                string tempFilePath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
        
                using (var fileStream = File.Create(tempFilePath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                ShowLoading($"Uploading {result.FileName}...");

                // Upload the file
                var uploadedFile = await _fileService.UploadFileAsync(
                    tempFilePath,
                    _currentDirectoryId,
                    _authService.CurrentUser.Id,
                    (current, total) => {
                        MainThread.BeginInvokeOnMainThread(() => {
                            StatusLabel.Text = $"Uploading {result.FileName}... ({current}/{total} chunks)";
                        });
                    });

                if (uploadedFile != null)
                {
                    // Add the uploaded file to the list
                    Files.Add(uploadedFile);
                    OnPropertyChanged(nameof(HasFiles));
                    
                    // Update empty state visibility
                    EmptyStateLayout.IsVisible = !HasFiles && !HasDirectories;
                    
                    await DisplayAlert("Upload Complete", $"{result.FileName} has been uploaded successfully.", "OK");
                }
                else
                {
                    await DisplayAlert("Upload Failed", $"Failed to upload {result.FileName}.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while uploading: {ex.Message}", "OK");
            }
            finally
            {
                HideLoading();
            }
        }

        private void UpdateBreadcrumb()
        {
            // Clear existing breadcrumb
            BreadcrumbContainer.Clear();
            
            // Add home item
            var homeLabel = new Label
            {
                Text = "Home",
                TextColor = Color.Parse("#0969da"),
                FontFamily = "InterMedium",
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14
            };
            
            var homeGesture = new TapGestureRecognizer();
            homeGesture.Tapped += async (s, e) => {
                // Clear navigation history and go to root
                _navigationHistory.Clear();
                await LoadDirectoryContents(null);
            };
            homeLabel.GestureRecognizers.Add(homeGesture);
            
            BreadcrumbContainer.Add(homeLabel);
            
            // Add separator if we have navigation history
            if (_navigationHistory.Count > 0 || _currentDirectoryId != null)
            {
                BreadcrumbContainer.Add(new Label
                {
                    Text = "/",
                    TextColor = Color.Parse("#57606a"),
                    FontFamily = "InterRegular",
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14,
                    Margin = new Thickness(4, 0)
                });
            }
            
            // Add navigation history items
            int index = 0;
            var historyItems = _navigationHistory.Reverse().ToList();
            
            foreach (var (dirId, dirName) in historyItems)
            {
                var dirLabel = new Label
                {
                    Text = dirName,
                    TextColor = Color.Parse("#0969da"),
                    FontFamily = "InterMedium",
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14
                };
                
                // Store a copy of the index and dirId for the gesture
                int capturedIndex = index;
                string? capturedDirId = dirId;
                
                var dirGesture = new TapGestureRecognizer();
                dirGesture.Tapped += async (s, e) => {
                    // Navigate to this directory by removing items from history
                    for (int i = 0; i < historyItems.Count - capturedIndex; i++)
                    {
                        _navigationHistory.Pop();
                    }
                    await LoadDirectoryContents(capturedDirId);
                };
                dirLabel.GestureRecognizers.Add(dirGesture);
                
                BreadcrumbContainer.Add(dirLabel);
                
                // Add separator if not the last item
                if (index < historyItems.Count - 1 || _currentDirectoryId != null)
                {
                    BreadcrumbContainer.Add(new Label
                    {
                        Text = "/",
                        TextColor = Color.Parse("#57606a"),
                        FontFamily = "InterRegular",
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = 14,
                        Margin = new Thickness(4, 0)
                    });
                }
                
                index++;
            }
            
            // Add current directory if we're not at root
            if (_currentDirectoryId != null)
            {
                string currentName = GetCurrentDirectoryName();
                BreadcrumbContainer.Add(new Label
                {
                    Text = currentName,
                    TextColor = Color.Parse("#24292f"),
                    FontFamily = "InterMedium",
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14
                });
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}