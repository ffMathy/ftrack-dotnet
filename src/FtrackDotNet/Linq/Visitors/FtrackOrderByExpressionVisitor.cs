using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackOrderByExpressionVisitor : ExpressionVisitor
{
    private string _orderByExpression = string.Empty;
    
    public string OrderByExpression => string.IsNullOrWhiteSpace(_orderByExpression) ? string.Empty : $" order by {_orderByExpression}";

    public override Expression? Visit(Expression? node)
    {
        if (node == null)
        {
            return base.Visit(node);
        }
        
        if (node is not MethodCallExpression methodCallExpression)
        {
            throw new InvalidOperationException("OrderBy expression must be a method call expression.");
        }
        
        var isDescending = methodCallExpression.Method.Name == nameof(Queryable.OrderByDescending);
        
        // node.Arguments[1] is the lambda: x => x.Field
        var lambda = (LambdaExpression)ExpressionSanitizationHelper.StripQuotes(methodCallExpression.Arguments[1]);
        
        if (lambda.Body is not MemberExpression member) 
            return base.Visit(node);
        
        var fieldName = member.Member.Name.ToLowerInvariant();
        _orderByExpression = !isDescending ? fieldName : fieldName + " descending";

        return null;
    }
}