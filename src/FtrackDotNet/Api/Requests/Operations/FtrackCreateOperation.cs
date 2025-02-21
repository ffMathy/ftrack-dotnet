namespace FtrackDotNet.Api.Requests.Operations;

public class FtrackCreateOperation : FtrackOperation
{
    public override string Action => "create";
    public required string EntityType { get; init; }
    public required object EntityData { get; init; }
}