using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackOffsetExpressionVisitor : ExpressionVisitor
{
    private string _offsetExpression = string.Empty;
    
    public string OffsetExpression => _offsetExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        if (node is not ConstantExpression constantExpression)
        {
            throw new InvalidOperationException("Offset expression must be a constant.");
        }
        
        var skip = (int?)constantExpression.Value;
        if (skip != null)
        {
            _offsetExpression = $" offset {skip}";
        }

        return base.Visit(node);
    }
}