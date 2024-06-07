namespace Sugarmaple.TheSeed.Crawler;
using System.Collections;

public enum LogType
{
    All,
    Create,
    Delete,
    Move,
    Revert,
}

public class RecentChanges : IReadOnlyList<DocumentChange>
{
    private readonly DocumentChange[] _changes;

    internal RecentChanges(DocumentChange[] changes)
    {
        _changes = changes;
    }

    public DocumentChange this[int index] => ((IReadOnlyList<DocumentChange>)_changes)[index];

    public int Count => ((IReadOnlyCollection<DocumentChange>)_changes).Count;

    public IEnumerator<DocumentChange> GetEnumerator()
    {
        return ((IEnumerable<DocumentChange>)_changes).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _changes.GetEnumerator();
    }
}

public record struct DocumentChange(string Title, int SizeChange, string User, DateTime Time, string Comment);
public class OldPages : IReadOnlyList<OldPage>
{
    private readonly OldPage[] _pages;

    internal OldPages(OldPage[] pages)
    {
        _pages = pages;
    }

    public OldPage this[int index] => ((IReadOnlyList<OldPage>)_pages)[index];

    public int Count => ((IReadOnlyCollection<OldPage>)_pages).Count;

    public IEnumerator<OldPage> GetEnumerator()
    {
        return ((IEnumerable<OldPage>)_pages).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _pages.GetEnumerator();
    }
}

public record struct OldPage(string Title, DateTime Time);

public class PageListBySize : IReadOnlyList<PageAndSize>
{
    private readonly PageAndSize[] _pages;

    public PageListBySize(PageAndSize[] pages)
    {
        _pages = pages;
    }

    public PageAndSize this[int index] => ((IReadOnlyList<PageAndSize>)_pages)[index];

    public int Count => ((IReadOnlyCollection<PageAndSize>)_pages).Count;

    public IEnumerator<PageAndSize> GetEnumerator()
    {
        return ((IEnumerable<PageAndSize>)_pages).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _pages.GetEnumerator();
    }
}
public record struct PageAndSize(string Title, int Size);

public class SpecialPageList : IReadOnlyList<string>
{
    public Namespaces Namespaces { get; }
    private readonly string[] _pages;

    internal SpecialPageList(Namespaces namespaces, string[] pages)
    {
        _pages = pages;
        Namespaces = namespaces;
    }

    public string this[int index] => ((IReadOnlyList<string>)_pages)[index];

    public int Count => ((IReadOnlyCollection<string>)_pages).Count;

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)_pages).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _pages.GetEnumerator();
    }
}

public class Namespaces : IReadOnlyList<string>
{
    private readonly string[] _namespaces;

    internal Namespaces(string[] namespaces)
    {
        _namespaces = namespaces;
    }

    public string this[int index] => ((IReadOnlyList<string>)_namespaces)[index];

    public int Count => ((IReadOnlyCollection<string>)_namespaces).Count;

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)_namespaces).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _namespaces.GetEnumerator();
    }
}