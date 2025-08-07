using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GetVideoService.Services;

public interface IServiceEventNotifier
{
    Task NotifyVideoDownloadStarted(string fileName);
    Task NotifyVideoDownloadCompleted(string fileName);
    Task NotifyVideoDownloadFailed(string fileName, string error);
    Task NotifyServiceStatusChanged(string status);
}

public class ServiceEventNotifier : IServiceEventNotifier, IDisposable
{
    private readonly ILogger<ServiceEventNotifier> _logger;
    private NamedPipeServerStream? _pipeServer;
    private StreamWriter? _writer;
    private readonly string _pipeName = "GetVideoServiceEvents";
    private bool _disposed;

    public ServiceEventNotifier(ILogger<ServiceEventNotifier> logger)
    {
        _logger = logger;
        InitializePipe();
    }

    private void InitializePipe()
    {
        try
        {
            _pipeServer = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            // Start waiting for client connection in background
            Task.Run(async () =>
            {
                try
                {
                    await _pipeServer.WaitForConnectionAsync();
                    _writer = new StreamWriter(_pipeServer) { AutoFlush = true };
                    _logger.LogInformation("Named pipe client connected for event notifications");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to establish named pipe connection");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize named pipe server");
        }
    }

    public async Task NotifyVideoDownloadStarted(string fileName)
    {
        await SendEvent(new ServiceEvent
        {
            Type = "VideoDownloadStarted",
            FileName = fileName,
            Timestamp = DateTime.Now
        });
    }

    public async Task NotifyVideoDownloadCompleted(string fileName)
    {
        await SendEvent(new ServiceEvent
        {
            Type = "VideoDownloadCompleted",
            FileName = fileName,
            Timestamp = DateTime.Now
        });
    }

    public async Task NotifyVideoDownloadFailed(string fileName, string error)
    {
        await SendEvent(new ServiceEvent
        {
            Type = "VideoDownloadFailed",
            FileName = fileName,
            Error = error,
            Timestamp = DateTime.Now
        });
    }

    public async Task NotifyServiceStatusChanged(string status)
    {
        await SendEvent(new ServiceEvent
        {
            Type = "ServiceStatusChanged",
            Status = status,
            Timestamp = DateTime.Now
        });
    }

    private async Task SendEvent(ServiceEvent serviceEvent)
    {
        if (_writer == null || _pipeServer?.IsConnected != true)
        {
            return; // No client connected
        }

        try
        {
            var json = JsonSerializer.Serialize(serviceEvent);
            await _writer.WriteLineAsync(json);
            _logger.LogDebug("Sent event: {EventType}", serviceEvent.Type);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send event notification");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _writer?.Dispose();
        _pipeServer?.Dispose();
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
