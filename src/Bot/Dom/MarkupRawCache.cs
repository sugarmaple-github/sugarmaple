namespace Sugarmaple.TheSeed.Namumark;

internal static class MarkupRawCache
{
    private static readonly Dictionary<object, StringSegment> _markupCache = new();

    public static void Add(object clause, StringSegment value) => _markupCache[clause] = value;
    public static void Remove(object clause) => _markupCache.Remove(clause);
    public static bool TryGetValue(object clause, out StringSegment value) => _markupCache.TryGetValue(clause, out value);
}
