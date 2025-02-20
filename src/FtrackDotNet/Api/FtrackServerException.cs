namespace FtrackDotNet.Api.Models;
public class FtrackServerErrorResponse
{
    public string Exception { get; set; }
    public string Content { get; set; }
}

public class FtrackServerException : Exception
{
    public FtrackServerException(
        string requestJson,
        FtrackServerErrorResponse response
    ) : base(
        $"The Ftrack server returned an error: [{response.Exception}]: {response.Content} for request: {requestJson}")
    {
        Response = response.Content;
        Category = response.Exception;
    }
    
    public string Category { get; }

    public string Response { get; }
}