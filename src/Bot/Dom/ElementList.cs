namespace Sugarmaple.TheSeed.Namumark;

using System.Collections;
using System.Diagnostics;

[DebuggerDisplay("Count = {Count}")]
public class NamumarkClauseList : IList<Clause>, IReadOnlyList<Clause>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly List<Clause> _items = new();

    internal NamumarkClauseList()
    {
    }

    internal NamumarkClauseList(List<Clause> items)
    {
        _items = items;
    }

    public Clause this[int index] { get => ((IList<Clause>)_items)[index]; set => ((IList<Clause>)_items)[index] = value; }

    public int Count => ((ICollection<Clause>)_items).Count;

    public bool IsReadOnly => ((ICollection<Clause>)_items).IsReadOnly;

    public void Add(Clause item)
    {
        ((ICollection<Clause>)_items).Add(item);
    }

    public void Clear()
    {
        ((ICollection<Clause>)_items).Clear();
    }

    public bool Contains(Clause item)
    {
        return ((ICollection<Clause>)_items).Contains(item);
    }

    public void CopyTo(Clause[] array, int arrayIndex)
    {
        ((ICollection<Clause>)_items).CopyTo(array, arrayIndex);
    }

    public IEnumerator<Clause> GetEnumerator()
    {
        return ((IEnumerable<Clause>)_items).GetEnumerator();
    }

    public int IndexOf(Clause item)
    {
        return ((IList<Clause>)_items).IndexOf(item);
    }

    public void Insert(int index, Clause item)
    {
        ((IList<Clause>)_items).Insert(index, item);
    }

    public bool Remove(Clause item)
    {
        return ((ICollection<Clause>)_items).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<Clause>)_items).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }
}

[DebuggerDisplay("Count = {Count}", Target = typeof(IEnumerable))]
public class ElementList<T> : IList<T>, IReadOnlyList<T> where T : IElement

{
    public IParentElement<T> Parent { get; internal set; }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly List<T> _items = new();

    internal ElementList(IParentElement<T> parent)
    {
        Parent = parent;
    }

    internal ElementList(List<T> items)
    {
        _items = items;
    }

    public T this[int index]
    {
        get => ((IList<T>)_items)[index]; set
        {
            ((IList<T>)_items)[index] = value;
            Parent.AsWeak().NotifyChange();
        }
    }

    public int Count => ((ICollection<T>)_items).Count;

    public bool IsReadOnly => ((ICollection<T>)_items).IsReadOnly;

    public void Add(T item)
    {
        item.AsWeak().Parent = Parent;
        ((ICollection<T>)_items).Add(item);
        Parent.NotifyModifying();
    }

    public void Clear()
    {
        foreach (T item in _items)
            item.AsWeak().Parent = null;
        ((ICollection<T>)_items).Clear();
        Parent.NotifyModifying();
    }

    public bool Contains(T item)
    {
        return ((ICollection<T>)_items).Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        ((ICollection<T>)_items).CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_items).GetEnumerator();
    }

    public int IndexOf(T item)
    {
        return ((IList<T>)_items).IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        item.AsWeak().Parent = Parent;
        ((IList<T>)_items).Insert(index, item);
        Parent.NotifyModifying();
    }


    public bool Remove(T item)
    {
        var ret = ((ICollection<T>)_items).Remove(item);
        item.AsWeak().Parent = null;
        Parent.NotifyModifying();
        return ret;
    }

    public void RemoveAt(int index)
    {
        _items[index].AsWeak().Parent = null;
        ((IList<T>)_items).RemoveAt(index);
        Parent.NotifyModifying();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }

    public string OuterMarkup => string.Concat(_items.Select(o => o.OuterMarkup));
}