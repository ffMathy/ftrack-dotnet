using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using FtrackDotNet.Extensions;
using FtrackDotNet.Models;
using Type = System.Type;

namespace FtrackDotNet.UnitOfWork;

internal class ChangeTracker : IChangeTracker
{
    private readonly Dictionary<int, TrackedEntity> _trackedEntities = new();

    public void TrackEntity(JsonElement jsonElement, object entity, TrackedEntityOperationType operationType)
    {
        if (entity == null)
        {
            throw new InvalidOperationException("Entity cannot be null.");
        }
        
        var id = entity.GetHashCode();
        if (_trackedEntities.TryGetValue(id, out var trackedEntity))
        {
            trackedEntity.Operation = operationType;
            return;
        }

        Debug.WriteLine("Tracking entity: " + id);

        var type = entity.GetType();
        
        var valueSnapshot = TakeValueSnapshot(entity);

        var relationalProperties = GetRelationalProperties(type);
        foreach (var property in relationalProperties)
        {
            var value = property.GetValue(entity) as IFtrackEntity;
            if (value == null)
            {
                continue;
            }
            
            TrackEntity(JsonSerializer.SerializeToElement(value), value, operationType);
        }

        var primaryKeys = FtrackContext.GetPrimaryKeysForEntity(jsonElement);
        if (primaryKeys.Length == 0)
        {
            return;
        }
        
        _trackedEntities.Add(id, new TrackedEntity()
        {
            Entity = new EntityReference() {
                Reference = new WeakReference(entity),
                Type = FtrackContext.GetTypeNameFromJsonElement(jsonElement),
                PrimaryKeys = primaryKeys
            },
            ValueSnapshot = valueSnapshot,
            Operation = operationType
        });
    }

    private static PropertyInfo[] GetSimpleProperties(Type type)
    {
        return type
            .GetProperties()
            .Where(x => x.PropertyType
                .GetInterfaces()
                .All(i => i != typeof(IFtrackEntity)))
            .ToArray();
    }

    private static PropertyInfo[] GetRelationalProperties(Type type)
    {
        return type
            .GetProperties()
            .Where(x => x.PropertyType
                .GetInterfaces()
                .Any(i => i == typeof(IFtrackEntity)))
            .ToArray();
    }

    private Dictionary<string, object?> TakeValueSnapshot(object entity)
    {
        var type = entity.GetType();
        
        var valueSnapshot = new Dictionary<string, object?>();
        
        var valueTypeProperties = type
            .GetProperties()
            .Where(x => x.PropertyType.IsSimple())
            .ToArray();
        foreach (var property in valueTypeProperties)
        {
            var value = property.GetValue(entity);
            valueSnapshot.Add(property.Name, value);
        }

        return valueSnapshot;
    }

    public Change[] GetChanges()
    {
        return _trackedEntities
            .Select(x => new Change()
            {
                Operation = x.Value.Operation!.Value,
                Differences = x.Value.Operation!.Value switch
                {
                    TrackedEntityOperationType.Update or TrackedEntityOperationType.Delete => 
                        GetDifferencesForEntitySinceSnapshot(x.Value.Entity.Reference.Target!, x.Value.ValueSnapshot),
                    TrackedEntityOperationType.Create => 
                        GetValuesAsDictionary(x.Value.Entity.Reference.Target!)
                            .Where(v => v.Value != null)
                            .ToFrozenDictionary(v => v.Key, v => v.Value),
                },
                Entity = x.Value.Entity,
            })
            .ToArray();
    }

    private IReadOnlyDictionary<string,object?> GetDifferencesForEntitySinceSnapshot(
        object entityReferenceTarget, 
        IReadOnlyDictionary<string,object?> valueSnapshot)
    {
        return GetValuesAsDictionary(entityReferenceTarget)
            .Where(x => valueSnapshot.ContainsKey(x.Key))
            .Where(x => !Equals(x.Value, valueSnapshot[x.Key]))
            .ToFrozenDictionary(x => x.Key, x => x.Value);
    }

    private static IReadOnlyDictionary<string, object?> GetValuesAsDictionary(object entityReferenceTarget)
    {
        return GetSimpleProperties(entityReferenceTarget.GetType())
            .Select(x => new {
                Value = x.GetValue(entityReferenceTarget),
                PropertyName = x.Name
            })
            .ToFrozenDictionary(x => x.PropertyName, x => x.Value);
    }

    public void OnSaved()
    {
        var keysToRemove = new HashSet<int>();
        foreach (var keyValuePair in _trackedEntities)
        {
            if (keyValuePair.Value.Entity.Reference.Target is not { } entity)
            {
                keysToRemove.Add(keyValuePair.Key);
                continue;
            }

            if (keyValuePair.Value.Operation == TrackedEntityOperationType.Create)
            {
                keyValuePair.Value.Operation = TrackedEntityOperationType.Update;
            }
            
            var valueSnapshot = TakeValueSnapshot(entity);
            _trackedEntities[keyValuePair.Key].ValueSnapshot = valueSnapshot;
        }

        foreach (var key in keysToRemove)
        {
            _trackedEntities.Remove(key);
        }
    }
}

public record EntityReference
{
    public string Type { get; init; }
    public FtrackPrimaryKey[] PrimaryKeys { get; set; }
    public WeakReference Reference { get; init; }
}

public record Change
{
    public EntityReference Entity { get; init; }
    public TrackedEntityOperationType Operation { get; init; }
    public IReadOnlyDictionary<string, object?> Differences { get; init; }
}