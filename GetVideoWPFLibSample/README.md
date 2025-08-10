# GetVideoWPFLibSample

This is a sample WPF application that demonstrates how to use the GetVideoWPFLib library for downloading videos over TCP.

## Overview

GetVideoWPFLibSample showcases the integration of the GetVideoWPFLib library in a standard WPF application. It provides a user interface for:

- Setting up TCP listening parameters (IP address and port)
- Starting and stopping the TCP listener
- Selecting a download folder for received videos
- Viewing downloaded video files
- Monitoring download progress

## Prerequisites

- .NET 6.0 or later
- Windows operating system

## Dependencies

- GetVideoWPFLib - The main library providing video download functionality
- DownloadVideoOverTcpLib - Core TCP communication library
- CommunityToolkit.Mvvm - MVVM support
- Microsoft.Extensions.Configuration.Json - Configuration support
- Microsoft.Extensions.DependencyInjection - Dependency injection

## Setup

1. Clone the repository
2. Open the solution in Visual Studio
3. Build the solution
4. Run the GetVideoWPFLibSample project

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "VideoSettings": {
    "DefaultFolder": "C:\\temp\\vid",
    "DefaultPort": 5000
  }
}
```

You can modify these settings to change the default download folder and TCP port.

## Usage

1. **Start the Application**: Run the GetVideoWPFLibSample application.

2. **Configure Settings**:
   - Set the IP address (default is the local machine's IP)
   - Set the port number (default is from appsettings.json)
   - Set the download folder (default is from appsettings.json)

3. **Start Listening**: Click the "Start Listening" button to begin listening for incoming video transfers.

4. **Send Videos**: Use a compatible sender application (like SendVideo from the same solution) to send videos to this application.

5. **View Downloaded Files**: The application automatically displays downloaded files in the list. You can refresh the list manually using the "Refresh Files" button.

6. **Open Download Folder**: Use the "Open Download Folder" option in the File menu to open the download folder in Windows Explorer.

## Architecture

The application follows the MVVM pattern:

- **Views**: MainWindow.xaml
- **ViewModels**: MainWindowViewModel.cs
- **Services**: Provided by GetVideoWPFLib (VideoDownloadService)
- **Dependency Injection**: Set up in App.xaml.cs

## Troubleshooting

- Ensure the sender and receiver are using the same port number
- Check that the download folder exists and is writable
- Verify that no firewall is blocking the TCP communication
- If videos fail to download, check the status messages at the bottom of the application
