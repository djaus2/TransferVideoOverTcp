@echo off
echo Installing GetVideoService...

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

REM Get the directory where this batch file is located
set "SERVICE_PATH=%~dp0GetVideoService.exe"

echo Service path: %SERVICE_PATH%

REM Check if service executable exists
if not exist "%SERVICE_PATH%" (
    echo Error: GetVideoService.exe not found at %SERVICE_PATH%
    echo Please build the service project first.
    pause
    exit /b 1
)

REM Install the service
echo Installing service...
sc create GetVideoService binPath= "%SERVICE_PATH%" start= auto DisplayName= "Get Video Service"

if %errorLevel% equ 0 (
    echo Service installed successfully!
    echo Starting service...
    sc start GetVideoService
    
    if %errorLevel% equ 0 (
        echo Service started successfully!
    ) else (
        echo Warning: Service installed but failed to start. You can start it manually from Services.msc
    )
) else (
    echo Failed to install service. Error code: %errorLevel%
)

pause
