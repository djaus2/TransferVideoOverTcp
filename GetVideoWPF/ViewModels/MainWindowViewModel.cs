using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetVideoWPF.Services;
using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace GetVideoWPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IServiceControlService _serviceControlService;
    private readonly IVideoDownloadService _videoDownloadService;

    [ObservableProperty]
    private string _serviceStatus = "Unknown";

    [ObservableProperty]
    private string _downloadFolder = @"C:\temp\vid";

    partial void OnDownloadFolderChanged(string value)
    {
        // Refresh the downloaded files when folder changes
        RefreshDownloadedFiles();
        
        // Create directory if it doesn't exist
        try
        {
            if (!Directory.Exists(value))
            {
                Directory.CreateDirectory(value);
            }
            
            // Set up file system watcher for the new folder
            SetupFileWatcher(value);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create directory: {ex.Message}";
        }
    }

    [ObservableProperty]
    private int _port = 5000;

    [ObservableProperty]
    private bool _isServiceRunning;

    [ObservableProperty]
    private bool _isServiceInstalled;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private string _localIpAddress = "";

    [ObservableProperty]
    private ObservableCollection<string> _downloadedFiles = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isVideoDownloading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _currentDownloadFile = "";

    private System.Timers.Timer? _logMonitorTimer;
    private long _lastLogPosition = 0;
    private DateTime _lastDownloadActivity = DateTime.MinValue;
    private FileSystemWatcher? _fileWatcher;

    public MainWindowViewModel(IServiceControlService serviceControlService, IVideoDownloadService videoDownloadService)
    {
        _serviceControlService = serviceControlService;
        _videoDownloadService = videoDownloadService;
        
        LoadLocalIpAddress();
        RefreshServiceStatus();
        RefreshDownloadedFiles();
        InitializeLogMonitoring();
        SetupFileWatcher(DownloadFolder); // Initialize file watcher for current folder
    }

    [RelayCommand]
    private async Task StartService()
    {
        try
        {
            IsBusy = true;
            CurrentDownloadFile = "Starting service...";
            StatusMessage = "Starting service...";
            
            await _serviceControlService.StartServiceAsync();
            await Task.Delay(2000); // Give service time to start
            RefreshServiceStatus();
            StatusMessage = "Service started successfully";
            
            System.Windows.MessageBox.Show(
                "Service started successfully!",
                "Service Started",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start service: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to start service:\n\n{ex.Message}\n\n" +
                "Check the service logs at C:\\Logs\\GetVideoService.log for more details.",
                "Service Start Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StopService()
    {
        try
        {
            IsBusy = true;
            CurrentDownloadFile = "Stopping service...";
            StatusMessage = "Stopping service...";
            
            await _serviceControlService.StopServiceAsync();
            await Task.Delay(2000); // Give service time to stop
            RefreshServiceStatus();
            StatusMessage = "Service stopped successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to stop service: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InstallService()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "This will install the GetVideoService as a Windows Service.\n\n" +
                "You will be prompted for Administrator privileges.\n\n" +
                "Do you want to continue?",
                "Install Service",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            IsBusy = true;
            CurrentDownloadFile = "Installing service...";
            StatusMessage = "Installing service...";
            
            await _serviceControlService.InstallServiceAsync();
            await Task.Delay(3000); // Give service time to install
            RefreshServiceStatus();
            StatusMessage = "Service installed successfully";
            
            System.Windows.MessageBox.Show(
                "Service installed successfully!\n\n" +
                "You can now start the service from the Service menu or Service Control section.",
                "Installation Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Service installation cancelled by user";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to install service: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to install service:\n\n{ex.Message}",
                "Installation Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UninstallService()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "This will uninstall the GetVideoService from Windows Services.\n\n" +
                "The service will be stopped and removed from the system.\n" +
                "You will be prompted for Administrator privileges.\n\n" +
                "Do you want to continue?",
                "Uninstall Service",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            IsBusy = true;
            CurrentDownloadFile = "Uninstalling service...";
            StatusMessage = "Uninstalling service...";
            
            await _serviceControlService.UninstallServiceAsync();
            await Task.Delay(3000); // Give service time to uninstall
            RefreshServiceStatus();
            StatusMessage = "Service uninstalled successfully";
            
            System.Windows.MessageBox.Show(
                "Service uninstalled successfully!",
                "Uninstallation Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Service uninstallation cancelled by user";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to uninstall service: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to uninstall service:\n\n{ex.Message}",
                "Uninstallation Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StartListening()
    {
        try
        {
            StatusMessage = "Starting to listen for videos...";
            IsListening = true;
            await _videoDownloadService.StartListeningAsync(DownloadFolder, Port);
            StatusMessage = $"Listening on port {Port}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start listening: {ex.Message}";
            IsListening = false;
        }
    }

    [RelayCommand]
    private void StopListening()
    {
        try
        {
            StatusMessage = "Stopping listening...";
            _videoDownloadService.StopListening();
            IsListening = false;
            StatusMessage = "Stopped listening";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to stop listening: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshServiceStatus()
    {
        try
        {
            IsServiceInstalled = _serviceControlService.IsServiceInstalled();
            
            if (IsServiceInstalled)
            {
                var status = _serviceControlService.GetServiceStatus();
                ServiceStatus = status.ToString();
                IsServiceRunning = status == ServiceControllerStatus.Running;
            }
            else
            {
                ServiceStatus = "Not Installed";
                IsServiceRunning = false;
            }
        }
        catch (Exception ex)
        {
            ServiceStatus = $"Error: {ex.Message}";
            IsServiceRunning = false;
            IsServiceInstalled = false;
        }
    }

    [RelayCommand]
    private void StartNewLog()
    {
        try
        {
            // Check for date-based log file (Serilog rolling format)
            var logDirectory = @"C:\Logs\";
            var today = DateTime.Now.ToString("yyyyMMdd");
            var dateBasedLogPath = Path.Combine(logDirectory, $"GetVideoService{today}.log");
            var staticLogPath = Path.Combine(logDirectory, "GetVideoService.log");
            
            string logPath = null;
            if (File.Exists(dateBasedLogPath))
            {
                logPath = dateBasedLogPath;
            }
            else if (File.Exists(staticLogPath))
            {
                logPath = staticLogPath;
            }
            
            if (logPath != null)
            {
                var separator = new string('=', 80);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var newLogEntry = $"\n{separator}\n" +
                                $"NEW LOG SESSION STARTED - {timestamp}\n" +
                                $"Started from WPF Application\n" +
                                $"{separator}\n";
                
                File.AppendAllText(logPath, newLogEntry);
                StatusMessage = $"New log session started at {timestamp} in {Path.GetFileName(logPath)}";
            }
            else
            {
                StatusMessage = "Log file not found. Service may not be running.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start new log: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshDownloadedFiles()
    {
        try
        {
            if (Directory.Exists(DownloadFolder))
            {
                var files = Directory.GetFiles(DownloadFolder)
                    .Select(Path.GetFileName)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .OrderByDescending(f => new FileInfo(Path.Combine(DownloadFolder, f)).LastWriteTime);

                DownloadedFiles.Clear();
                foreach (var file in files)
                {
                    DownloadedFiles.Add(file);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to refresh files: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseDownloadFolder()
    {
        try
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select Download Folder";
            dialog.UseDescriptionForTitle = true;
            dialog.ShowNewFolderButton = true;
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            
            // Set initial directory if current folder exists
            if (Directory.Exists(DownloadFolder))
            {
                dialog.SelectedPath = DownloadFolder;
            }
            
            // Show the dialog
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                DownloadFolder = dialog.SelectedPath;
                StatusMessage = $"Download folder changed to: {DownloadFolder}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to browse folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenDownloadFolder()
    {
        try
        {
            if (Directory.Exists(DownloadFolder))
            {
                System.Diagnostics.Process.Start("explorer.exe", DownloadFolder);
            }
            else
            {
                StatusMessage = "Download folder does not exist";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open folder: {ex.Message}";
        }
    }

    private void SetupFileWatcher(string folderPath)
    {
        try
        {
            // Dispose existing watcher
            _fileWatcher?.Dispose();
            
            if (!Directory.Exists(folderPath))
                return;

            // Create new file system watcher
            _fileWatcher = new FileSystemWatcher(folderPath);
            _fileWatcher.Filter = "*.*"; // Watch all files
            _fileWatcher.IncludeSubdirectories = false;
            _fileWatcher.EnableRaisingEvents = true;

            // Subscribe to events
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;
            
            StatusMessage = $"File monitoring enabled for: {folderPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to setup file watcher: {ex.Message}";
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Use dispatcher to update UI from background thread
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RefreshDownloadedFiles();
            StatusMessage = $"File {e.ChangeType.ToString().ToLower()}: {Path.GetFileName(e.FullPath)}";
        });
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Use dispatcher to update UI from background thread  
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RefreshDownloadedFiles();
            StatusMessage = $"File renamed: {Path.GetFileName(e.OldName)} → {Path.GetFileName(e.Name)}";
        });
    }

    private void LoadLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var localIP = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            LocalIpAddress = localIP?.ToString() ?? "Unknown";
        }
        catch
        {
            LocalIpAddress = "Unable to determine";
        }
    }

    private void InitializeLogMonitoring()
    {
        // Initialize log monitoring timer (check every 500ms for download activity)
        _logMonitorTimer = new System.Timers.Timer(500);
        _logMonitorTimer.Elapsed += CheckForDownloadActivity;
        _logMonitorTimer.AutoReset = true;
        _logMonitorTimer.Start();
    }

    private void CheckForDownloadActivity(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // Get log file - check multiple possible dates since service might be running from previous day
            var logDirectory = @"C:\Logs\";
            string? logPath = null;
            
            // Check today's date first, then yesterday, then static log
            for (int daysBack = 0; daysBack <= 1; daysBack++)
            {
                var checkDate = DateTime.Now.AddDays(-daysBack).ToString("yyyyMMdd");
                var dateBasedLogPath = Path.Combine(logDirectory, $"GetVideoService{checkDate}.log");
                if (File.Exists(dateBasedLogPath))
                {
                    logPath = dateBasedLogPath;
                    break;
                }
            }
            
            // Fallback to static log file
            if (logPath == null)
            {
                var staticLogPath = Path.Combine(logDirectory, "GetVideoService.log");
                if (File.Exists(staticLogPath))
                {
                    logPath = staticLogPath;
                }
            }

            if (logPath == null || !File.Exists(logPath))
            {
                // If download is active but no log file, timeout after 5 seconds
                if (IsVideoDownloading && DateTime.Now - _lastDownloadActivity > TimeSpan.FromSeconds(5))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsVideoDownloading = false;
                        CurrentDownloadFile = "";
                        StatusMessage = "Download monitoring stopped - log file not found";
                    });
                }
                return;
            }

            var fileInfo = new FileInfo(logPath);
            if (fileInfo.Length <= _lastLogPosition)
            {
                // Check if download is still active (timeout after 3 seconds of no new log activity)
                if (IsVideoDownloading && DateTime.Now - _lastDownloadActivity > TimeSpan.FromSeconds(3))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsVideoDownloading = false;
                        CurrentDownloadFile = "";
                        StatusMessage = "Download completed";
                        RefreshDownloadedFiles(); // Refresh the file list
                    });
                }
                return;
            }

            // Read new content from the log
            using var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStream.Seek(_lastLogPosition, SeekOrigin.Begin);
            
            using var reader = new StreamReader(fileStream);
            string? line;
            bool downloadInProgress = false;
            bool downloadCompleted = false;
            string? downloadedFileName = null;
            bool connectionAccepted = false;

            while ((line = reader.ReadLine()) != null)
            {
                // Check for download start indicators - more aggressive detection
                if (line.Contains("Waiting for video download connection") || 
                    line.Contains("AcceptTcpClient") ||
                    line.Contains("Listening on IP") ||
                    line.Contains("DOWNLOAD: Client connected") ||
                    line.Contains("DOWNLOAD: Waiting for connection"))
                {
                    if (!IsVideoDownloading)
                    {
                        downloadInProgress = true;
                        _lastDownloadActivity = DateTime.Now;
                    }
                }
                
                // Check for connection accepted (more reliable start indicator)
                if (line.Contains("Client connected") || 
                    line.Contains("starting download") ||
                    line.Contains("Receiving file:") ||
                    line.Contains("DOWNLOAD: Client connected") ||
                    line.Contains("DOWNLOAD: Receiving file:") ||
                    line.Contains("DOWNLOAD: Downloading video data"))
                {
                    connectionAccepted = true;
                    downloadInProgress = true;
                    _lastDownloadActivity = DateTime.Now;
                    
                    // Extract filename if present
                    if (line.Contains("Receiving file:") || line.Contains("DOWNLOAD: Receiving file:"))
                    {
                        var parts = line.Contains("DOWNLOAD:") ? 
                            line.Split("DOWNLOAD: Receiving file:") : 
                            line.Split("Receiving file:");
                        if (parts.Length > 1)
                        {
                            var fileName = parts[1].Trim();
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                CurrentDownloadFile = $"Receiving: {fileName}";
                            });
                        }
                    }
                    else if (line.Contains("DOWNLOAD: Downloading video data"))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentDownloadFile = "Downloading video data...";
                        });
                    }
                }
                
                // Check for download completion - immediate detection
                if (line.Contains("Video file received:") || 
                    line.Contains("File received successfully") ||
                    line.Contains("✅") ||
                    line.Contains("DOWNLOAD: Download completed") ||
                    line.Contains("DOWNLOAD: ✅"))
                {
                    downloadCompleted = true;
                    _lastDownloadActivity = DateTime.Now;
                    
                    // Extract filename from log
                    if (line.Contains("Video file received:"))
                    {
                        var parts = line.Split("Video file received:");
                        if (parts.Length > 1)
                        {
                            downloadedFileName = parts[1].Trim();
                        }
                    }
                }
                
                // Check for download failure
                if (line.Contains("Video download failed or was cancelled") || 
                    line.Contains("Error during video download") ||
                    line.Contains("❌") ||
                    line.Contains("DOWNLOAD: Error during transfer") ||
                    line.Contains("DOWNLOAD: ❌"))
                {
                    downloadCompleted = true;
                    _lastDownloadActivity = DateTime.Now;
                }
            }

            _lastLogPosition = fileStream.Position;

            // Update UI on main thread - more responsive
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (downloadInProgress && !IsVideoDownloading)
                {
                    IsVideoDownloading = true;
                    if (string.IsNullOrEmpty(CurrentDownloadFile))
                    {
                        CurrentDownloadFile = "Preparing to receive video...";
                    }
                    StatusMessage = "Video download in progress...";
                }
                
                if (downloadCompleted)
                {
                    IsVideoDownloading = false;
                    CurrentDownloadFile = "";
                    if (!string.IsNullOrEmpty(downloadedFileName))
                    {
                        StatusMessage = $"Download completed: {downloadedFileName}";
                    }
                    else
                    {
                        StatusMessage = "Download completed";
                    }
                    // Always refresh files when download completes
                    RefreshDownloadedFiles();
                }
            });
        }
        catch (Exception ex)
        {
            // Silently handle file access errors (file might be locked by service)
            System.Diagnostics.Debug.WriteLine($"Log monitoring error: {ex.Message}");
            
            // If we're stuck in downloading state due to errors, reset after 10 seconds
            if (IsVideoDownloading && DateTime.Now - _lastDownloadActivity > TimeSpan.FromSeconds(10))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsVideoDownloading = false;
                    CurrentDownloadFile = "";
                    StatusMessage = "Download monitoring stopped due to error";
                });
            }
        }
    }

    // Implement IDisposable to clean up timer
    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _logMonitorTimer?.Stop();
                _logMonitorTimer?.Dispose();
                _fileWatcher?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
