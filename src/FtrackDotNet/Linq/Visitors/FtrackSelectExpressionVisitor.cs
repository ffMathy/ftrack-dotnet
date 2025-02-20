using System.Linq.Expressions;
using System.Reflection.Metadata;
using FtrackDotNet.Extensions;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Linq.Visitors;

internal class FtrackSelectExpressionVisitor : ExpressionVisitor
{
    private readonly HashSet<string> _selectExpressions = [];

    public string SelectExpression => _selectExpressions.Count == 0
        ? string.Empty
        : $"select {string.Join(", ", _selectExpressions)}";

    public Expression? VisitMethodCallExpression(MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Method.Name != nameof(Enumerable.Select))
        {
            throw new InvalidOperationException("Only select expressions are supported.");
        }
        
        var constantExpression = methodCallExpression.Arguments[0] as ConstantExpression;
        var selectLambdaExpression = methodCallExpression.Arguments[1];
        if (FtrackFromExpressionVisitor.TryParseEntityTypeFromConstantExpression(
                constantExpression,
                out var entityType))
        {
            //we always want to select the primary keys of an entity no matter what.
            //so for instance if the user only selected "name" of a "Task", we still fetch the "id" as well.
            //if we don't, we won't be able to update the entity again at a later point, as updates are based on primary keys.
            AttachPrimaryKeysOfEntityToSelectClause(entityType);
        }
            
        return Visit((LambdaExpression)ExpressionSanitizationHelper.StripQuotes(selectLambdaExpression));
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        _selectExpressions.Add(node.Member.Name.FromCamelOrPascalCaseToSnakeCase());
        return base.VisitMember(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        var fields = node.Arguments
            .OfType<MemberExpression>()
            .Select(m => m.Member.Name.FromCamelOrPascalCaseToSnakeCase());
        foreach (var field in fields)
        {
            _selectExpressions.Add(field);
        }
        return base.VisitNew(node);
    }

    private void AttachPrimaryKeysOfEntityToSelectClause(Type entityType)
    {
        var primaryKeys = FtrackContext.GetPrimaryKeysForEntity(entityType.Name);
        foreach (var primaryKey in primaryKeys)
        {
            _selectExpressions.Add(primaryKey.Name.FromCamelOrPascalCaseToSnakeCase());
        }
    }
}