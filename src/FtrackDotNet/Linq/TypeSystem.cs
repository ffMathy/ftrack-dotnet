namespace FtrackDotNet.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

internal static class TypeSystem
{
    /// <summary>
    /// Returns the element type T for IEnumerable&lt;T&gt; or IQueryable&lt;T&gt;.
    /// If the type implements the interface in some other hierarchy level,
    /// this method attempts to walk up and find it.
    /// If no generic interface is found, it returns the type itself.
    /// </summary>
    public static Type GetElementType(Type type)
    {
        var enumerableType = FindIEnumerable(type);
        if (enumerableType == null) 
            return type;
        
        return enumerableType.GetGenericArguments()[0];
    }

    public static bool IsEnumerable(Type type)
    {
        if (type == typeof(Array))
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }
        
        var genericTypeDefinition = type.GetGenericTypeDefinition();
        
        var interfaces = genericTypeDefinition.GetInterfaces();
        return interfaces.Any(i => 
            i.IsGenericType && 
            i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static Type? FindIEnumerable(Type? sequenceType)
    {
        if (sequenceType == null || sequenceType == typeof(string))
            return null;

        if (sequenceType.IsArray)
        {
            return typeof(IEnumerable<>).MakeGenericType(
                sequenceType.GetElementType() ?? throw new InvalidOperationException());
        }

        if (sequenceType.IsGenericType)
        {
            foreach (var genericArgumentType in sequenceType.GetGenericArguments())
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(genericArgumentType);
                if (enumerableType.IsAssignableFrom(sequenceType))
                {
                    return enumerableType;
                }
            }
        }

        var interfaceTypes = sequenceType.GetInterfaces();
        foreach (var interfaceType in interfaceTypes)
        {
            var enumerableType = FindIEnumerable(interfaceType);
            if (enumerableType != null)
                return enumerableType;
        }

        var baseType = sequenceType.BaseType;
        if (baseType != null && baseType != typeof(object))
        {
            return FindIEnumerable(baseType);
        }

        return null;
    }
}