using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FtrackDotNet.Api;
using FtrackDotNet.Api.Requests.Operations;
using FtrackDotNet.Extensions;
using FtrackDotNet.Models;

namespace FtrackDotNet.UnitOfWork;

internal delegate FtrackPrimaryKey[] GetPrimaryKeysForEntityDelegate(JsonElement? entity);

public partial class FtrackContext(
    IFtrackClient ftrackClient,
    IChangeTracker changeTracker)
{
    private static readonly Dictionary<string, GetPrimaryKeysForEntityDelegate> PrimaryKeyAccessorsByEntityTypeName =
        new Dictionary<string, GetPrimaryKeysForEntityDelegate>();
    
    internal static JsonSerializerOptions GetJsonSerializerOptions(JsonIgnoreCondition jsonIgnoreCondition = JsonIgnoreCondition.Never) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = jsonIgnoreCondition,
        Converters =
        {
            new FtrackDateJsonConverter()
        }
    };
    
    internal static string GetTypeNameFromJsonElement(JsonElement entity)
    {
        return entity
            .GetProperty(nameof(IFtrackEntity.__entity_type__))
            .GetString()!;
    }
    
    internal static FtrackPrimaryKey[] GetPrimaryKeysForEntity(JsonElement entity)
    {
        var ftrackEntityTypeName = GetTypeNameFromJsonElement(entity);
        if (!PrimaryKeyAccessorsByEntityTypeName.TryGetValue(ftrackEntityTypeName, out var primaryKeyAccessor))
        {
            return [];
        }

        return primaryKeyAccessor(entity);
    }
    
    internal static FtrackPrimaryKey[] GetPrimaryKeysForEntity(string ftrackEntityTypeName)
    {
        if (!PrimaryKeyAccessorsByEntityTypeName.TryGetValue(ftrackEntityTypeName, out var primaryKeyAccessor))
        {
            return [];
        }

        return primaryKeyAccessor(null);
    }
    
    internal static void RegisterFtrackType(System.Type type)
    {
        var getPrimaryKeysMethod = type
            .GetMethods()
            .Single(x => x is { IsStatic: true, Name: nameof(TypedContext.GetPrimaryKeys) });
        
        PrimaryKeyAccessorsByEntityTypeName.TryAdd(type.Name, entity => (FtrackPrimaryKey[])(
            getPrimaryKeysMethod.Invoke(null, [entity]) ?? 
            throw new InvalidOperationException("Primary keys not found.")));
    }
    
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = changeTracker.GetChanges();
        var operations = changes
            .Select(x => x.Operation switch
            {
                TrackedEntityOperationType.Create => (FtrackOperation)new FtrackCreateOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityData = x.Differences
                        .ToFrozenDictionary(
                            v => v.Key.FromCamelOrPascalCaseToSnakeCase(), 
                            v => v.Value)
                },
                TrackedEntityOperationType.Update => (FtrackOperation)new FtrackUpdateOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityKey = x.Entity.PrimaryKeys
                        .Select(primaryKey => primaryKey.Value)
                        .ToArray(),
                    EntityData = x.Differences
                        .ToFrozenDictionary(
                            v => v.Key.FromCamelOrPascalCaseToSnakeCase(), 
                            v => v.Value)
                },
                TrackedEntityOperationType.Delete => (FtrackOperation)new FtrackDeleteOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityKey = x.Entity.PrimaryKeys
                        .Select(primaryKey => primaryKey.Value)
                        .ToArray(),
                },
                _ => throw new InvalidOperationException("Unknown operation: " + x.Operation)
            })
            .ToImmutableArray();
        
        var responses = await ftrackClient.CallAsync(operations, cancellationToken);
        UpdateLocalEntityDataFromComputedValues(responses, changes);

        changeTracker.OnSaved();
    }

    private static void UpdateLocalEntityDataFromComputedValues(JsonElement[] responses, Change[] changes)
    {
        for (var i = 0; i < responses.Length; i++)
        {
            var change = changes[i];
            var response = responses[i];

            if (response.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            change.Entity.PrimaryKeys = GetPrimaryKeysForEntity(response);

            var target = change.Entity.Reference.Target!;
            var targetPropertySetters = target.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.SetMethod != null);
            foreach (var targetPropertySetter in targetPropertySetters)
            {
                if(!response.TryGetProperty(targetPropertySetter.Name.FromCamelOrPascalCaseToSnakeCase(), out var responseProperty))
                {
                    continue;
                }

                var deserializedValue = responseProperty.Deserialize(targetPropertySetter.PropertyType, GetJsonSerializerOptions());
                targetPropertySetter.SetValue(target, deserializedValue);
            }
        }
    }
}
