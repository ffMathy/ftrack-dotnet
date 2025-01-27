using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

public class FtrackSelectExpressionVisitor : ExpressionVisitor
{
    private string _selectExpression = string.Empty;
    
    public string SelectExpression => _selectExpression;

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        // If it's a simple MemberExpression like x => x.Name, we handle that
        if (node is MemberExpression member)
        {
            _selectExpression = member.Member.Name.ToLowerInvariant();
        }
        else if (node is NewExpression newExpr)
        {
            // If it's a projection like x => new { x.FieldA, x.FieldB }, collect the members
            var fields = newExpr.Arguments
                .OfType<MemberExpression>()
                .Select(m => m.Member.Name.ToLowerInvariant());
            _selectExpression = string.Join(", ", fields);
        }
        // If there's no direct match, fallback to "*"
        
        return base.Visit(node);
    }
}