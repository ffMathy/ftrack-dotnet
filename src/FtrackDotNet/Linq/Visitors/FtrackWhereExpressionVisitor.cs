using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using FtrackDotNet.Api;
using FtrackDotNet.Extensions;

namespace FtrackDotNet.Linq.Visitors
{
    internal class FtrackWhereExpressionVisitor : ExpressionVisitor
    {
        private string _whereExpression = string.Empty;
        public string WhereExpression => _whereExpression;

        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            // When visiting the root lambda, pass its parameter for evaluation.
            string whereClause = node is LambdaExpression lambda
                ? ParsePredicate(lambda.Body, lambda.Parameters.First())
                : ParsePredicate(node, null);

            if (string.IsNullOrEmpty(_whereExpression))
            {
                _whereExpression = $" where {whereClause}";
            }

            return base.Visit(node);
        }

        // Overload that carries the lambda's parameter (if available)
        private string ParsePredicate(Expression? expression, ParameterExpression? rootParameter)
        {
            if (expression == null)
                return string.Empty;
            
            var evaluationResult = EvaluateExpression(expression);
            if (evaluationResult != null)
            {
                return ConvertToStringQueryValue(evaluationResult);
            }

            if (expression is LambdaExpression lambda)
            {
                return ParsePredicate(lambda.Body, lambda.Parameters.First());
            }

            if (expression is BinaryExpression binary)
            {
                var leftPredicateString = ParsePredicate(binary.Left, rootParameter);

                var rightEvaluation = EvaluateExpression(binary.Right);
                var rightValue = rightEvaluation != null ? 
                    ConvertToStringQueryValue(rightEvaluation) :
                    ParsePredicate(binary.Right, rootParameter);

                var operatorString = ParseOperator(binary.NodeType);
                return $"({leftPredicateString} {operatorString} {rightValue})";
            }

            if (expression is MemberExpression member)
            {
                var predicateString = ParsePredicate(member.Expression, rootParameter);
                return $"{(string.IsNullOrEmpty(predicateString) ? string.Empty : $"{predicateString}.")}{member.Member.Name.FromCamelOrPascalCaseToSnakeCase()}";
            }

            if (expression is ConstantExpression constantExpression)
            {
                return ConvertToStringQueryValue(constantExpression.Value?.ToString());
            }

            if (expression is ParameterExpression)
            {
                // The lambda parameter itself translates to nothing (its use appears in MemberExpressions).
                return string.Empty;
            }

            if (expression is MethodCallExpression methodCallExpression)
            {
                if (methodCallExpression.Method.DeclaringType == typeof(string))
                {
                    var objectMemberExpression = methodCallExpression.Object as MemberExpression;
                    var methodCallArgument = (ConstantExpression)methodCallExpression.Arguments[0];

                    string valueString;
                    string operatorString;
                    switch (methodCallExpression.Method.Name)
                    {
                        case nameof(string.StartsWith):
                            valueString = $"%{methodCallArgument.Value}";
                            operatorString = "like";
                            break;
                        case nameof(string.EndsWith):
                            valueString = $"{methodCallArgument.Value}%";
                            operatorString = "like";
                            break;
                        case nameof(string.Contains):
                            valueString = $"%{methodCallArgument.Value}%";
                            operatorString = "like";
                            break;
                        case nameof(string.Equals):
                            valueString = methodCallArgument.Value?.ToString() ?? "null";
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
                    string operatorPrefixString = methodCallExpression.Method.Name switch
                    {
                        nameof(Enumerable.All) => "not ",
                        nameof(Enumerable.Any) => string.Empty,
                        _ => throw new InvalidOperationException("Could not translate method call to Ftrack query: " +
                                                                  methodCallExpression)
                    };

                    var memberArg = (MemberExpression)methodCallExpression.Arguments[0];
                    var lambdaArg = (LambdaExpression)methodCallExpression.Arguments[1];

                    var propertyNameString = ParsePredicate(memberArg, rootParameter);
                    var valueString = ParsePredicate(lambdaArg.Body, rootParameter);
                    return $@"{operatorPrefixString}{propertyNameString} any ({valueString})";
                }
            }

            throw new InvalidOperationException(
                $"Could not parse expression as predicate: {expression} ({expression?.GetType().FullName ?? "<null>"})");
        }

        private static string ConvertToStringQueryValue(object? rightValue)
        {
            return rightValue switch
            {
                string => $"\"{rightValue}\"",
                DateTimeOffset dateTimeOffset => ConvertToStringQueryValue(FtrackDateJsonConverter.ConvertDateTimeOffsetToString(dateTimeOffset)),
                null => "null",
                _ => rightValue?.ToString() ?? "null"
            };
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

        // Recursively evaluates an expression to obtain its constant value.
        private object? EvaluateExpression(Expression? expression)
        {
            if (expression == null)
            {
                return null;
            }
            
            if (expression is ConstantExpression constant)
            {
                return constant.Value;
            }
            
            if (expression is MemberExpression member)
            {
                var container = EvaluateExpression(member.Expression);
                if (container == null)
                {
                    return null;
                }
                
                if (member.Member is FieldInfo field)
                {
                    return field.GetValue(container);
                }
                if (member.Member is PropertyInfo prop)
                {
                    return prop.GetValue(container);
                }
                
                throw new InvalidOperationException("Unsupported member type.");
            }

            try
            {
                return Expression.Lambda(expression).Compile().DynamicInvoke();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
