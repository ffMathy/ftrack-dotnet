using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FtrackDotNet.Models;
using Type = System.Type;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackFromExpressionVisitor : ExpressionVisitor
{
    private Type? _type;

    public Type Type => _type ?? throw new InvalidOperationException("Type not found");
    
    public string FromExpression => $" from {Type.Name}";

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        return base.Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (TryParseEntityTypeFromConstantExpression(node, out var genericType))
        {
            _type = genericType;
        }
        
        return base.VisitConstant(node);
    }

    internal static bool TryParseEntityTypeFromConstantExpression(ConstantExpression node, [NotNullWhen(true)] out Type? entityType)
    {
        var type = node.Value?.GetType();
        if (type is
            {
                IsGenericType: true,
                GenericTypeArguments: [var genericTypeResult]
            } &&
            (type.GetGenericTypeDefinition() == typeof(FtrackQueryable<>) ||
             type.GetGenericTypeDefinition() == typeof(FtrackDataSet<>)))
        {
            entityType = genericTypeResult;
            return true;
        }

        entityType = null;
        return false;
    }
}