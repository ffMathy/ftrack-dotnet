using System.Linq.Expressions;
using System.Reflection.Metadata;
using FtrackDotNet.Extensions;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackSelectExpressionVisitor : ExpressionVisitor
{
    private readonly HashSet<string> _selectExpressions = new HashSet<string>();

    public string SelectExpression => _selectExpressions.Count == 0
        ? string.Empty
        : $"select {string.Join(", ", _selectExpressions)}";

    public override Expression? Visit(Expression? node)
    {
        if (node is MemberExpression member)
        {
            _selectExpressions.Add(member.Member.Name.ToLowerInvariant());
        }
        else if (node is MethodCallExpression
                 {
                     Method.Name: nameof(Enumerable.Select), 
                     Arguments: [
                         ConstantExpression constantExpression, 
                         var selectLambdaExpression]
                 })
        {
            if (FtrackFromExpressionVisitor.TryParseEntityTypeFromConstantExpression(
                    constantExpression,
                    out var entityType))
            {
                //we always want to select the primary keys of an entity no matter what.
                //so for instance if the user only selected "name" of a "Task", we still fetch the "id" as well.
                //if we don't, we won't be able to update the entity again at a later point, as updates are based on primary keys.
                AttachPrimaryKeysOfEntityToSelectClause(entityType);
            }
            
            Visit((LambdaExpression)ExpressionSanitizationHelper.StripQuotes(selectLambdaExpression));
        }
        else if (node is NewExpression newExpression)
        {
            var fields = newExpression.Arguments
                .OfType<MemberExpression>()
                .Select(m => m.Member.Name.FromCamelOrPascalCaseToSnakeCase());
            foreach (var field in fields)
            {
                _selectExpressions.Add(field);
            }
        }

        return base.Visit(node);
    }

    private void AttachPrimaryKeysOfEntityToSelectClause(Type entityType)
    {
        var primaryKeys = FtrackContext.GetPrimaryKeysForEntity(entityType.Name, null);
        foreach (var primaryKey in primaryKeys)
        {
            _selectExpressions.Add(primaryKey.Name.FromCamelOrPascalCaseToSnakeCase());
        }
    }
}