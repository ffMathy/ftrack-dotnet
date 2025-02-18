using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FtrackDotNet.Api;
using FtrackDotNet.Models;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet.UnitOfWork;

internal delegate FtrackPrimaryKey[]? GetPrimaryKeysForEntityDelegate(dynamic? entity);

public partial class FtrackContext(
    IFtrackClient ftrackClient,
    IChangeTracker changeTracker)
{
    private static readonly Dictionary<string, GetPrimaryKeysForEntityDelegate> PrimaryKeyAccessorsByEntityTypeName =
        new Dictionary<string, GetPrimaryKeysForEntityDelegate>();
    
    internal static JsonSerializerOptions GetJsonSerializerOptions(JsonIgnoreCondition jsonIgnoreCondition = JsonIgnoreCondition.Never) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = jsonIgnoreCondition
    };
    
    internal static FtrackPrimaryKey[] GetPrimaryKeysForEntity(string ftrackEntityTypeName, dynamic? entity)
    {
        if (!PrimaryKeyAccessorsByEntityTypeName.TryGetValue(ftrackEntityTypeName, out var primaryKeyAccessor))
        {
            return [];
        }

        return primaryKeyAccessor(entity);
    }
    
    internal static void RegisterFtrackType(System.Type type)
    {
        var getPrimaryKeysMethod = type
            .GetMethods()
            .Single(x => x is { IsStatic: true, Name: nameof(IFtrackEntity.GetPrimaryKeys) });
        
        PrimaryKeyAccessorsByEntityTypeName.TryAdd(type.Name, entity => (FtrackPrimaryKey[])(
            getPrimaryKeysMethod.Invoke(null, [entity]) ?? 
            throw new InvalidOperationException("Primary keys not found.")));
    }
    
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var operations = changeTracker
            .GetChanges()
            .Select(x => x.Operation switch
            {
                TrackedEntityOperationType.Create => (FtrackOperation)new FtrackCreateOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityData = x.Entity.Reference.Target!
                },
                TrackedEntityOperationType.Update => (FtrackOperation)new FtrackUpdateOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityKey = x.Entity.PrimaryKeys,
                    EntityData = x.Entity.Reference.Target!
                },
                TrackedEntityOperationType.Delete => (FtrackOperation)new FtrackDeleteOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityKey = x.Entity.PrimaryKeys,
                },
                _ => throw new InvalidOperationException("Unknown operation: " + x.Operation)
            });
        
        var responses = await ftrackClient.CallAsync(operations, cancellationToken);

        changeTracker.OnSaved();
    }
}
