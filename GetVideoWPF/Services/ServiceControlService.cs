using System.ServiceProcess;
using System.Diagnostics;
using System.IO;

namespace GetVideoWPF.Services;

public interface IServiceControlService
{
    Task StartServiceAsync();
    Task StopServiceAsync();
    Task InstallServiceAsync();
    Task UninstallServiceAsync();
    ServiceControllerStatus GetServiceStatus();
    bool IsServiceInstalled();
}

public class ServiceControlService : IServiceControlService
{
    private const string ServiceName = "GetVideoService";

    public async Task StartServiceAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            service.Refresh();
            
            if (service.Status == ServiceControllerStatus.Running)
            {
                return; // Already running
            }
            
            if (service.Status == ServiceControllerStatus.StartPending)
            {
                await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
                return;
            }
            
            service.Start();
            await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Failed to start service '{ServiceName}'. " +
                "This might be due to:\n" +
                "• Service configuration issues\n" +
                "• Missing dependencies\n" +
                "• Insufficient permissions\n" +
                "• Service executable path problems\n" +
                "• Log directory permissions (service writes to C:\\Logs)\n\n" +
                "Check Windows Event Viewer (Windows Logs > System) for detailed error information.\n\n" +
                $"Original error: {ex.Message}", ex);
        }
        catch (System.TimeoutException)
        {
            throw new System.TimeoutException($"Service '{ServiceName}' failed to start within 30 seconds. " +
                "Check Windows Event Log for detailed error information.");
        }
    }

    public async Task StopServiceAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            service.Refresh();
            
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                return; // Already stopped
            }
            
            if (service.Status == ServiceControllerStatus.StopPending)
            {
                await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
                return;
            }
            
            service.Stop();
            await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Failed to stop service '{ServiceName}': {ex.Message}", ex);
        }
        catch (System.TimeoutException)
        {
            throw new System.TimeoutException($"Service '{ServiceName}' failed to stop within 30 seconds.");
        }
    }

    public async Task InstallServiceAsync()
    {
        var serviceExePath = GetServiceExecutablePath();
        if (!File.Exists(serviceExePath))
        {
            throw new FileNotFoundException($"Service executable not found at: {serviceExePath}\n\n" +
                "Please ensure the GetVideoService project has been built. You can:\n" +
                "1. Build the entire solution in Visual Studio\n" +
                "2. Run 'dotnet build' from the solution directory\n" +
                "3. Build the GetVideoService project specifically");
        }

        // Use PowerShell to run sc with elevation
        var powerShellScript = $@"
# Create logs directory if it doesn't exist
if (!(Test-Path 'C:\Logs')) {{
    New-Item -ItemType Directory -Path 'C:\Logs' -Force
}}

# Install the service
$result = Start-Process -FilePath 'sc' -ArgumentList 'create','{ServiceName}','binPath=""{serviceExePath}""','start=demand' -Verb RunAs -Wait -PassThru
if ($result.ExitCode -ne 0) {{
    Write-Host ""Failed to create service. Exit code: $($result.ExitCode)""
    exit $result.ExitCode
}}

# Set service description
Start-Process -FilePath 'sc' -ArgumentList 'description','{ServiceName}','Video Download Service - Listens for TCP video transfers' -Verb RunAs -Wait

Write-Host ""Service installed successfully""
";

        await ExecutePowerShellWithElevation(powerShellScript);
    }

    public async Task UninstallServiceAsync()
    {
        // First stop the service if it's running and installed
        if (IsServiceInstalled())
        {
            try
            {
                using var service = new ServiceController(ServiceName);
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    var stopScript = $@"
$result = Start-Process -FilePath 'sc' -ArgumentList 'stop','{ServiceName}' -Verb RunAs -Wait -PassThru
if ($result.ExitCode -ne 0) {{
    Write-Host ""Warning: Failed to stop service. Exit code: $($result.ExitCode)""
}}
";
                    await ExecutePowerShellWithElevation(stopScript);
                    await Task.Delay(3000); // Wait for service to stop
                }
            }
            catch
            {
                // Ignore errors when stopping before uninstall
            }
        }

        // Use PowerShell to run sc delete with elevation
        var powerShellScript = $@"
$result = Start-Process -FilePath 'sc' -ArgumentList 'delete','{ServiceName}' -Verb RunAs -Wait -PassThru
if ($result.ExitCode -eq 0) {{
    Write-Host ""Service uninstalled successfully""
}} else {{
    Write-Host ""Failed to uninstall service. Exit code: $($result.ExitCode)""
    exit $result.ExitCode
}}
";
        await ExecutePowerShellWithElevation(powerShellScript);
    }

    public ServiceControllerStatus GetServiceStatus()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            return service.Status;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException($"Service '{ServiceName}' is not installed.");
        }
    }

    public bool IsServiceInstalled()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            _ = service.Status; // This will throw if service doesn't exist
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private string GetServiceExecutablePath()
    {
        // Get the current application directory and solution directory
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var workingDir = Environment.CurrentDirectory;
        
        // When running from Visual Studio, we need to look for the service in various locations
        var possiblePaths = new[]
        {
            // Same directory as WPF app (for deployment)
            Path.Combine(currentDir, "GetVideoService.exe"),
            
            // From GetVideoWPF Debug output, look for GetVideoService Debug output
            Path.Combine(currentDir, "..", "..", "..", "..", "GetVideoService", "bin", "Debug", "net9.0", "win-x64", "GetVideoService.exe"),
            Path.Combine(currentDir, "..", "..", "..", "..", "GetVideoService", "bin", "Debug", "net9.0", "GetVideoService.exe"),
            
            // From GetVideoWPF Release output, look for GetVideoService Release output  
            Path.Combine(currentDir, "..", "..", "..", "..", "GetVideoService", "bin", "Release", "net9.0", "win-x64", "GetVideoService.exe"),
            Path.Combine(currentDir, "..", "..", "..", "..", "GetVideoService", "bin", "Release", "net9.0", "GetVideoService.exe"),
            
            // Working directory relative paths (when running from VS)
            Path.Combine(workingDir, "..", "GetVideoService", "bin", "Debug", "net9.0", "win-x64", "GetVideoService.exe"),
            Path.Combine(workingDir, "..", "GetVideoService", "bin", "Debug", "net9.0", "GetVideoService.exe"),
            Path.Combine(workingDir, "..", "GetVideoService", "bin", "Release", "net9.0", "win-x64", "GetVideoService.exe"),
            Path.Combine(workingDir, "..", "GetVideoService", "bin", "Release", "net9.0", "GetVideoService.exe"),
            
            // Solution root relative paths
            Path.Combine(workingDir, "GetVideoService", "bin", "Debug", "net9.0", "win-x64", "GetVideoService.exe"),
            Path.Combine(workingDir, "GetVideoService", "bin", "Debug", "net9.0", "GetVideoService.exe"),
            Path.Combine(workingDir, "GetVideoService", "bin", "Release", "net9.0", "win-x64", "GetVideoService.exe"),
            Path.Combine(workingDir, "GetVideoService", "bin", "Release", "net9.0", "GetVideoService.exe")
        };

        // Debug: Log the paths we're checking
        System.Diagnostics.Debug.WriteLine($"Current Directory: {currentDir}");
        System.Diagnostics.Debug.WriteLine($"Working Directory: {workingDir}");
        
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            System.Diagnostics.Debug.WriteLine($"Checking: {fullPath}");
            if (File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"Found service at: {fullPath}");
                return fullPath;
            }
        }

        // If not found, let's build the service first
        var serviceProjectPath = FindServiceProjectPath();
        if (!string.IsNullOrEmpty(serviceProjectPath))
        {
            System.Diagnostics.Debug.WriteLine($"Service project found at: {serviceProjectPath}");
            // Try to build the service project
            var builtPath = TryBuildServiceProject(serviceProjectPath);
            if (!string.IsNullOrEmpty(builtPath) && File.Exists(builtPath))
            {
                return builtPath;
            }
        }

        // Default fallback - assume it's in the same directory
        var fallbackPath = Path.Combine(currentDir, "GetVideoService.exe");
        System.Diagnostics.Debug.WriteLine($"Using fallback path: {fallbackPath}");
        return fallbackPath;
    }

    private string FindServiceProjectPath()
    {
        var workingDir = Environment.CurrentDirectory;
        var possibleProjectPaths = new[]
        {
            Path.Combine(workingDir, "..", "GetVideoService", "GetVideoService.csproj"),
            Path.Combine(workingDir, "GetVideoService", "GetVideoService.csproj"),
            Path.Combine(workingDir, "..", "..", "GetVideoService", "GetVideoService.csproj")
        };

        foreach (var projectPath in possibleProjectPaths)
        {
            var fullPath = Path.GetFullPath(projectPath);
            if (File.Exists(fullPath))
            {
                return Path.GetDirectoryName(fullPath);
            }
        }

        return null;
    }

    private string TryBuildServiceProject(string projectDirectory)
    {
        try
        {
            // Try to build the service project
            var projectFile = Path.Combine(projectDirectory, "GetVideoService.csproj");
            if (!File.Exists(projectFile))
                return null;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectFile}\" --configuration Debug --output \"{Path.Combine(projectDirectory, "bin", "Debug", "net9.0")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = projectDirectory
            };

            using var process = Process.Start(processStartInfo);
            process?.WaitForExit(30000); // 30 second timeout

            if (process?.ExitCode == 0)
            {
                var builtExecutable = Path.Combine(projectDirectory, "bin", "Debug", "net9.0", "GetVideoService.exe");
                if (File.Exists(builtExecutable))
                {
                    return builtExecutable;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to build service: {ex.Message}");
        }

        return null;
    }

    private async Task ExecutePowerShellWithElevation(string script)
    {
        var encodedScript = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-EncodedCommand {encodedScript}",
            UseShellExecute = true,
            Verb = "runas", // This triggers UAC elevation
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"PowerShell command failed with exit code: {process.ExitCode}");
                }
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            throw new OperationCanceledException("User cancelled the elevation request.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to execute elevated PowerShell command: {ex.Message}", ex);
        }
    }
}
