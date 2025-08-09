# Video Transfer Over TCP - Deployment Guide

## Project Overview

This solution contains multiple projects for transferring video over TCP:

### Existing Projects
- **DownloadVideoOverTcpLib**: Core library for video downloading functionality over Tcp
- **GetVideoConsoleApp**: Original console application
- **SendVideo**: MAUI mobile application for sending videos
- **SendVideoOverTcpLib**: MAUI Library for sending video functionality

### Recently Projects Created
- **GetVideoService**: Windows Service for continuous video downloading
- **GetVideoViaSvcWPF**: WPF management application for video downloading with Tcp Service using Windows Service ***GetVideoService***..
- **GetVideoInAppWPF**: WPF application for video downloading with Tcp Service running In-App.

## Project Structure _(To be updated)_

```
TransferVideoOverTcp/
├── GetVideoService/          # Windows Service (NEW)
│   ├── Program.cs            # Service host configuration
│   ├── VideoDownloadWorker.cs # Background worker service
│   ├── ServiceSettings.cs    # Configuration settings
│   └── appsettings.json      # Service configuration
├── GetVideoWPF/              # WPF Management App (NEW)
│   ├── MainWindow.xaml       # Main UI
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs # MVVM view model
│   ├── Services/
│   │   ├── ServiceControlService.cs # Windows service control
│   │   └── VideoDownloadService.cs  # Manual download service
│   └── Converters.cs         # UI value converters
└── [Other existing projects...]
```

## Features

### GetVideoConsoleApp (Original Console App)
- Simple console application for video downloading
- Starts listening for video transfers on a specified port
- Once a video is received, it saves the file to a specified directory
- Then stops the service and exits

### GetVideoService (Windows Service)
- Runs as a Windows background service
- Automatically starts video downloads when TCP connections are received
- Configurable through appsettings.json
- Logs to Windows Event Log and file system
- Self-contained deployment ready
- Can be installed via next app or using InstallService.bat
- Note that installation doesn't normally need elevated privileges, but starting the service does require Administrator rights.

### GetVideoViaSvcWPF (Management Application)
- Modern WPF interface using MVVM pattern
- **Service menu with Install/Uninstall options**
- Service management (Start/Stop/Install/Uninstall)
- Manual video download capability
- Real-time status monitoring
- Settings configuration
- Confirmation dialogs for critical operations

### GetVideoInAppWPF (In-App WPF Application)
- As per the Console app , but with largely same UI as GetVideoViaSvcWPF
- No Install/Uninstall options for service as it runs in app.
- A download does not trigger service stop nor app exit.
- Has a brief popup after each download

## MAUII

### SendVideo (MAUI Application)
- Mobile application for sending videos from Android devices
- Sends *.mp4 videos*
- Can configure port used and Tcp connection timeout
- Select target from local devices that can be pinged.
  - They need to accept being pinged
- Remembers previous settings
- Popup notification after each successful send
- Popup notification if device does't connect after timeout *(default 15 sec)*

### SendVideoOverTcpLib (MAUI Library)
- Core library for sending video files over TCP

## Installation Instructions

### 1. Build the Solution
```powershell
cd "C:\Folders\Source\repos\DownloadVideoOverTcp"
dotnet build --configuration Release
```

### 2. Install the Windows Service

#### Option A: Using WPF Application (Recommended)
1. Launch GetVideoWPF.exe (no need to run as Administrator)
2. Go to Service → Install Service
3. **The system will automatically prompt for Administrator privileges (UAC)*
  - If accepted Visual Studio restarts in elevated mode and the service is installed
4. Confirm the UAC elevation when prompted
5. Confirm the installation when prompted
6. The service will be installed and ready to start

**Note**: The WPF application automatically handles UAC elevation, so you don't need to run it as Administrator manually.

#### Option B: Using Batch File
```powershell
# Run as Administrator
.\InstallService.bat
```

### 3. Start the WPF Management Application
```powershell
cd GetVideoWPF\bin\Release\net9.0-windows
.\GetVideoWPF.exe -verb runas
```

## Configuration

### Service Configuration (appsettings.json)
```json
{
  "VideoDownload": {
    "ListenPort": 8080,
    "DownloadDirectory": "C:\\Downloads\\Videos",
    "MaxConcurrentDownloads": 5,
    "BufferSize": 8192
  }
}
```

### Logging
- Windows Event Log: Application logs under "GetVideoService"
- File Logs: Located in service directory
- Log levels: Information, Warning, Error

## Service Management

### Using WPF Application
1. Launch GetVideoWPF.exe
2. Use the **Service menu** to:
   - **Install Service**: Install the Windows Service (requires Admin privileges)
   - **Uninstall Service**: Remove the Windows Service (requires Admin privileges)
   - **Start Service**: Start the installed service
   - **Stop Service**: Stop the running service
   - **Refresh Status**: Update service status display
3. Or use the "Service Control" section for Start/Stop operations
4. Confirmation dialogs will appear for install/uninstall operations

### Using Command Line
```powershell
# Start service
net start GetVideoService

# Stop service
net stop GetVideoService

# Uninstall service
.\UninstallService.bat
```

## Troubleshooting

### Common Issues
1. **Permission Errors**: The application will automatically request Administrator privileges when needed
2. **UAC Cancelled**: If you cancel the UAC prompt, the operation will be cancelled gracefully
3. **Port Conflicts**: Check if port 8080 is already in use
4. **Service Won't Start**: Check Windows Event Log for errors
5. **Installation Failed**: Ensure you have Administrator rights when prompted by UAC

### Log Locations
- Windows Event Log: Event Viewer → Application logs
- Service Logs: GetVideoService directory
- WPF Application: Temp directory

## Development Notes

### Technologies Used
- **.NET 9.0**: Target framework
- **Windows Services**: Background processing
- **WPF + MVVM**: User interface
- **Serilog**: Logging framework
- **Dependency Injection**: Service container

### Project Dependencies
- GetVideoService → DownloadVideoOverTcpLib
- GetVideoWPF → DownloadVideoOverTcpLib
- Both projects are self-contained and ready for deployment

## Build Status
✅ All projects build successfully
✅ Windows Service ready for deployment
✅ WPF application ready for use
✅ Solution integrated and tested
