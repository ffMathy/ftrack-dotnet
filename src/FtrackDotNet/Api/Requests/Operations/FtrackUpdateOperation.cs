namespace FtrackDotNet.Api.Requests.Operations;

public class FtrackUpdateOperation : FtrackOperation
{
    public override string Action => "update";
    public required string EntityType { get; init; }
    public required object EntityKey { get; init; }
    public required object EntityData { get; init; }
}