using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

public class FtrackFromExpressionVisitor : ExpressionVisitor
{
    private string _fromExpression = string.Empty;
    
    public string FromExpression => _fromExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        if (node is not MethodCallExpression methodCallExpression)
        {
            throw new InvalidOperationException("From expression must be a method call expression.");
        }
        
        return base.Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var type = node.Value?.GetType();
        if (type is { IsGenericType: true, GenericTypeArguments: [{ Name: var genericTypeName }] } && type.GetGenericTypeDefinition() == typeof(FtrackQueryable<>))
        {
            _fromExpression = $" from {genericTypeName}";
        }
        
        return base.VisitConstant(node);
    }
}