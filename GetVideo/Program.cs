using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using DownloadVideoOverTCPLib;



using System.Security.Cryptography;


namespace Transfer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Phone World!");
            string folder = @"C:\temp\AAA";
            var filepath = GetVideo.Download(folder);

            Console.WriteLine($"File received: {filepath} in folder {folder}");
        }

        public static void Transfer2(string fileName, string fileFolder = @"C:\tempAAA", int port = 5000)
        {
            try
            {
                if (!Directory.Exists(fileFolder))
                {
                    Directory.CreateDirectory(fileFolder);
                }

                string savePath = Path.Combine(fileFolder, fileName);

                var listener = new TcpListener(IPAddress.Any, port);
                listener.Server.ReceiveTimeout = 10000; // 10 seconds

                listener.Start();
                Console.WriteLine("Waiting for connection...");

                using var client = listener.AcceptTcpClient();
                var remoteEndPoint = client.Client.RemoteEndPoint;
                Console.WriteLine($"Connected to {remoteEndPoint}");

                using var networkStream = client.GetStream();
                using var fileStream = File.Create(savePath);
                networkStream.CopyTo(fileStream);


                Console.WriteLine("File received and saved.");
                listener.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during transfer: {ex.Message}");

            }
        }


        public static void Transfer(int port = 5000, string fileFolder = @"C:\temp")
        {
            // Display local IP address
            string? localIP = Dns.GetHostEntry(Dns.GetHostName())
                                .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

            if (string.IsNullOrEmpty(localIP))
            {
                Console.WriteLine("Local IP address not found. Please check your network connection.");
                return; // Exit early — no point starting the server
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
                using var networkStream = client.GetStream();

                byte[] nameLengthBytes = new byte[sizeof(int)];
                ReadExact(networkStream, nameLengthBytes, 0, sizeof(int));
                int nameLength = BitConverter.ToInt32(nameLengthBytes, 0);

                byte[] nameBuffer = new byte[nameLength];
                ReadExact(networkStream, nameBuffer, 0, nameLength);
                string fileName = Encoding.UTF8.GetString(nameBuffer);

                byte[] expectedChecksum = new byte[32]; // SHA256
                ReadExact(networkStream, expectedChecksum, 0, 32);

                using var fileStream = File.Create(fileName);
                using var sha256 = SHA256.Create();

                byte[] buffer = new byte[1024 * 1024];
                int bytesRead;
                while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                byte[] actualChecksum = sha256.Hash!;

                // Inject an error for testing
                //expectedChecksum[0] ^= 0xFF; // Flip the first byte to corrupt it


                bool isValid = expectedChecksum.SequenceEqual(actualChecksum);
                Console.WriteLine(isValid ? "✅ File received successfully" : "❌ Checksum mismatch!");

                listener.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during transfer: {ex.Message}");
            }
        }

        public static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
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

