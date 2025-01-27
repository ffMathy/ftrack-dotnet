using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackWhereExpressionVisitor : ExpressionVisitor
{
    private string _whereExpression = string.Empty;
    
    public string WhereExpression => _whereExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        var whereClause = ParsePredicate(node);
        if (string.IsNullOrEmpty(_whereExpression))
        {
            _whereExpression = $" where {whereClause}";
        }
        
        return base.Visit(node);
    }

    private string ParsePredicate(Expression? expression)
    {
        if (expression == null)
            return string.Empty;
        
        // Very naive example that handles simple (x => x.Field == "value") style
        if (expression is BinaryExpression binary)
        {
            var leftPredicateString = ParsePredicate(binary.Left);
            var rightPredicateString = ParsePredicate(binary.Right);
            var operatorString = ParseOperator(binary.NodeType);

            return $"({leftPredicateString} {operatorString} {rightPredicateString})";
        }

        if (expression is MemberExpression memberExpression)
        {
            var predicateString = ParsePredicate(memberExpression.Expression);
            return $"{(string.IsNullOrEmpty(predicateString) ? string.Empty : $"{predicateString}.")}{memberExpression.Member.Name.ToLowerInvariant()}"; // or some mapping
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
        
        if(expression is LambdaExpression lambdaExpression)
        {
            return ParsePredicate(lambdaExpression.Body);
        }
        
        if(expression is ParameterExpression parameterExpression)
        {
            return string.Empty;
        }

        if (expression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(string))
            {
                var objectMemberExpression = (MemberExpression?)methodCallExpression.Object;
                var methodCallExpressionArgument = (ConstantExpression)methodCallExpression.Arguments[0];

                string valueString;
                string operatorString;
                switch (methodCallExpression.Method.Name)
                {
                    case nameof(string.StartsWith):
                        valueString = $"%{methodCallExpressionArgument.Value}";
                        operatorString = "like";
                        break;

                    case nameof(string.EndsWith):
                        valueString = $"{methodCallExpressionArgument.Value}%";
                        operatorString = "like";
                        break;

                    case nameof(string.Contains):
                        valueString = $"%{methodCallExpressionArgument.Value}%";
                        operatorString = "like";
                        break;

                    case nameof(string.Equals):
                        valueString = methodCallExpressionArgument.Value?.ToString() ?? "null";
                        operatorString = "=";
                        break;

                    default:
                        throw new InvalidOperationException("Could not translate method call to Ftrack query: " +
                                                            methodCallExpression);
                }

                return $@"{objectMemberExpression?.Member.Name.ToLowerInvariant()} {operatorString} ""{valueString}""";
            }

            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var operatorPrefixString = string.Empty;
                switch (methodCallExpression.Method.Name)
                {
                    case nameof(Enumerable.All):
                        operatorPrefixString = "not ";
                        break;

                    case nameof(Enumerable.Any):
                        break;

                    default:
                        throw new InvalidOperationException("Could not translate method call to Ftrack query: " +
                                                            methodCallExpression);
                }
                
                var methodCallMemberExpressionArgument = (MemberExpression)methodCallExpression.Arguments[0];
                var methodCallLambdaExpressionArgument = (LambdaExpression)methodCallExpression.Arguments[1];

                var propertyNameString = $"{ParsePredicate(methodCallMemberExpressionArgument)}";
                var valueString = ParsePredicate(methodCallLambdaExpressionArgument.Body);
                return $@"{operatorPrefixString}{propertyNameString} any ({valueString})";
            }
        }

        // Additional logic for method calls (Contains, etc.) or other expression types
        throw new InvalidOperationException(
            $"Could not parse expression as predicate: {expression} ({expression?.GetType().FullName ?? "<null>"})");
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