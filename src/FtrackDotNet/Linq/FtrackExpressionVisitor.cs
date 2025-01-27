using System.Text;
using FtrackDotNet.Linq.Visitors;

namespace FtrackDotNet.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public class FtrackExpressionVisitor : ExpressionVisitor
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
        // Handle Where, Select, Skip, Take, OrderBy, etc.
        if (node.Method.DeclaringType == typeof(Queryable))
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.Where):
                    // node.Arguments[1] should be a lambda: x => BooleanExpression
                    var whereLambdaExpression = (LambdaExpression)ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]);
                    _whereExpressionVisitor.Visit(whereLambdaExpression.Body);
                    break;

                case nameof(Queryable.Select):
                    // node.Arguments[1] is the lambda: x => new { x.FieldA, x.FieldB, ... }
                    var selectLambdaExpression = (LambdaExpression)ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]);
                    _selectExpressionVisitor.Visit(selectLambdaExpression.Body);
                    break;

                case nameof(Queryable.Skip):
                    _offsetExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]));
                    break;

                case nameof(Queryable.Take):
                    _limitExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node.Arguments[1]));
                    break;

                case nameof(Queryable.OrderBy):
                    _orderByExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node));
                    break;

                case nameof(Queryable.OrderByDescending):
                    _orderByExpressionVisitor.Visit(ExpressionSanitizationHelper.StripQuotes(node));
                    break;
            }
        }

        // Visit the rest of the expression tree
        return base.VisitMethodCall(node);
    }
}