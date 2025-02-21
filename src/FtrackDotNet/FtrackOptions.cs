namespace FtrackDotNet;

public class FtrackOptions
{
    public string ServerUrl { get; set; }
    public string ApiKey { get; set; }
    public string ApiUser { get; set; }
    public TimeSpan? RequestTimeout { get; set; }
}