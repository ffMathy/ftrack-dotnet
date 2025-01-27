namespace FtrackDotNet.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        // Special case for string, which implements IEnumerable<char>, but typically 
        // we don't treat string as a sequence in this context.
        if (sequenceType == null || sequenceType == typeof(string))
            return null;

        // If seqType is an array, return IEnumerable<T> for its element type
        if (sequenceType.IsArray)
        {
            return typeof(IEnumerable<>).MakeGenericType(
                sequenceType.GetElementType() ?? throw new InvalidOperationException());
        }

        // If seqType is a generic type, check if it implements IEnumerable<T>
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

        // Check all the interfaces implemented by seqType
        var interfaceTypes = sequenceType.GetInterfaces();
        foreach (var interfaceType in interfaceTypes)
        {
            var enumerableType = FindIEnumerable(interfaceType);
            if (enumerableType != null)
                return enumerableType;
        }

        // Finally, check the base type (recursively), unless it's object
        var baseType = sequenceType.BaseType;
        if (baseType != null && baseType != typeof(object))
        {
            return FindIEnumerable(baseType);
        }

        return null;
    }
}