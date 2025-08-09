using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using DownloadVideoOverTCPLib;

using Sportronics.ConfigurationManager;



using System.Security.Cryptography;
using Microsoft.Extensions.Configuration.Json;
using GetVideoApp;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;


namespace Transfer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting application...");
            AppSettings? appSettings = null;
            // Default settings (lowest priority)
            appSettings = new AppSettings
            {
                Folder = @"c:\Temp\Vid",
                Port = 5000
            };




            // Define command line options mapping
            var optionsMap = new Dictionary<string, (string LongName, string ShortName)>
            {
                { "Folder", ("folder", "f") },
                { "Port", ("port", "p") }
            };

            // Create configuration processor
            var configProcessor = new ConfigurationProcessor<AppSettings>(
              "appsettings.json",
              optionsMap,
              appSettings);



            appSettings = configProcessor.ProcessConfiguration(args);
            if(appSettings == null)
            {
                return;
            }

            string folder = appSettings.Folder;
            int port = appSettings.Port;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Console.WriteLine("Hello Phone World!");

            // Get local IP address
            string localIP = GetLocalIPAddress();

            Console.WriteLine($"App is running at IP: {localIP}, Port: {port}");
            Console.WriteLine($"Using folder: {folder}");

            var filepath = DownloadVideoOverTCPLib.GetVideo.Download(folder,port);

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

