using System.Linq.Expressions;

namespace FtrackDotNet.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/// <summary>
/// Visits expression trees (from .Where(...), .Skip(...), .Take(...), etc.)
/// and produces a FTrack query string such as:
///   "Task where status.name is \"Open\" limit 10 offset 20"
/// </summary>
public class FtrackExpressionVisitor : ExpressionVisitor
{
    private readonly string _entityName;

    // For building our WHERE clause, we use a stack of partial expressions.
    // We push fragments as we visit, then pop/combine them in parent nodes.
    private readonly Stack<string> _stack = new();

    // Track limit/offset (mapped from Take/Skip).
    private int? _limit;
    private int? _offset;

    /// <summary>
    /// Construct the visitor, specifying an entity name like "Task" or "AssetBuild".
    /// </summary>
    public FtrackExpressionVisitor(string entityName)
    {
        _entityName = entityName;
    }

    /// <summary>
    /// Main entry point. Call this on your expression tree.
    /// It visits all nodes and then returns the final FTrack query string.
    /// </summary>
    public string Translate(Expression expression)
    {
        Visit(expression);

        // The WHERE clause (if we built one) is on top of the stack.
        // If there's no WHERE clause, we just leave it blank.
        var whereClause = _stack.Count > 0 ? _stack.Peek() : string.Empty;

        // Build final query. For example:
        //   "Task where status.name is \"Open\" limit 10 offset 20"
        // If there's no WHERE clause, it might just be "Task limit 10 offset 20", etc.
        var query = _entityName;
        if (!string.IsNullOrEmpty(whereClause))
        {
            query += $" where {whereClause}";
        }

        // Append LIMIT/OFFSET if set
        if (_limit.HasValue)
        {
            query += $" limit {_limit.Value}";
        }
        if (_offset.HasValue)
        {
            query += $" offset {_offset.Value}";
        }

        return query;
    }

    /// <summary>
    /// Handle method calls like .Where(...), .Skip(...), .Take(...), and string methods
    /// (Contains, StartsWith, EndsWith).
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // 1. Detect .Where(...)
        if (node.Method.Name == "Where" && node.Method.DeclaringType == typeof(Queryable))
        {
            // The 2nd argument of .Where(...) is the lambda with the predicate
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
            Visit(lambda.Body);

            // Also visit the source (the first argument) in case there's chaining
            Visit(node.Arguments[0]);

            return node;
        }

        // 2. Detect .Take(...)
        if (node.Method.Name == "Take" && node.Method.DeclaringType == typeof(Queryable))
        {
            // Evaluate the argument, which is the integer count
            var takeCount = EvaluateIntArgument(node.Arguments[1]);
            _limit = takeCount;

            // Visit the source
            Visit(node.Arguments[0]);
            return node;
        }

        // 3. Detect .Skip(...)
        if (node.Method.Name == "Skip" && node.Method.DeclaringType == typeof(Queryable))
        {
            var skipCount = EvaluateIntArgument(node.Arguments[1]);
            _offset = skipCount;

            // Visit the source
            Visit(node.Arguments[0]);
            return node;
        }

        // 4. Detect string methods: Contains, StartsWith, EndsWith
        if (node.Method.DeclaringType == typeof(string))
        {
            switch (node.Method.Name)
            {
                case "Contains":   return HandleStringLikeMethod(node, "contains");
                case "StartsWith": return HandleStringLikeMethod(node, "startswith");
                case "EndsWith":   return HandleStringLikeMethod(node, "endswith");
            }
        }

        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Handles .Contains, .StartsWith, .EndsWith by generating a "like" clause with wildcards.
    /// E.g. x => x.Name.Contains("foo") => "name like \"%foo%\""
    /// </summary>
    private Expression HandleStringLikeMethod(MethodCallExpression node, string methodName)
    {
        // The object is typically a MemberExpression: x.SomeProp
        var leftPath = ExpressionToPath(node.Object);

        // The argument is a constant or evaluatable expression
        var argValue = GetValueFromExpression(node.Arguments[0]);

        // Build a "like" expression with wildcards
        var likePattern = argValue;
        switch (methodName)
        {
            case "contains":
                likePattern = $"%{argValue}%";
                break;
            case "startswith":
                likePattern = $"{argValue}%";
                break;
            case "endswith":
                likePattern = $"%{argValue}";
                break;
        }

        var fragment = $"{leftPath} like \"{likePattern}\"";
        PushStackFragment(fragment);

        return node;
    }

    /// <summary>
    /// Handle binary expressions, including logical ops (&&, ||) and comparisons (==, !=, &gt;, &lt;, etc.).
    /// </summary>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Check for logical operators first
        if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
        {
            return HandleLogicalOperator(node);
        }

        // Otherwise assume it's a comparison operator (==, !=, &gt;, &lt;, etc.)
        return HandleComparisonOperator(node);
    }

    private Expression HandleLogicalOperator(BinaryExpression node)
    {
        // Visit left side
        Visit(node.Left);
        var leftFrag = _stack.Pop();

        // Visit right side
        Visit(node.Right);
        var rightFrag = _stack.Pop();

        // Map && => "and", || => "or"
        var op = (node.NodeType == ExpressionType.AndAlso) ? "and" : "or";

        // Combine with parentheses
        var combined = $"({leftFrag} {op} {rightFrag})";
        PushStackFragment(combined);

        return node;
    }

    private Expression HandleComparisonOperator(BinaryExpression node)
    {
        // Visit left
        Visit(node.Left);
        var leftFrag = _stack.Pop();

        // Visit right
        Visit(node.Right);
        var rightFrag = _stack.Pop();

        // Map ExpressionType => FTrack operator
        var op = node.NodeType switch
        {
            ExpressionType.Equal => "is",         // For strings in FTrack, "is" often used for equality
            ExpressionType.NotEqual => "is not",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Unsupported binary operator: {node.NodeType}")
        };

        var fragment = $"{leftFrag} {op} {rightFrag}";
        PushStackFragment(fragment);

        return node;
    }

    /// <summary>
    /// Visits a MemberExpression, which could be "x.Status" or a captured variable's property.
    /// If it's on the parameter, we build a path like "status.name".
    /// Otherwise, we evaluate it (like a captured variable).
    /// </summary>
    protected override Expression VisitMember(MemberExpression node)
    {
        // If it's a property/field on the lambda parameter, e.g. t => t.Parent.Name
        // We'll build "parent.name" (lowercasing for consistency).
        if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
        {
            var path = ExpressionToPath(node);
            PushStackFragment(path);
        }
        else
        {
            // Likely a captured variable or something else: evaluate it
            var value = GetValueFromExpression(node);
            var quoted = QuoteIfString(value);
            PushStackFragment(quoted);
        }

        return node;
    }

    /// <summary>
    /// For a constant like "Open" or 123. 
    /// We'll convert it to a string, quoting if it's not numeric.
    /// </summary>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        var valueStr = node.Value != null ? node.Value.ToString() : "null";
        var quoted = QuoteIfString(valueStr);
        PushStackFragment(quoted);
        return node;
    }

    /// <summary>
    /// Evaluate an integer expression (for .Take(n), .Skip(n) arguments).
    /// Usually a ConstantExpression or something we can compile easily.
    /// </summary>
    private int EvaluateIntArgument(Expression expression)
    {
        var val = Expression.Lambda(expression).Compile().DynamicInvoke();
        return Convert.ToInt32(val);
    }

    /// <summary>
    /// Utility to push a string fragment onto our stack.
    /// </summary>
    private void PushStackFragment(string fragment)
    {
        _stack.Push(fragment);
    }

    /// <summary>
    /// Convert an expression to a property path (e.g. t => t.Parent.Name => "parent.name"),
    /// by walking up MemberExpressions until we reach the parameter.
    /// </summary>
    private string ExpressionToPath(Expression expression)
    {
        var pathParts = new List<string>();
        var e = expression;
        while (e is MemberExpression m)
        {
            pathParts.Insert(0, m.Member.Name.ToLower());
            e = m.Expression;
        }
        return string.Join(".", pathParts);
    }

    /// <summary>
    /// Evaluate an expression if it's not a direct property path,
    /// returning the string representation of its value.
    /// </summary>
    private string GetValueFromExpression(Expression expr)
    {
        if (expr is ConstantExpression c)
        {
            return c.Value?.ToString() ?? "";
        }

        // If it's a MemberExpression, it might be referencing a captured variable
        // or something else. Evaluate it by compiling a small lambda.
        var result = Expression.Lambda(expr).Compile().DynamicInvoke();
        return result?.ToString() ?? "";
    }

    /// <summary>
    /// Quotes the string if it is not numeric. 
    /// For FTrack queries, we typically use double quotes around string values.
    /// </summary>
    private string QuoteIfString(string value)
    {
        // Naive check: if it's numeric, don't quote
        if (decimal.TryParse(value, out _))
        {
            return value; // numeric
        }
        // Otherwise, treat it as a string
        return $"'{value}'";
    }

    /// <summary>
    /// Strips ExpressionType.Quote from around a lambda expression (typical in .Where calls).
    /// </summary>
    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }
}
