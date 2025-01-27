using System.Linq.Expressions;

namespace FtrackDotNet.Linq.Visitors;

internal class ExpressionSanitizationHelper
{
    public static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }
}