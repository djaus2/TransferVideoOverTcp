using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;

namespace SendVideo
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {


            // Load your file as bytes
            byte[] fileBytes = File.ReadAllBytes(@"c:\temp\AAA\fghfggN.mp4");

            // Create TCP client and connect
            using var client = new TcpClient();
            await client.ConnectAsync("192.168.x.x", 5000); // Use desktop's LAN IP

            using var stream = client.GetStream();
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);

            await stream.FlushAsync();

        }
        private async void OnSendMovieFileClicked(object sender, EventArgs e)
        {
            var file = await PickMovieFileAsync();
            if (file is null)
                return;

            await SendFileWithChecksumAsync(file.FullPath, "192.168.0.9", 5000); // Use desktop's LAN IP
            /*var fileBytes = File.ReadAllBytes(file.FullPath);

            using var client = new TcpClient();
            await client.ConnectAsync("192.168.0.9", 5000); // desktop IP

            using var stream = client.GetStream();
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);

            await stream.FlushAsync();*/
        }



        private async Task<FileResult?> PickMovieFileAsync()
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

        public async Task SendFileAsync(string filePath, string ipAddress, int port)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ipAddress), port);

            using var stream = client.GetStream();
            string fileName = Path.GetFileName(filePath);
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);

            // 1️⃣ Send filename length (Int32 - 4 bytes)
            await stream.WriteAsync(fileNameLength, 0, fileNameLength.Length);

            // 2️⃣ Send filename (UTF-8 bytes)
            await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

            // 3️⃣ Send file contents
            using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(stream);

            // Optional: flush and close
            //await stream.FlushAsync();
        }

        const int ChunkSize = 1024 * 1024; // 1MB

        public async Task SendFileInChunksAsync(string filePath, string ipAddress, int port)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ipAddress), port);

            using var stream = client.GetStream();

            // 1️⃣ Send filename header
            string fileName = Path.GetFileName(filePath);
            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await stream.WriteAsync(nameLength);
            await stream.WriteAsync(nameBytes);

            // 2️⃣ Send file contents in chunks
            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }

            // Optional: stream.FlushAsync() isn’t strictly needed here
        }


        public async Task SendFileWithChecksumAsync(string filePath, string ipAddress, int port)
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
