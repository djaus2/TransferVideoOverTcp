using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DownloadVideoOverTCPLib;
using System.Net;
using System.Net.Sockets;

namespace GetVideoService;

public class VideoDownloadWorker : BackgroundService
{
    private readonly ILogger<VideoDownloadWorker> _logger;
    private readonly ServiceSettings _settings;
    private CancellationTokenSource? _downloadCancellationTokenSource;

    public VideoDownloadWorker(ILogger<VideoDownloadWorker> logger, IOptions<ServiceSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Add log separator for new service session
        var separator = new string('=', 80);
        _logger.LogInformation("{Separator}", separator);
        _logger.LogInformation("NEW SERVICE SESSION STARTED - {Timestamp}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        _logger.LogInformation("Video Download Service started");
        _logger.LogInformation("{Separator}", separator);
        
        // Ensure the download folder exists
        if (!Directory.Exists(_settings.Folder))
        {
            Directory.CreateDirectory(_settings.Folder);
            _logger.LogInformation("Created download folder: {Folder}", _settings.Folder);
        }

        // Get local IP address
        string localIP = GetLocalIPAddress();
        _logger.LogInformation("Service listening on IP: {LocalIP}, Port: {Port}", localIP, _settings.Port);

        if (_settings.AutoStart)
        {
            await StartListening(stoppingToken);
        }
        else
        {
            // Wait for external trigger or cancellation
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }
        }
    }

    public async Task StartListening(CancellationToken cancellationToken = default)
    {
        _downloadCancellationTokenSource?.Cancel();
        _downloadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await Task.Run(() =>
            {
                while (!_downloadCancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Waiting for video download connection...");
                        
                        // Redirect Console output to capture library messages
                        var originalOut = Console.Out;
                        var stringWriter = new StringWriter();
                        Console.SetOut(stringWriter);
                        
                        var filePath = GetVideo.Download(_settings.Folder, _settings.Port);
                        
                        // Restore console output and capture the messages
                        Console.SetOut(originalOut);
                        var consoleOutput = stringWriter.ToString();
                        
                        // Log each line from the library with DOWNLOAD prefix for easy detection
                        if (!string.IsNullOrEmpty(consoleOutput))
                        {
                            var lines = consoleOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    _logger.LogInformation("DOWNLOAD: {Message}", line);
                                }
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            _logger.LogInformation("Video file received: {FilePath}", filePath);
                        }
                        else
                        {
                            _logger.LogWarning("Video download failed or was cancelled");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during video download");
                        // Add a delay before retrying to prevent rapid failures
                        Task.Delay(5000, _downloadCancellationTokenSource.Token).Wait();
                    }
                }
            }, _downloadCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Video download listening was cancelled");
        }
    }

    public void StopListening()
    {
        _downloadCancellationTokenSource?.Cancel();
        _logger.LogInformation("Video download listening stopped");
    }

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "No network adapters with an IPv4 address in the system!";
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Video Download Service is stopping");
        StopListening();
        
        try
        {
            await base.StopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // This is expected during service shutdown, log as info rather than error
            _logger.LogInformation("Service stop completed (cancellation requested)");
        }
    }
}
