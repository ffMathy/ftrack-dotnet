namespace FtrackDotNet.Models;

public class ObjectType
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsTimeReportable { get; set; }
    public bool IsTaskable { get; set; }
    public bool IsTypeable { get; set; }
    public bool IsStatusable { get; set; }
    public bool IsSchedulable { get; set; }
    public bool IsPrioritizable { get; set; }
    public bool IsLeaf { get; set; }
}