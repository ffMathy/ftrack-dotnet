namespace FtrackDotNet.Models;

public struct FtrackPrimaryKey {
    public string Name { get; init; }
    public object Value { get; init; }
}
    
public interface IFtrackEntity {
    public string EntityType { get; }
    public FtrackPrimaryKey[] GetPrimaryKeys();
}