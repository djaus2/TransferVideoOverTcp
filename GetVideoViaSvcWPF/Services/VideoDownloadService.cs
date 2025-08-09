using DownloadVideoOverTCPLib;

namespace GetVideoWPF.Services;

public interface IVideoDownloadService
{
    Task StartListeningAsync(string folder, int port);
    void StopListening();
    bool IsListening { get; }
    event EventHandler<string>? VideoReceived;
}

public class VideoDownloadService : IVideoDownloadService
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listeningTask;

    public bool IsListening { get; private set; }

    public event EventHandler<string>? VideoReceived;

    public async Task StartListeningAsync(string folder, int port)
    {
        if (IsListening)
        {
            StopListening();
        }

        _cancellationTokenSource = new CancellationTokenSource();
        IsListening = true;

        _listeningTask = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var fileName = GetVideo.Download(folder, port);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        VideoReceived?.Invoke(this, fileName);
                    }
                }
                catch (Exception)
                {
                    // Handle or log exception as needed
                    await Task.Delay(1000, _cancellationTokenSource.Token); // Brief delay before retry
                }
            }
        }, _cancellationTokenSource.Token);

        await Task.CompletedTask;
    }

    public void StopListening()
    {
        if (!IsListening)
            return;

        _cancellationTokenSource?.Cancel();
        _listeningTask?.Wait(TimeSpan.FromSeconds(5));
        
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _listeningTask = null;
        
        IsListening = false;
    }
}
