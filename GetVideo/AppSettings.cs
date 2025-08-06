using Sportronics.ConfigurationManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetVideoApp;



using Sportronics.ConfigurationManager;

public class AppSettings : AppSettingsBase
{
    public string Folder { get; set; } = @"C:\temp\vid";
    public int Port { get; set; } = 5000;

    public override string SectionName => "AppSettings";

    public override string ToString()
    {
        return $"Folder: {Folder}, Port: {Port}";
    }
}
