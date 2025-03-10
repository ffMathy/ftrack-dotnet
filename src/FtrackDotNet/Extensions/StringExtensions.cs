using System.Text.RegularExpressions;

namespace FtrackDotNet.Extensions;

internal static partial class StringExtensions
{
    public static string FromCamelOrPascalCaseToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var pattern = GetCamelCaseRegex();
        var snake = pattern.Replace(input, "$1_$2").ToLower();
        return snake;
    }
    
    public static string FromSnakeCaseToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var parts = input.Split('_', StringSplitOptions.RemoveEmptyEntries);

        var pascalParts = parts.Select(part => part switch
        {
            { Length: > 0 } => char.ToUpperInvariant(part[0]) + part[1..],
            _ => part
        });

        return string.Concat(pascalParts);
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex GetCamelCaseRegex();
}