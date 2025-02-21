namespace FtrackDotNet.Extensions;

internal static class TypeExtensions
{
    public static bool IsSimple(this Type type)
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
}