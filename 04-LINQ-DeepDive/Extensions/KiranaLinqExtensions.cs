namespace KiranaHub.LinqDeepDive.Extensions;

// =========================================================================================
// [KYA KARTA HAI]:
// Custom LINQ Extension methods jo `IEnumerable<T>` ko naye capabilities dete hain.
//
// [KYU ZAROORI HAI]:
// Built-in LINQ methods (jaise `Where`, `Select`) ke alawa domain-specific queries
// (jaise Pagination, Median, Moving Average) ko clean chained syntax me likhne ke liye.
//
// [KAISE CHALTA HAI]:
// Static class aur `this` keyword ke zariye C# compiler in methods ko instance methods ki tarah call karne deta hai.
// `yield return` use karne se custom extension method me bhi Deferred Execution برقرار rehti hai!
//
// [VARIATIONS TO TRY]:
// `Batch<T>(this IEnumerable<T> source, int size)` method banayein jo items ko chunk me group kare.
// =========================================================================================
public static class KiranaLinqExtensions
{
    // [PAGINATION EXTENSION]: Skip & Take ko reusable clean method me encapsulate karta hai
    public static IEnumerable<T> Paginate<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    // [MEDIAN AGGREGATE EXTENSION]: Sorted sequence ka middle value calculate karta hai
    public static decimal Median(this IEnumerable<decimal> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var sorted = source.OrderBy(x => x).ToList();
        if (sorted.Count == 0)
            throw new InvalidOperationException("Cannot compute median of an empty sequence.");

        int count = sorted.Count;
        if (count % 2 == 1)
        {
            return sorted[count / 2];
        }
        else
        {
            return (sorted[(count / 2) - 1] + sorted[count / 2]) / 2.0m;
        }
    }
}

