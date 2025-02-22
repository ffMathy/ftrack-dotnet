using FtrackDotNet.Extensions;
using FtrackDotNet.UnitOfWork;
using Sprache;

internal class Clause(
    string pascalCaseKey,
    string value)
{
    public string PascalCaseKey { get; set; } = pascalCaseKey;

    public string SnakeCaseKey { get; set; } = pascalCaseKey.FromCamelOrPascalCaseToSnakeCase();

    public string Value { get; set; } = value;
    public override string ToString() => $"{SnakeCaseKey}={Value}";
}

internal static class FtrackEventHubExpressionParser
{
    // An identifier is a sequence of letters, digits or underscores.
    private static readonly Parser<string> Identifier =
        from first in Parse.Letter.Or(Parse.Char('_'))
        from rest in Parse.LetterOrDigit.Or(Parse.Char('_')).Many().Text()
        select first + rest;

    // A key is one or more identifiers separated by a dot.
    private static readonly Parser<string> Key =
        from parts in Identifier.DelimitedBy(Parse.Char('.'))
        select string.Join(".", parts);

    // A value is a sequence of characters until whitespace or end-of-input.
    // This example is basic and assumes values don’t contain spaces.
    private static readonly Parser<string> Value =
        Parse.CharExcept(" \t\n\r").AtLeastOnce().Text();

    // A clause is a key, an equals sign, and a value.
    private static readonly Parser<Clause> ClauseParser =
        from key in Key.Token()
        from eq in Parse.Char('=').Token()
        from value in Value.Token()
        select new Clause(key, value);

    // The expression is one or more clauses joined by the keyword "and".
    public static readonly Parser<IEnumerable<Clause>> Expression =
        from first in ClauseParser
        from rest in (
            from _ in Parse.IgnoreCase("and").Token()
            from clause in ClauseParser
            select clause
        ).Many()
        select new[] { first }.Concat(rest);
}