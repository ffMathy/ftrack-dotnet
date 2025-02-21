using System.Text;
using FtrackDotNet.Linq.Visitors;

namespace FtrackDotNet.Linq;

using System;
using System.Linq;
using System.Linq.Expressions;

internal class FtrackExpressionVisitor : ExpressionVisitor
{
    private readonly FtrackSelectExpressionVisitor _selectExpressionVisitor = new ();

    private readonly FtrackOrderByExpressionVisitor _orderByExpressionVisitor = new ();
        
    private readonly FtrackWhereExpressionVisitor _whereExpressionVisitor = new ();
        
    private readonly FtrackFromExpressionVisitor _fromExpressionVisitor = new ();
    
    private readonly FtrackOffsetExpressionVisitor _offsetExpressionVisitor = new ();
    
    private readonly FtrackLimitExpressionVisitor _limitExpressionVisitor = new ();

    public string Translate(Expression expression)
    {
        Visit(expression);
        
        _fromExpressionVisitor.Visit(expression);

        if (string.IsNullOrWhiteSpace(_selectExpressionVisitor.SelectExpression))
        {
            throw new InvalidOperationException(
                "FtrackDotNet does not currently support creating an Ftrack query without a select clause.");
        }

        var queryBuilder = new StringBuilder();
        queryBuilder.Append(_selectExpressionVisitor.SelectExpression);
        queryBuilder.Append(_fromExpressionVisitor.FromExpression);
        queryBuilder.Append(_whereExpressionVisitor.WhereExpression);
        queryBuilder.Append(_orderByExpressionVisitor.OrderByExpression);
        queryBuilder.Append(_offsetExpressionVisitor.OffsetExpression);
        queryBuilder.Append(_limitExpressionVisitor.LimitExpression);
        
        return queryBuilder.ToString();
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable))
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.Where):
                    if(_whereExpressionVisitor.WhereExpression.Length > 0)
                    {
                        throw new InvalidOperationException("FtrackDotNet does not currently support multiple where clauses. Use an 'and' operator instead.");
                    }
                    
                    var whereLambdaExpression = (LambdaExpression)ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]);
                    _whereExpressionVisitor.Visit(whereLambdaExpression.Body);
                    break;

                case nameof(Queryable.Select):
                    _selectExpressionVisitor.VisitMethodCallExpression(node);
                    break;

                case nameof(Queryable.Skip):
                    _offsetExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]));
                    break;

                case nameof(Queryable.Take):
                    _limitExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]));
                    break;

                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                    _orderByExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node));
                    break;
            }
        }

        return base.VisitMethodCall(node);
    }
}