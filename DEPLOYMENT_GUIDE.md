# Video Transfer Over TCP - Deployment Guide

## Project Overview

This solution contains multiple projects for transferring video over TCP:

### Existing Projects
- **DownloadVideoOverTcpLib**: Core library for video downloading functionality
- **GetVideoApp**: Original console application
- **SendVideo**: MAUI mobile application for sending videos
- **SendVideoOverTcpLib**: Library for sending video functionality

### New Projects Created
- **GetVideoService**: Windows Service for continuous video downloading
- **GetVideoWPF**: WPF management application for controlling the service

## Project Structure

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

### GetVideoService (Windows Service)
- Runs as a Windows background service
- Automatically starts video downloads when TCP connections are received
- Configurable through appsettings.json
- Logs to Windows Event Log and file system
- Self-contained deployment ready

### GetVideoWPF (Management Application)
- Modern WPF interface using MVVM pattern
- **Service menu with Install/Uninstall options**
- Service management (Start/Stop/Install/Uninstall)
- Manual video download capability
- Real-time status monitoring
- Settings configuration
- Confirmation dialogs for critical operations

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
3. **The system will automatically prompt for Administrator privileges (UAC)**
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
.\GetVideoWPF.exe
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
