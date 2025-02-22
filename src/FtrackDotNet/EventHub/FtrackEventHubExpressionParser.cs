using System.Text.Json;
using Sprache;

namespace FtrackDotNet.EventHub;

internal static class FtrackEventHubExpressionGrammar
{
        // Parser for an identifier (letters, digits, underscore).
        private static readonly Parser<string> Identifier =
            from first in Parse.Letter.Or(Parse.Char('_'))
            from rest in Parse.LetterOrDigit.Or(Parse.Char('_')).Many().Text()
            select first + rest;

        // Parser for a dotted key (e.g., foo.bar.baz).
        private static readonly Parser<string> Key =
            Identifier.DelimitedBy(Parse.Char('.')).Select(parts => string.Join(".", parts));

        // Parser for a value. This basic version accepts any characters except whitespace or parentheses.
        private static readonly Parser<string> Value =
            Parse.CharExcept(" \t\n\r()").AtLeastOnce().Text();

        // Parser for a single clause: key=value.
        private static readonly Parser<IExpression> ClauseParser =
            from key in Key.Token()
            from eq in Parse.Char('=').Token()
            from value in Value.Token()
            select (IExpression)new Clause { Key = key, Value = value };

        // Parser for parenthesized expressions. It recursively refers to the overall expression parser.
        private static readonly Parser<IExpression> Parenthesized =
            from lparen in Parse.Char('(').Token()
            from expr in Parse.Ref(() => Expression)
            from rparen in Parse.Char(')').Token()
            select expr;

        // A factor is either a clause or a parenthesized expression.
        private static readonly Parser<IExpression> Factor =
            Parenthesized.Or(ClauseParser);

        // Parser for the "and" operator.
        private static readonly Parser<Func<IExpression, IExpression, IExpression>> AndOperator =
            Parse.IgnoreCase("and").Token()
                .Return((IExpression left, IExpression right) => new AndExpression(left, right));

        // Parser for the "or" operator.
        private static readonly Parser<Func<IExpression, IExpression, IExpression>> OrOperator =
            Parse.IgnoreCase("or").Token()
                .Return((IExpression left, IExpression right) => new OrExpression(left, right));

        // Term: chain together factors with "and" (which has higher precedence).
        private static readonly Parser<IExpression> Term =
            Parse.ChainOperator(AndOperator, Factor, (op, left, right) => op(left, right));

        // The full expression: chain together terms with "or".
        public static readonly Parser<IExpression> Expression =
            Parse.ChainOperator(OrOperator, Term, (op, left, right) => op(left, right));


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