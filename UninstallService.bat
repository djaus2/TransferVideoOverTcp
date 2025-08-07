@echo off
echo Uninstalling GetVideoService...

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo Stopping service...
sc stop GetVideoService

echo Removing service...
sc delete GetVideoService

if %errorLevel% equ 0 (
    echo Service uninstalled successfully!
) else (
    echo Failed to uninstall service. Error code: %errorLevel%
)

pause
