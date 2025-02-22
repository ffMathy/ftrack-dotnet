using System.Text.Json;
using Sprache;

namespace FtrackDotNet.EventHub;

internal static class FtrackEventHubExpressionGrammar
{
    private static readonly Parser<string> Identifier =
        from first in Parse.Letter.Or(Parse.Char('_'))
        from rest in Parse.LetterOrDigit.Or(Parse.Char('_')).Many().Text()
        select first + rest;

    private static readonly Parser<string> Key =
        Identifier
            .DelimitedBy(Parse.Char('.'))
            .Select(parts => string.Join(".", parts));

    private static readonly Parser<string> Value =
        Parse.CharExcept(" \t\n\r")
            .AtLeastOnce()
            .Text();

    private static readonly Parser<IExpression> ClauseParser =
        from key in Key.Token()
        from eq in Parse.Char('=').Token()
        from value in Value.Token()
        select new Clause { Key = key, Value = value };

    private static readonly Parser<IExpression> ParenthesizedExpression =
        from lparen in Parse.Char('(').Token()
        from expr in Parse.Ref(() => Expression)
        from rparen in Parse.Char(')').Token()
        select expr;

    private static readonly Parser<IExpression> Factor =
        ParenthesizedExpression.Or(Expression);

    private static readonly Parser<Func<IExpression, IExpression, IExpression>> AndOperator =
        Parse.IgnoreCase("and").Token().Return((IExpression left, IExpression right) => new AndExpression(left, right));

    private static readonly Parser<Func<IExpression, IExpression, IExpression>> OrOperator =
        Parse.IgnoreCase("or").Token().Return((IExpression left, IExpression right) => new OrExpression(left, right));

    public static readonly Parser<IExpression> Expression =
        Parse.ChainOperator(OrOperator.Or(AndOperator), Factor, (op, left, right) => op(left, right));


    internal interface IExpression
    {
        bool Evaluate(JsonElement jsonElement);
    }

    internal class Clause : IExpression
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public bool Evaluate(JsonElement jsonElement)
        {
            var parts = Key.Split('.');
            JsonElement currentElement = jsonElement;
            foreach (var part in parts)
            {
                if (!currentElement.TryGetProperty(part, out currentElement))
                {
                    return false;
                }
            }

            var jsonValue = currentElement.ValueKind switch
            {
                JsonValueKind.String => currentElement.GetString()!,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => currentElement.GetRawText()
            };

            if (Value.EndsWith("*"))
            {
                var prefix = Value.Substring(0, Value.Length - 1);
                return jsonValue.StartsWith(prefix, StringComparison.Ordinal);
            }
            else
            {
                return jsonValue == Value;
            }
        }

        public override string ToString() => $"{Key} = {Value}";
    }

    private class AndExpression : IExpression
    {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public AndExpression(IExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public bool Evaluate(JsonElement jsonElement) =>
            Left.Evaluate(jsonElement) && Right.Evaluate(jsonElement);

        public override string ToString() => $"({Left} AND {Right})";
    }

    private class OrExpression : IExpression
    {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public OrExpression(IExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public bool Evaluate(JsonElement jsonElement) =>
            Left.Evaluate(jsonElement) || Right.Evaluate(jsonElement);

        public override string ToString() => $"({Left} OR {Right})";
    }
}