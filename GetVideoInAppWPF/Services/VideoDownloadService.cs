using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DownloadVideoOverTCPLib;

namespace GetVideoNow.Services
{
    public class VideoDownloadService : IVideoDownloadService
    {
        private readonly IConfiguration _configuration;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listeningTask;

        public event EventHandler<string>? VideoDownloadStarted;
        public event EventHandler<string>? VideoDownloadCompleted;
        public event EventHandler<Exception>? VideoDownloadFailed;

        public bool IsListening => _listeningTask != null && !_listeningTask.IsCompleted;

        public VideoDownloadService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task StartListeningAsync(string folder, int port, CancellationToken cancellationToken)
        {
            if (IsListening)
            {
                // Already listening, don't start again
                return;
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            _listeningTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Ensure the directory exists
                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }

                            // Notify that we're waiting for a connection
                            VideoDownloadStarted?.Invoke(this, "Waiting for connection...");

                            // Use the DownloadVideoOverTcpLib to receive the file
                            string fileName = null;
                            
                            try
                            {
                                // The Download method blocks until a connection is established
                                // We need to notify when an actual file transfer starts
                                // Since we can't modify the library, we'll use a workaround
                                
                                // Create a task to monitor for connection and notify when it happens
                                var connectionTask = Task.Run(async () => 
                                {
                                    // Wait a bit to let the Download method establish a connection
                                    await Task.Delay(500);
                                    
                                    // Notify with a special message that will trigger the popup
                                    // This will happen while the Download method is still receiving the file
                                    VideoDownloadStarted?.Invoke(this, "File download in progress");
                                });
                                
                                // Get the file name from the Download method
                                // This will block until a connection is established and file transfer completes
                                fileName = DownloadVideoOverTCPLib.GetVideo.Download(folder, port);

                                // If we got a valid file name, it means we have an actual file
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    // Notify that download completed successfully with the actual filename
                                    // This will trigger the popup to show
                                    VideoDownloadCompleted?.Invoke(this, fileName);
                                    
                                    // Short delay before starting to listen again
                                    if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        await Task.Delay(500, _cancellationTokenSource.Token);
                                    }
                                    else
                                    {
                                        await Task.Delay(500);
                                    }
                                    
                                    // Reset the UI state for the next download
                                    VideoDownloadStarted?.Invoke(this, "Waiting for next connection...");
                                    
                                    // Continue to the next iteration of the loop
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Check if we're still supposed to be listening
                                if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    // We're not supposed to be listening anymore, so just break out
                                    break;
                                }
                                
                                VideoDownloadFailed?.Invoke(this, ex);
                                
                                if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    await Task.Delay(1000, _cancellationTokenSource.Token);
                                }
                                else
                                {
                                    await Task.Delay(1000);
                                }
                                continue;
                            }

                            // We only reach here if fileName is null or empty but no exception was thrown
                            if (_cancellationTokenSource != null && _cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                break;
                            }
                            else
                            {
                                // If there was an error but not due to cancellation
                                VideoDownloadFailed?.Invoke(this, new Exception("Download failed or was interrupted"));
                                
                                // Wait a bit before retrying
                                if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    await Task.Delay(1000, _cancellationTokenSource.Token);
                                }
                                else
                                {
                                    await Task.Delay(1000);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when cancellation is requested
                            break;
                        }
                        catch (Exception ex)
                        {
                            // Check if we're still supposed to be listening
                            if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                // We're not supposed to be listening anymore, so just break out
                                break;
                            }
                            
                            // Notify of any errors
                            VideoDownloadFailed?.Invoke(this, ex);
                            
                            // Wait a bit before retrying
                            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await Task.Delay(1000, _cancellationTokenSource.Token);
                            }
                            else
                            {
                                await Task.Delay(1000);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    // Check if we're still supposed to be listening
                    if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // We're not supposed to be listening anymore, so just break out
                        return;
                    }
                    
                    VideoDownloadFailed?.Invoke(this, ex);
                }
                finally
                {
                    _cancellationTokenSource = null;
                }
            }, _cancellationTokenSource.Token);

            await Task.CompletedTask;
        }

        public void StopListening()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        public string[] GetDownloadedFiles(string folder)
        {
            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            return Directory.GetFiles(folder)
                .Select(Path.GetFileName)
                .ToArray();
        }
    }
}
