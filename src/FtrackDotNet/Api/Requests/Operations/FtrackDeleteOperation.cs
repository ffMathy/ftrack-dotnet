namespace FtrackDotNet.Api.Requests.Operations;

public class FtrackDeleteOperation : FtrackOperation
{
    public override string Action => "delete";
    public required string EntityType { get; init; }
    public required object EntityKey { get; init; }
}