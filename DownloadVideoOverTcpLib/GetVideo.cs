using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Security.Cryptography;

namespace DownloadVideoOverTCPLib
{
    public static class GetVideo
    {
        const int ChunkSize = 1024 * 1024; // 1MB
    public static string Download(string fileFolder = @"C:\temp",int port = 5000 )
    {
        // Display local IP address
        string? localIP = Dns.GetHostEntry(Dns.GetHostName())
                            .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

        if (string.IsNullOrEmpty(localIP))
        {
            Console.WriteLine("Local IP address not found. Please check your network connection.");
            return ""; // Exit early — no point starting the server
        }
        else
        {
            Console.WriteLine($"Listening on IP: {localIP}, Port: {port}");
        }


        try
        {
            if (!Directory.Exists(fileFolder))
                Directory.CreateDirectory(fileFolder);

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Waiting for connection...");

            using var client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected - starting download...");
            using var networkStream = client.GetStream();

            byte[] nameLengthBytes = new byte[sizeof(int)];
            ReadExact(networkStream, nameLengthBytes, 0, sizeof(int));
            int nameLength = BitConverter.ToInt32(nameLengthBytes, 0);

            byte[] nameBuffer = new byte[nameLength];
            ReadExact(networkStream, nameBuffer, 0, nameLength);
            string fileName = Encoding.UTF8.GetString(nameBuffer);
            Console.WriteLine($"Receiving file: {fileName}");

            byte[] expectedChecksum = new byte[32]; // SHA256
            ReadExact(networkStream, expectedChecksum, 0, 32);
            string filePath = Path.Combine(fileFolder, fileName);
            using var fileStream = File.Create(filePath);
            using var sha256 = SHA256.Create();

            Console.WriteLine("Downloading video data...");
            byte[] buffer = new byte[1024 * 1024];
            int bytesRead;
            long totalBytes = 0;
            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                totalBytes += bytesRead;
            }
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            Console.WriteLine($"Download completed. Total bytes: {totalBytes:N0}");

            byte[] actualChecksum = sha256.Hash!;

            // Inject an error for testing
            //expectedChecksum[0] ^= 0xFF; // Flip the first byte to corrupt it


            bool isValid = expectedChecksum.SequenceEqual(actualChecksum);
            Console.WriteLine($"Expected Checksum: {BitConverter.ToString(expectedChecksum).Replace("-", "")}");
            Console.WriteLine($"Actual Checksum: {BitConverter.ToString(actualChecksum).Replace("-", "")}");
            Console.WriteLine(isValid ? "✅ File received successfully" : "❌ Checksum mismatch!");

            listener.Stop();
            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during transfer: {ex.Message}");
            return "";
        }
    }        public static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    throw new EndOfStreamException("Stream ended before reading expected bytes.");
                totalRead += bytesRead;
            }
        }
    }
}
