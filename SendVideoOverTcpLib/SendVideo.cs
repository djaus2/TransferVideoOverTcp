using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using SendVideoOverTCPLib.ViewModels;


namespace SendVideoOverTCPLib
{
    // All the code in this file is included in all platforms.
    public static class SendVideo
    {
 
        /*public static int MaxIPAddress { get; set; } = 20;
        public static int MinIPAddress { get; set; } = 2;  //Router is often XX.YY.ZZ.1
        public static int TimeoutInHalfSeconds { get; set; } = 6;*/
        public static NetworkViewModel? NetworkViewModel { 
            get; 
            set; 
        } 



        public static async Task OnSendMovieFileClicked(NetworkViewModel _networkViewModel)
        {
            var file = await PickMovieFileAsync();
            if (file is null)
                return;

            try
            {
                // Set busy state to show the indicator
                await SendFileWithChecksumAsync(file.FullPath, _networkViewModel.SelectedIpAddress, _networkViewModel.SelectedPort);
            }
            catch (Exception ex)
            {
                // Show error popup
                await Application.Current.MainPage.DisplayAlert("Connection Error",
                    $"Could not connect to receiver. Please ensure the receiver app is running and listening on port {_networkViewModel.SelectedPort}.",
                    "OK");
            }
            finally
            {
                // Always clear busy state when done
            }
        }

        private static async Task<FileResult?> PickMovieFileAsync()
        {
            var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/*" } }, // targets all video types
            });

            var options = new PickOptions
            {
                PickerTitle = "Select a Movie File",
                FileTypes = customFileTypes
            };

            return await FilePicker.PickAsync(options);
        }

        public static async Task SendFileWithChecksumAsync(string filePath, string ipAddress, int port)
        {

            using var client = new TcpClient();
            try
            {
                // Get the configurable timeout from NetworkViewModel - access directly from the static instance
                int timeoutMs = SendVideo.NetworkViewModel.DownloadTimeoutInSec*1000;

                // Set a connection timeout based on the configurable setting
                var connectTask = client.ConnectAsync(IPAddress.Parse(ipAddress), port);

                // Wait for connection with configurable timeout
                if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) != connectTask)
                {
                    // Connection timed out
                    throw new TimeoutException($"Connection to {ipAddress}:{port} timed out after {timeoutMs/1000} seconds. Please ensure the receiver is listening.");
                }

                // Make sure the connection task completed successfully
                await connectTask;
            }
            catch (SocketException ex)
            {
                throw new Exception("Failed to connect to receiver", ex);
            }
            using var stream = client.GetStream();

            // Send filename
            string fileName = Path.GetFileName(filePath);
            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await stream.WriteAsync(nameLength);
            await stream.WriteAsync(nameBytes);

            // Calculate checksum
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            using var sha256 = SHA256.Create();
            byte[] checksum = sha256.ComputeHash(fileBytes);
            await stream.WriteAsync(checksum); // 32 bytes for SHA256

            // Send file in chunks
            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[1024 * 1024]; // 1MB
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Get list of local active IpAddresses 
        /// Uses this phones's subnet
        /// Excluding this phone's
        /// </summary>
        /// <returns>List of IpAddresses.</returns>
        public static async Task<List<string>> GetLocalActiveDevices()
        {
            string localIP = GetLocalPhoneIPAddress(); // e.g., 192.168.1.42
            string subnet = localIP.Substring(0, localIP.LastIndexOf('.') + 1); // e.g., 192.168.1.
            List<string> activeIPs = new List<string>();
            for (int i = NetworkViewModel.StartHostId; i <= NetworkViewModel.EndHostId; i++)
            {
                string ip = $"{subnet}{i}";
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, NetworkViewModel.PingTimeoutInMs);
                if (reply.Status == IPStatus.Success)
                {
                    if(ip != localIP) // Exclude the local IP
                        activeIPs.Add(ip);
                }
            }

            return activeIPs;

        }

        static string GetLocalPhoneIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "No active IPv4 network adapters found.";
        }

        /// <summary>
        /// Get Observable Collection of local active IpAddresses.
        /// Excluding this phone's
        /// And excluding Subnet.1, oftehn the router.
        /// </summary>
        /// <returns>IPAddress if only one.</returns>
        public static async Task<string> GetIps()
        {;
            var ips = await GetLocalActiveDevices();
            NetworkViewModel.ActiveIPs = new ObservableCollection<string>(ips);
            if (NetworkViewModel.ActiveIPs.Count == 1)
            {
                NetworkViewModel.SelectedIpAddress = NetworkViewModel.ActiveIPs[0];
                return NetworkViewModel.SelectedIpAddress;
            }
            else
            {
                NetworkViewModel.SelectedIpAddress = "";
            }
            return "";
        }



        public static async Task<string> GetSettings(bool checkSettings = true)
        {
            NetworkViewModel = Settings.GetSettingNetworkViewModel();
            NetworkViewModel.ActiveIPs = new();
            string ip = "";
            if(checkSettings)
                ip = await TryRestoreSelectedIpAsync();
            if (string.IsNullOrEmpty(ip))
            { 
                try
                {
                    ip = await GetIps();
                    Settings.SaveSelectedSettings(NetworkViewModel);
                }
                catch (Exception ex)
                {
                    // Handle exceptions, e.g., log them or show an alert
                    Console.WriteLine($"Error retrieving IPs: {ex.Message}");
                    return ""; // Return an empty on error
                }   
            }
            return ip;
        }

        public static async Task<string> TryRestoreSelectedIpAsync()
        {

            string savedIp = NetworkViewModel.SelectedIpAddress;
            if (!string.IsNullOrEmpty(savedIp))
            {
                using var ping = new Ping();
                try
                {
                    var reply = await ping.SendPingAsync(savedIp, NetworkViewModel.PingTimeoutInHalfSeconds * 500);
                    if (reply.Status == IPStatus.Success)
                    {
                        return savedIp;
                    }
                }
                catch
                {
                    // Optionally log or ignore
                }
            }
            return "";
        }

    }
}
