using System.Linq.Expressions;
using System.Text;

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
    private List<string> _selectedFields;

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
        var query = $"select id from {_entityName}";
        if (!string.IsNullOrEmpty(whereClause))
        {
            query += $" where ({whereClause})";
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
        // Detect .Select(...)
        if (node.Method.Name == "Select" && node.Method.DeclaringType == typeof(Queryable))
        {
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
            CaptureSelectedFields(lambda.Body);

            // Visit the source in case there’s .Where(...) or other calls before/after
            Visit(node.Arguments[0]);
            return node;
        }

        if (node.Method.Name == "Take" && node.Method.DeclaringType == typeof(Queryable))
        {
            // Evaluate the argument, which is the integer count
            var takeCount = EvaluateIntArgument(node.Arguments[1]);
            _limit = takeCount;

            // Visit the source
            Visit(node.Arguments[0]);
            return node;
        }

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
    
    // For example, if the user does Select(t => new { t.Name, t.Bid }),
// we have a NewExpression or MemberInit. We gather property references.
    private void CaptureSelectedFields(Expression body)
    {
        if (body is NewExpression newExpression)
        {
            var fields = new List<string>();
            foreach (var arg in newExpression.Arguments)
            {
                string fieldName = GetValueFromExpression(arg); 
                // e.g. "name" or "bid"
                fields.Add(fieldName);
            }
            _selectedFields = fields;
        }
        else if (body is MemberInitExpression initExpr)
        {
            // for "Select(t => new SomeDto { Name = t.Name, Bid = t.Bid })"
            // you'd similarly gather
            var fields = new List<string>();
            foreach (var bind in initExpr.Bindings)
            {
                if (bind is MemberAssignment ma)
                {
                    string fieldName = GetValueFromExpression(ma.Expression);
                    fields.Add(fieldName);
                }
            }
            _selectedFields = fields;
        }
        else
        {
            // If it's just Select(t => t), you might do nothing or store all fields. 
            // The tests might not require that scenario.
        }
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
        if (node.Expression == null)
        {
            throw new InvalidOperationException("Node expression not present.");
        }
        
        // If it's a property/field on the lambda parameter, e.g. t => t.Parent.Name
        // We'll build "parent.name" (lowercasing for consistency).
        if (node.Expression.NodeType is ExpressionType.Parameter or ExpressionType.MemberAccess)
        {
            var path = ExpressionToPath(node);
            PushStackFragment(path);
        }
        else if (node.NodeType is ExpressionType.MemberAccess && TypeSystem.IsEnumerable(node.Type))
        {
            var valueWrapper = Expression.Lambda(node.Expression)
                .Compile()
                .DynamicInvoke();
            if (valueWrapper == null)
            {
                throw new NullReferenceException("The enumerable is null.");
            }
            
            var valueWrapperType = valueWrapper.GetType();
            var field = valueWrapperType.GetFields().Single();
            
            var value = 
                field.GetValue(valueWrapper) ??
                throw new NullReferenceException("The enumerable is null.");
            var valueType = value.GetType();

            var genericType = valueType.GetGenericArguments()[0];
            
            var genericIEnumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
            var getEnumeratorMethod = 
                genericIEnumerableType.GetMethod(nameof(IEnumerable<object>.GetEnumerator)) ?? 
                throw new InvalidOperationException("Could not find GetEnumerator method.");
            var enumerator = getEnumeratorMethod.Invoke(value, null) ?? throw new InvalidOperationException("Enumerator returned null.");
            var genericEnumeratorType = enumerator.GetType();
            
            var moveNextMethod = 
                genericEnumeratorType.GetMethod(nameof(IEnumerator<object>.MoveNext)) ?? 
                throw new InvalidOperationException("Could not find MoveNext method on enumerator.");
            var currentProperty = 
                genericEnumeratorType.GetProperty(nameof(IEnumerator<object>.Current)) ?? 
                throw new InvalidOperationException("Could not find Current property on enumerator.");

            var queryStringBuilder = new StringBuilder();
            queryStringBuilder.Append(" in (");

            var values = new List<object?>();
            
            var hasNext = () => (bool) moveNextMethod.Invoke(enumerator, [])!;
            while (hasNext())
            {
                var current = currentProperty.GetValue(enumerator);
                values.Add(current);
            }

            queryStringBuilder.Append(values
                .Select(x => x is string ? $"'{x}'" : (x ?? "null"))
                .Aggregate((x, y) => $"{x}, {y}"));
            queryStringBuilder.Append(")");
            
            PushStackFragment(queryStringBuilder.ToString());
        }
        else
        {
            throw new InvalidOperationException("Could not parse expression: " + node.NodeType + " of type " + node.Type);
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

        if (expr is ParameterExpression p)
        {
            return p.Name ?? "";
        }

        if (expr is MemberExpression m)
        {
            return m.Member.Name;
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
