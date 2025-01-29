

namespace FtrackDotNet.UnitOfWork;

internal class ChangeDetector : IChangeDetector
{
    private readonly Dictionary<int, TrackedEntity> _trackedEntities = new();
    
    public void TrackEntity(object entity)
    {
        var id = entity.GetHashCode();
        if (_trackedEntities.ContainsKey(id))
        {
            return;
        }

        var type = entity.GetType();
        
        var valueSnapshot = TakeValueSnapshot(entity);

        var referenceTypeProperties = type
            .GetProperties()
            .Where(x => !IsSimple(x.PropertyType))
            .ToArray();
        foreach (var property in referenceTypeProperties)
        {
            var value = property.GetValue(entity);
            if (value == null)
            {
                continue;
            }

            TrackEntity(value);
        }
        
        _trackedEntities.Add(id, new TrackedEntity()
        {
            EntityReference = new WeakReference(entity),
            ValueSnapshot = valueSnapshot
        });
    }

    private object TakeValueSnapshot(object entity)
    {
        var type = entity.GetType();
        
        var valueSnapshot = Activator.CreateInstance(type);
        if (valueSnapshot == null)
        {
            throw new InvalidOperationException("Could not create value snapshot.");
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

    private bool IsSimple(System.Type type)
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
            var entity = keyValuePair.Value.EntityReference.Target;
            if (entity == null)
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