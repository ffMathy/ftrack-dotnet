using System.Diagnostics;
using FtrackDotNet.Models;
using Type = System.Type;

namespace FtrackDotNet.UnitOfWork;

internal class ChangeTracker : IChangeTracker
{
    private readonly Dictionary<int, TrackedEntity> _trackedEntities = new();

    public void TrackEntity(object entity, TrackedEntityOperationType operationType)
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

        var relationalProperties = type
            .GetProperties()
            .Where(x => x.PropertyType.GetInterfaces().Any(x => x == typeof(IFtrackEntity)))
            .ToArray();
        foreach (var property in relationalProperties)
        {
            var value = property.GetValue(entity) as IFtrackEntity;
            if (value == null)
            {
                continue;
            }

            TrackEntity(value, operationType);
        }
        
        _trackedEntities.Add(id, new TrackedEntity()
        {
            EntityReference = new WeakReference(entity),
            ValueSnapshot = valueSnapshot,
            Operation = operationType
        });
    }

    private object TakeValueSnapshot(object entity)
    {
        var type = entity.GetType();
        
        var valueSnapshot = Activator.CreateInstance(type);
        if (valueSnapshot == null)
        {
            throw new InvalidOperationException("Could not create value snapshot of type: " + type);
        }
        
        var valueTypeProperties = type
            .GetProperties()
            .Where(x => IsSimple(x.PropertyType))
            .ToArray();
        foreach (var property in valueTypeProperties)
        {
            var value = property.GetValue(entity);
            property.SetValue(valueSnapshot, value);
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

    public void RefreshSnapshots()
    {
        var keysToRemove = new HashSet<int>();
        foreach (var keyValuePair in _trackedEntities)
        {
            if (keyValuePair.Value.EntityReference.Target is not IFtrackEntity entity)
            {
                keysToRemove.Add(keyValuePair.Key);
                continue;
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

public struct Change
{
    public object Entity { get; set; }
    public TrackedEntityOperationType Operation { get; set; }
}