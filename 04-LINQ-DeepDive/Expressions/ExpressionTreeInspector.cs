using System.Linq.Expressions;
using KiranaHub.LinqDeepDive.Models;

namespace KiranaHub.LinqDeepDive.Expressions;

// =========================================================================================
// [KYA KARTA HAI]:
// Expression Trees (`Expression<Func<T, bool>>`) ko inspect karta hai.
//
// [KYU ZAROORI HAI]:
// C# developers aksar confuse hote hain: `Func<T, bool>` aur `Expression<Func<T, bool>>` me kya antar hai?
// - `Func`: Direct compiled IL code hota hai jo memory me execute hota hai (LINQ to Objects).
// - `Expression`: Abstract Syntax Tree (AST) hota hai jo code ko Data ki tarah represent karta hai.
//   Entity Framework Core is AST ko padh kar `SELECT * FROM Products WHERE Price > 100` SQL query generate karta hai!
//
// [KAISE CHALTA HAI]:
// Expression tree ke nodes (BinaryExpression, MemberExpression, ConstantExpression) ko traverse karke
// runtime par inspect kiya ja sakta hai.
// =========================================================================================
public static class ExpressionTreeInspector
{
    public static void InspectFilterExpression(Expression<Func<GroceryItem, bool>> expression)
    {
        Console.WriteLine($"[EXPRESSION TREE] Full Expression: {expression}");
        Console.WriteLine($"[EXPRESSION TREE] Body Type: {expression.Body.NodeType}");

        if (expression.Body is BinaryExpression binary)
        {
            Console.WriteLine($"   Left Operand : {binary.Left} (NodeType: {binary.Left.NodeType})");
            Console.WriteLine($"   Operator     : {binary.NodeType}");
            Console.WriteLine($"   Right Operand: {binary.Right} (NodeType: {binary.Right.NodeType})");
        }
    }
}

