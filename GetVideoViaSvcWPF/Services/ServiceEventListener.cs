using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO;

namespace GetVideoWPF.Services;

public interface IServiceEventListener
{
    event EventHandler<VideoDownloadEventArgs>? VideoDownloadStarted;
    event EventHandler<VideoDownloadEventArgs>? VideoDownloadCompleted;
    event EventHandler<VideoDownloadEventArgs>? VideoDownloadFailed;
    event EventHandler<ServiceStatusEventArgs>? ServiceStatusChanged;
    Task StartListening();
    void StopListening();
}

public class ServiceEventListener : IServiceEventListener, IDisposable
{
    private readonly ILogger<ServiceEventListener> _logger;
    private NamedPipeClientStream? _pipeClient;
    private StreamReader? _reader;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _pipeName = "GetVideoServiceEvents";
    private bool _disposed;

    public event EventHandler<VideoDownloadEventArgs>? VideoDownloadStarted;
    public event EventHandler<VideoDownloadEventArgs>? VideoDownloadCompleted;
    public event EventHandler<VideoDownloadEventArgs>? VideoDownloadFailed;
    public event EventHandler<ServiceStatusEventArgs>? ServiceStatusChanged;

    public ServiceEventListener(ILogger<ServiceEventListener> logger)
    {
        _logger = logger;
    }

    public async Task StartListening()
    {
        if (_cancellationTokenSource != null)
        {
            return; // Already listening
        }

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.In);
            
            _logger.LogInformation("Connecting to service event pipe...");
            
            // Connect with timeout
            await _pipeClient.ConnectAsync(5000, _cancellationTokenSource.Token);
            _reader = new StreamReader(_pipeClient);
            
            _logger.LogInformation("Connected to service event notifications");

            // Start reading events
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested && _pipeClient.IsConnected)
                    {
                        var line = await _reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            ProcessEvent(line);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Error reading from service event pipe");
                }
            }, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to service event pipe");
        }
    }

    public void StopListening()
    {
        _cancellationTokenSource?.Cancel();
        _reader?.Dispose();
        _pipeClient?.Dispose();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private void ProcessEvent(string eventJson)
    {
        try
        {
            var serviceEvent = JsonSerializer.Deserialize<ServiceEvent>(eventJson);
            if (serviceEvent == null) return;

            _logger.LogDebug("Received event: {EventType}", serviceEvent.Type);

            switch (serviceEvent.Type)
            {
                case "VideoDownloadStarted":
                    VideoDownloadStarted?.Invoke(this, new VideoDownloadEventArgs
                    {
                        FileName = serviceEvent.FileName ?? "Unknown",
                        Timestamp = serviceEvent.Timestamp
                    });
                    break;

                case "VideoDownloadCompleted":
                    VideoDownloadCompleted?.Invoke(this, new VideoDownloadEventArgs
                    {
                        FileName = serviceEvent.FileName ?? "Unknown",
                        Timestamp = serviceEvent.Timestamp
                    });
                    break;

                case "VideoDownloadFailed":
                    VideoDownloadFailed?.Invoke(this, new VideoDownloadEventArgs
                    {
                        FileName = serviceEvent.FileName ?? "Unknown",
                        Error = serviceEvent.Error,
                        Timestamp = serviceEvent.Timestamp
                    });
                    break;

                case "ServiceStatusChanged":
                    ServiceStatusChanged?.Invoke(this, new ServiceStatusEventArgs
                    {
                        Status = serviceEvent.Status ?? "Unknown",
                        Timestamp = serviceEvent.Timestamp
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process service event");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopListening();
        _disposed = true;
    }
}

public class ServiceEvent
{
    public string Type { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class VideoDownloadEventArgs : EventArgs
{
    public string FileName { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ServiceStatusEventArgs : EventArgs
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
