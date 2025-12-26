namespace CustomChartLoader;

public static class Extensions
{
    public static IEnumerable<T> PadLengthTo<T>(this IEnumerable<T> enumerable, int n) where T : new()
    {
        var list = enumerable.ToList();
        if (list.Count >= n)
            return list;

        for (var i = list.Count; i < n; i++)
        {
            list.Add(new T());
        }
        return list;
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> e) where T : class =>
        e.Where(x => x is not null).Cast<T>();

    public static string AppendLine(this string a, string b) => a + b + "\n";
    public static string Quote(this string s) => '"' + s + '"';
}