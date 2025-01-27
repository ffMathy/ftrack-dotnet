using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

public class FtrackSelectExpressionVisitor : ExpressionVisitor
{
    private string _selectExpression = string.Empty;
    
    public string SelectExpression => string.IsNullOrWhiteSpace(_selectExpression) ? string.Empty : $"select {_selectExpression}";

    public override Expression? Visit(Expression? node)
    {
        if (!string.IsNullOrEmpty(_selectExpression))
        {
            return base.Visit(node);
        }
        
        // If it's a simple MemberExpression like x => x.Name, we handle that
        if (node is MemberExpression member)
        {
            _selectExpression = member.Member.Name.ToLowerInvariant();
        }
        else if (node is NewExpression newExpression)
        {
            // If it's a projection like x => new { x.FieldA, x.FieldB }, collect the members
            var fields = newExpression.Arguments
                .OfType<MemberExpression>()
                .Select(m => m.Member.Name.ToLowerInvariant());
            _selectExpression = string.Join(", ", fields);
        }
        else if(node is not ParameterExpression _)
        {
            throw new InvalidOperationException("You must specify which fields to select.");
        }
        
        return base.Visit(node);
    }
}