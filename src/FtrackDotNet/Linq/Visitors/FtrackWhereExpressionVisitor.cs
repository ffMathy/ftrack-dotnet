using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

public class FtrackWhereExpressionVisitor : ExpressionVisitor
{
    private string _whereExpression = string.Empty;
    
    public string WhereExpression => _whereExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        var whereClause = ParsePredicate(node);
        if (string.IsNullOrEmpty(_whereExpression))
        {
            _whereExpression = whereClause;
        }
        else
        {
            _whereExpression += $" and {whereClause}";
        }
        
        return base.Visit(node);
    }

    private string ParsePredicate(Expression? expression)
    {
        // Very naive example that handles simple (x => x.Field == "value") style
        if (expression is BinaryExpression binary)
        {
            var left = ParsePredicate(binary.Left);
            var right = ParsePredicate(binary.Right);
            var op = ParseOperator(binary.NodeType);

            return $"{left} {op} {right}";
        }

        if (expression is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name.ToLowerInvariant(); // or some mapping
        }

        if (expression is ConstantExpression constantExpression)
        {
            // For strings, wrap in quotes
            if (constantExpression.Type == typeof(string))
            {
                return $"\"{constantExpression.Value}\"";
            }

            return constantExpression.Value?.ToString() ?? "null";
        }

        if (expression is MethodCallExpression methodCallExpression && methodCallExpression.Method.DeclaringType == typeof(string))
        {
            switch (methodCallExpression.Method.Name)
            {
                case nameof(string.StartsWith):
                    throw new NotImplementedException();
                
                case nameof(string.EndsWith):
                    throw new NotImplementedException();
                
                case nameof(string.Contains):
                    throw new NotImplementedException();
                
                case nameof(string.Equals):
                    throw new NotImplementedException();
            }
        }

        // Additional logic for method calls (Contains, etc.) or other expression types
        throw new InvalidOperationException(
            $"Could not parse expression as predicate: {expression} ({expression.GetType().FullName})");
    }

    private string ParseOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "and",
            ExpressionType.OrElse => "or",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported.")
        };
    }
}