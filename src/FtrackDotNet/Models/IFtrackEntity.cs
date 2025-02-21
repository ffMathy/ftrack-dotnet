namespace FtrackDotNet.Models;

public struct FtrackPrimaryKey {
    public string Name { get; init; }
    public object? Value { get; init; }
}
    
public interface IFtrackEntity {
    // ReSharper disable once InconsistentNaming
    public string __entity_type__ { get; }
}