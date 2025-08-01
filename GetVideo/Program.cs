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
            string folder = @"C:\temp\AAA";
            int port = 5000;

            if (args.Length > 0)
            {
                if (!string.IsNullOrEmpty(args[0]))
                {
                    string arg = args[0];
                    if (arg.Contains("/?") || arg.Contains("--help", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Usage: Transfer.exe [folder] [port]\n" +
                            "folder: The folder to save the video (default is C:\\temp\\AAA)\n" +
                            "port: The port to listen on (default is 5000)");
                        return;
                    }
                }

                if (args.Length > 1)
                {
                    if (int.TryParse(args[1], out int _port))
                        port = _port;
                }
            }

            //// Parse arguments
            //foreach (var arg in args)
            //{
            //    if (arg.StartsWith("--folder="))
            //        folder = arg.Substring("--folder=".Length);
            //    else if (arg.StartsWith("--port=") && int.TryParse(arg.Substring("--port=".Length), out int parsedPort))
            //        port = parsedPort;
            //}
            Console.WriteLine("Hello Phone World!");

            // Get local IP address
            string localIP = GetLocalIPAddress();

            Console.WriteLine($"App is running at IP: {localIP}, Port: {port}");
            Console.WriteLine($"Using folder: {folder}");

            var filepath = GetVideo.Download(folder,port);

            Console.WriteLine($"File received: {filepath} in folder {folder}");
        }

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "No network adapters with an IPv4 address in the system!";
        }
    }
}

