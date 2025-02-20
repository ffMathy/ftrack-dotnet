namespace FtrackDotNet.Api.Requests.Operations;

public class FtrackQueryOperation : FtrackOperation
{
    public override string Action => "query";
    public required string Expression { get; init; }
}