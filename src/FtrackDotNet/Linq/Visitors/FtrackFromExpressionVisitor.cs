using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FtrackDotNet.Models;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackFromExpressionVisitor : ExpressionVisitor
{
    private string _fromExpression = string.Empty;
    
    public string FromExpression => _fromExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        return base.Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var type = node.Value?.GetType();
        if (type is
            {
                IsGenericType: true, 
                GenericTypeArguments: [{ Name: var genericTypeName }]
            } && 
            (type.GetGenericTypeDefinition() == typeof(FtrackQueryable<>) ||
            type.GetGenericTypeDefinition() == typeof(FtrackDataSet<>)))
        {
            _fromExpression = $" from {genericTypeName}";
        }
        
        return base.VisitConstant(node);
    }
}