namespace FtrackDotNet.Api;
public class FtrackServerErrorResponse
{
    public string Exception { get; set; }
    public string Content { get; set; }
}

public class FtrackServerException : Exception
{
    public FtrackServerException(FtrackServerErrorResponse response) : base(
        $"The Ftrack server returned an error: [{response.Exception}]: {response.Content}")
    {
        Response = response.Content;
        Category = response.Exception;
    }
    
    public string Category { get; }

    public string Response { get; }
}