using System.Linq.Expressions;

namespace KiranaHub.LinqDeepDive.Expressions;

// =========================================================================================
// [KYA KARTA HAI]:
// Custom `ExpressionVisitor` jo Abstract Syntax Tree (AST) ko traverse karke dynamically rewrite karta hai.
//
// [KYU ZAROORI HAI]:
// Entity Framework Core, OData, aur GraphQL libraries internally `ExpressionVisitor` use karti hain
// user ke C# queries ko modify ya translate karne ke liye (jaise soft-delete `WHERE IsDeleted = false` auto-inject karna).
//
// [KAISE CHALTA HAI]:
// `VisitConstant` method AST me hardcoded values ko intercept karta hai aur change kar deta hai.
//
// [VARIATIONS TO TRY]:
// Property name intercept karke dynamic column mapping banayein.
// =========================================================================================
public class PriceThresholdRewriter : ExpressionVisitor
{
    private readonly decimal _oldThreshold;
    private readonly decimal _newThreshold;

    public PriceThresholdRewriter(decimal oldThreshold, decimal newThreshold)
    {
        _oldThreshold = oldThreshold;
        _newThreshold = newThreshold;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Type == typeof(decimal) && (decimal)node.Value! == _oldThreshold)
        {
            // Rewrite the constant node with the new threshold
            return Expression.Constant(_newThreshold, typeof(decimal));
        }

        return base.VisitConstant(node);
    }
}

