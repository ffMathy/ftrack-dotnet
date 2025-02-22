namespace FtrackDotNet;

public class FtrackOptions
{
    [Required]
    [Url]
    public string ServerUrl { get; set; }

    [Required]
    public string ApiKey { get; set; }

    [Required]
    public string ApiUser { get; set; }

    public TimeSpan? RequestTimeout { get; set; }
}
