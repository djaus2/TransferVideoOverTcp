namespace GetVideoService;

public class ServiceSettings
{
    public string Folder { get; set; } = @"C:\temp\vid";
    public int Port { get; set; } = 5000;
    public int PollingIntervalSeconds { get; set; } = 30;
    public bool AutoStart { get; set; } = true;
}
