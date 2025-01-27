using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

public class FtrackLimitExpressionVisitor : ExpressionVisitor
{
    private string _limitExpression = string.Empty;
    
    public string LimitExpression => _limitExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        if (node is not ConstantExpression constantExpression)
        {
            throw new InvalidOperationException("Offset expression must be a constant.");
        }
        
        var take = (int?)constantExpression.Value;
        if (take != null)
        {
            _limitExpression = $" limit {take}";
        }

        return base.Visit(node);
    }
}