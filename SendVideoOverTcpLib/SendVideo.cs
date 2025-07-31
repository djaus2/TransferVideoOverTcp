using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Security.Cryptography;

namespace SendVideoOverTCPLib
{
    // All the code in this file is included in all platforms.
    public class SendVideo
    {
        public static async void OnSendMovieFileClicked(object sender, EventArgs e)
        {
            var file = await PickMovieFileAsync();
            if (file is null)
                return;

            await SendFileWithChecksumAsync(file.FullPath, "192.168.0.9", 5000); // Use desktop's LAN IP
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
            await client.ConnectAsync(IPAddress.Parse(ipAddress), port);
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
    }
}
