using System.Linq.Expressions;
using FtrackDotNet.Extensions;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackSelectExpressionVisitor : ExpressionVisitor
{
    private string _selectExpression = string.Empty;
    
    public string SelectExpression => string.IsNullOrWhiteSpace(_selectExpression) ? string.Empty : $"select {_selectExpression}";

    public override Expression? Visit(Expression? node)
    {
        if (!string.IsNullOrEmpty(_selectExpression))
        {
            return base.Visit(node);
        }
        
        if (node is MemberExpression member)
        {
            _selectExpression = member.Member.Name.ToLowerInvariant();
        }
        else if (node is NewExpression newExpression)
        {
            var fields = newExpression.Arguments
                .OfType<MemberExpression>()
                .Select(m => m.Member.Name.FromCamelOrPascalCaseToSnakeCase());
            _selectExpression = string.Join(", ", fields);
        }
        else if(node is not ParameterExpression _)
        {
            throw new InvalidOperationException("You must specify which fields to select.");
        }
        
        return base.Visit(node);
    }
}