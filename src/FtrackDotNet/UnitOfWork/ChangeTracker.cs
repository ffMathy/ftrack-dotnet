using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using FtrackDotNet.Models;
using Type = System.Type;

namespace FtrackDotNet.UnitOfWork;

internal class ChangeTracker : IChangeTracker
{
    private readonly Dictionary<int, TrackedEntity> _trackedEntities = new();

    public void TrackEntity(object entity, Type entityType, TrackedEntityOperationType operationType)
    {
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

            TrackEntity(value, value.GetType(), operationType);
        }
        
        _trackedEntities.Add(id, new TrackedEntity()
        {
            Entity = new EntityReference() {
                Reference = new WeakReference(entity),
                Type = entityType.Name,
            },
            ValueSnapshot = valueSnapshot,
            Operation = operationType
        });
    }

    private static PropertyInfo[] GetRelationalProperties(Type type)
    {
        return type
            .GetProperties()
            .Where(x => x.PropertyType
                .GetInterfaces()
                .Any(x => x == typeof(IFtrackEntity)))
            .ToArray();
    }

    private Dictionary<string, object?> TakeValueSnapshot(object entity)
    {
        var type = entity.GetType();
        
        var valueSnapshot = new Dictionary<string, object?>();
        
        var valueTypeProperties = type
            .GetProperties()
            .Where(x => IsSimple(x.PropertyType))
            .ToArray();
        foreach (var property in valueTypeProperties)
        {
            var value = property.GetValue(entity);
            valueSnapshot.Add(property.Name, value);
        }

        return valueSnapshot;
    }

    private static bool IsSimple(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return IsSimple(type.GetGenericArguments()[0]);
        }
        
        return type.IsPrimitive 
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal);
    }

    public Change[] GetChanges()
    {
        return _trackedEntities
            .Select(x => new Change()
            {
                Operation = x.Value.Operation!.Value,
                Differences = GetDifferencesForEntitySinceSnapshot(x.Value.Entity.Reference.Target!, x.Value.ValueSnapshot),
                Entity = x.Value.Entity
            })
            .ToArray();
    }

    private IDictionary<string,object?> GetDifferencesForEntitySinceSnapshot(
        object entityReferenceTarget, 
        IReadOnlyDictionary<string,object?> valueSnapshot)
    {
        return GetRelationalProperties(entityReferenceTarget.GetType())
            .Where(x => valueSnapshot.ContainsKey(x.Name))
            .Select(x => new {
                Value = x.GetValue(entityReferenceTarget),
                PropertyName = x.Name
            })
            .Where(x => !object.Equals(x.Value, valueSnapshot[x.PropertyName]))
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

public struct EntityReference
{
    public string Type { get; set; }
    public object Key { get; set; }
    public WeakReference Reference { get; set; }
}

public struct Change
{
    public EntityReference Entity { get; set; }
    public TrackedEntityOperationType Operation { get; set; }
    public IDictionary<string, object?> Differences { get; init; }
}