# Video Download Service and WPF Application

This solution contains a Windows Service and WPF application for downloading videos over TCP, based on the original console application.

## Projects

### 1. GetVideoService (Windows Service)
A Windows Service that runs in the background and listens for incoming video transfers over TCP.

**Features:**
- Runs as a Windows Service
- Configurable via appsettings.json
- Logging to Windows Event Log and file
- Automatic restart on failure
- Can be controlled via Service Manager or the WPF app

**Configuration (appsettings.json):**
```json
{
  "ServiceSettings": {
    "Folder": "C:\\temp\\vid",
    "Port": 5000,
    "PollingIntervalSeconds": 30,
    "AutoStart": true
  }
}
```

### 2. GetVideoWPF (WPF Application)
A Windows Presentation Foundation (WPF) application that provides a GUI for managing the service and monitoring downloads.

**Features:**
- Start/Stop the Windows Service
- Configure download folder and port
- Monitor service status
- View downloaded files
- Manual listening mode (independent of service)
- Real-time status updates

## Installation and Setup

### Prerequisites
- .NET 9.0 Runtime
- Windows 10/11 or Windows Server 2016+
- Administrator privileges for service installation

### Installing the Service

1. **Build the solution:**
   ```bash
   dotnet build --configuration Release
   ```

2. **Install the service (as Administrator):**
   - Run `InstallService.bat` as Administrator
   - Or manually:
     ```cmd
     sc create GetVideoService binPath="C:\path\to\GetVideoService.exe" start=auto DisplayName="Get Video Service"
     sc start GetVideoService
     ```

3. **Uninstall the service (as Administrator):**
   - Run `UninstallService.bat` as Administrator
   - Or manually:
     ```cmd
     sc stop GetVideoService
     sc delete GetVideoService
     ```

### Running the WPF Application

1. Build and run the GetVideoWPF project
2. The application will automatically detect if the service is installed and running
3. Use the GUI to control the service or run in manual mode

## Usage

### Service Mode
1. Install and start the service using the batch files or Service Manager
2. The service will automatically listen for incoming video transfers
3. Videos will be saved to the configured folder
4. Monitor the service using the WPF application

### Manual Mode
1. Run the WPF application
2. Configure the download folder and port
3. Click "Start Listening" to begin receiving videos
4. The application will show real-time status and downloaded files

## Architecture

### Original Console App (`GetVideo`)
- Simple console application that listens once for a video transfer
- Uses the `DownloadVideoOverTCPLib` library for the core functionality

### Windows Service (`GetVideoService`)
- Built using .NET Generic Host and Worker Services
- Runs continuously in the background
- Implements proper logging and error handling
- Can be managed via Windows Service Manager

### WPF Application (`GetVideoWPF`)
- Uses MVVM pattern with CommunityToolkit.Mvvm
- Dependency injection with Microsoft.Extensions.DependencyInjection
- Service control via ServiceController class
- Real-time UI updates

## Network Protocol
The applications use the same TCP protocol as the original console app:
1. Client connects to the specified port
2. Client sends filename length (4 bytes)
3. Client sends filename (UTF-8 encoded)
4. Client sends SHA256 checksum (32 bytes)
5. Client sends file data in chunks
6. Server validates checksum and saves file

## Logging
- **Service**: Logs to Windows Event Log and file (`C:\Logs\GetVideoService.log`)
- **WPF App**: Real-time status messages in the status bar

## Configuration Files

### Service (appsettings.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ServiceSettings": {
    "Folder": "C:\\temp\\vid",
    "Port": 5000,
    "PollingIntervalSeconds": 30,
    "AutoStart": true
  }
}
```

### WPF App (appsettings.json)
```json
{
  "VideoSettings": {
    "DefaultFolder": "C:\\temp\\vid",
    "DefaultPort": 5000
  }
}
```

## Troubleshooting

### Service Issues
1. **Service won't start**: Check Windows Event Log for error details
2. **Permission errors**: Ensure the service account has write access to the download folder
3. **Port conflicts**: Make sure the configured port isn't used by another application

### WPF App Issues
1. **Can't control service**: Run the WPF app as Administrator
2. **Service not detected**: Ensure the service is installed with the correct name "GetVideoService"

### Network Issues
1. **Can't receive files**: Check Windows Firewall settings for the configured port
2. **Checksum errors**: Verify network stability and file integrity

## Development

To extend or modify the applications:

1. **Core Logic**: Modify `DownloadVideoOverTCPLib` for protocol changes
2. **Service Logic**: Extend `VideoDownloadWorker` for service behavior
3. **UI Changes**: Modify the WPF XAML and ViewModels for GUI updates
4. **Configuration**: Add new settings to the respective appsettings.json files

## Dependencies

- Microsoft.Extensions.Hosting (Service framework)
- Microsoft.Extensions.DependencyInjection (IoC container)
- CommunityToolkit.Mvvm (MVVM helpers)
- Serilog (Logging for service)
- System.ServiceProcess.ServiceController (Service control)
