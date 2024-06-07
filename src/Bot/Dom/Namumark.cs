namespace Sugarmaple.TheSeed.Namumark;

using HtmlAgilityPack;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

internal enum TableState : byte
{
    None,
    CaptionStart,
    RowStart,
}

internal record class DesignLinkState(string TargetDocument, string Anchor, List<INamumarkClause> Children, int Index);

internal class ParseState
{
    public TableState TableState;
    public int Table;

    public Stack<DesignLinkState> LinkStack { get; } = new();
}

internal enum MatchResultType
{
    Element,
    Heading,
}

internal record struct MatchResult(NamumarkElement? Element, MatchResultType Type)
{

}

public class XPathResult<T> where T : NamumarkNode
{

}

internal record BracketInfo(string Name, [StringSyntax(StringSyntaxAttribute.Regex)] string Open, ParseCallback_Old Callback);

public class NamumarkHorizontalLineElement : NamumarkElement
{
    internal NamumarkHorizontalLineElement(NamumarkDocument? doc, in StringSegment raw) : base(doc, raw)
    {
    }

    protected override string ToMarkup()
    {
        return "----";
    }
}

/*public class NamumarkTrElement : NamumarkElement
{

}*/

public record struct PercentOrNum(int Degree, bool IsPercentage)
{

}

public class NamumarkFileLinkElement : NamumarkElement
{
    private string _refDocument;
    private PercentOrNum _width, _height;
    private FileAlign _align;
    private FileTheme _theme;

    internal NamumarkFileLinkElement(string refDocument,
        PercentOrNum width, PercentOrNum height,
        FileAlign align, FileTheme theme,
        NamumarkDocument doc, in StringSegment raw) : base(doc, raw)
    {
        _refDocument = refDocument;
        _width = width;
        _height = height;
        _align = align;
        _theme = theme;
    }

    protected override string ToMarkup()
    {
        return $"[[{_refDocument}]]";
    }
}

public class NamumarkIncludeMacroElement : NamumarkElement
{
    private string _refDocument;
    private readonly IDictionary<string, string> _argument;

    public string RefDocument
    {
        get => _refDocument; set
        {
            InvokeModifying();
            _refDocument = value;
        }
    }

    internal NamumarkIncludeMacroElement(string refDocument, IDictionary<string, string> argument, NamumarkDocument doc, in StringSegment raw) : base(doc, raw)
    {
        _refDocument = refDocument;
        _argument = argument;
    }

    protected override string ToMarkup()
    {
        return $"[include({_refDocument}{string.Concat(_argument.Select(o => $", {o.Key}={o.Value}"))})]";
    }
}

public class NamumarkCategoryLinkElement : NamumarkElement
{
    private string _refDocument;
    private bool _blur;

    public string RefDocument
    {
        get => _refDocument; set
        {
            InvokeModifying();
            _refDocument = value;
        }
    }

    public bool Blur
    {
        get => _blur; set
        {
            InvokeModifying();
            _blur = value;
        }
    }

    internal NamumarkCategoryLinkElement(string targetDocument, bool blur, NamumarkDocument doc, in StringSegment raw) : base(doc, raw)
    {
        _refDocument = targetDocument;
        _blur = blur;
    }

    protected override string ToMarkup()
    {
        return $"[[{_refDocument}{(_blur ? "#blur" : "")}]]";
    }
}

public interface INamumarkLink : INamumarkNode
{
    string RefDocument { get; set; }
}

public class NamumarkWikiLinkElement : NamumarkParentElement<INamumarkClause>, INamumarkLink
{
    private string _document;
    private string _anchor;

    internal NamumarkWikiLinkElement(string document, string anchor, List<INamumarkClause> children, NamumarkDocument doc, in StringSegment raw) : base(children, doc, raw)
    {
        _document = document;
        _anchor = anchor;
    }

    public string RefDocument
    {
        get => _document;
        set { InvokeModifying(); _document = value; }
    }
    public string Anchor
    {
        get => _anchor;
        set { InvokeModifying(); _anchor = value; }
    }

    protected override string ToMarkup()
    {
        return $"[[{_document}#{_anchor}|{ConcatChildren()}]]";
    }
}

public class NamumarkTextNode : NamumarkClause
{
    public string Data => _rawSource;

    internal NamumarkTextNode(NamumarkDocument? doc, in StringSegment raw) : base(doc, raw)
    {
    }

    protected override string ToMarkup() => Data;
}

public class NamumarkHeadingElement : NamumarkParentElement<INamumarkClause>
{
    private int _level;
    private bool _hidden;

    internal NamumarkHeadingElement(int level, bool hidden, List<INamumarkClause> children, NamumarkDocument doc, in StringSegment raw) : base(children, doc, raw)
    {
        _level = level;
        _hidden = hidden;
    }

    public int Level
    {
        get => _level; set
        {
            InvokeModifying();
            _level = value;
        }
    }
    public bool Hidden
    {
        get { return _hidden; }
        set { InvokeModifying(); _hidden = value; }
    }

    protected override string ToMarkup()
    {
        var ret = new StringBuilder();
        ret.Append('=', _level);
        if (_hidden) ret.Append('#');
        ret.Append(ConcatChildren());
        if (_hidden) ret.Append('#');
        ret.Append('=', _level);
        return ret.ToString();
    }
}

public class NamumarkParagraph : NamumarkElement
{
    private NamumarkHeadingElement _heading;
    private NamumarkParagraphContentElement _content;

    internal NamumarkParagraph(NamumarkHeadingElement heading, NamumarkParagraphContentElement content, NamumarkDocument doc, in StringSegment raw) : base(doc.DocumentElement, doc, raw)
    {
        _heading = heading;
        _heading.ParentElement = this;
        _content = content;
        _content.ParentElement = this;
    }

    protected override string ToMarkup()
    {
        return "";
    }
}

public interface INamumarkHeadingLevel : INamumarkParentNode<INamumarkClause>
{

}

public class NamumarkParagraphContentElement : NamumarkParentElement<INamumarkClause>, INamumarkHeadingLevel
{

    internal NamumarkParagraphContentElement(List<INamumarkClause> children, NamumarkDocument doc, in StringSegment raw) : base(children, doc, raw)
    {
    }

    protected override string ToMarkup() => ConcatChildren();

}

public abstract class NamumarkParentElement<TChild> : NamumarkElement, INamumarkParentNode<TChild> where TChild : INamumarkNode
{
    private readonly NamumarkNodeCollection<TChild> _children;

    internal NamumarkParentElement(List<TChild> children, NamumarkDocument doc, in StringSegment raw) : base(doc, raw)
    {
        _children = new(children, this);
    }

    public NamumarkNodeCollection<TChild> Children => _children;

    public NamumarkNodeCollection<TChild> ChildNodes => _children;

    public void RemoveChild(TChild child)
    {
        _children.Remove(child);
    }

    protected string ConcatChildren() => string.Concat(_children.Select(o => o.OuterMarkup));

    protected override string ToMarkup()
    {
        return string.Join("", _children.Select(o => o.OuterMarkup));
    }
}

/*public abstract class NamumarkParentElement : NamumarkElement, INamumarkParentNode<INamumarkClause>
{
    private readonly NamumarkClauseCollection _children;

    internal NamumarkParentElement(List<INamumarkClause> children, NamumarkDocument doc, in StringSegment raw) : base(doc, raw)
    {
        _children = new(children, this);
    }

    public NamumarkClauseCollection Children => _children;

    public void RemoveChild(INamumarkClause child)
    {
        _children.Remove(child);
    }

    protected string ConcatChildren() => string.Concat(_children.Select(o => o.OuterMarkup));
}*/

public interface INamumarkParentNode<TChild> : INamumarkNode where TChild : INamumarkNode
{
    void RemoveChild(TChild child);
    NamumarkNodeCollection<TChild> ChildNodes { get; }
}

public interface INamumarkParentNode<TSelf, TChild> : INamumarkNode<TSelf, TChild> where TSelf : INamumarkParentNode<TSelf, TChild> where TChild : INamumarkNode<TSelf, TChild>
{
    void RemoveChild(TChild child);
    NamumarkNodeCollection<TChild> ChildNodes { get; }
}

public static class ParentNodeExtensions
{
    public static IEnumerable<INamumarkNode> GetDescendantsAndSelf<TChild>(this INamumarkParentNode<TChild> parent) where TChild : INamumarkNode
    {
        var stack = new Stack<INamumarkNode>();
        stack.Push(parent);

        while (stack.Count > 0)
        {
            var next = stack.Pop();
            yield return next;

            if (next is INamumarkParentNode asParent)
            {
                var length = asParent.ChildNodes.Count;

                while (length > 0)
                    stack.Push(asParent.ChildNodes[--length]);
            }
        }
    }
}

public abstract class NamumarkElement : NamumarkNode
{
    internal NamumarkElement(NamumarkDocument? doc, in StringSegment raw) : this(null, doc, raw) { }

    internal NamumarkElement(NamumarkElement? parent, NamumarkDocument? doc, in StringSegment raw) : base(parent, doc, raw)
    {

    }


    public void Remove()
    {
        //ParentNode.RemoveChild(this);
    }
}

public class NamumarkNodeCollection<T> : IList<T> where T : INamumarkNode
{
    private readonly List<T> _items = new();
    private NamumarkElement _parent;

    public NamumarkNodeCollection(List<T> items, NamumarkElement parent)
    {
        _items = items;
        _parent = parent;
    }

    public T this[int index] { get => ((IList<T>)_items)[index]; set => ((IList<T>)_items)[index] = value; }

    public int Count => ((ICollection<T>)_items).Count;

    public bool IsReadOnly => ((ICollection<T>)_items).IsReadOnly;

    public void Add(T item)
    {
        ((ICollection<T>)_items).Add(item);
    }

    public void Clear()
    {
        ((ICollection<T>)_items).Clear();
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
        ((IList<T>)_items).Insert(index, item);
    }

    public bool Remove(T item)
    {
        return ((ICollection<T>)_items).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<T>)_items).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }
}

public class NamumarkClauseCollection : IList<INamumarkClause>
{
    private readonly List<INamumarkClause> _items = new();
    private NamumarkElement _parent;

    public NamumarkClauseCollection(List<INamumarkClause> items, NamumarkElement parent)
    {
        _items = items;
        _parent = parent;
    }

    public INamumarkClause this[int index] { get => ((IList<INamumarkClause>)_items)[index]; set => ((IList<INamumarkClause>)_items)[index] = value; }

    public int Count => ((ICollection<INamumarkClause>)_items).Count;

    public bool IsReadOnly => ((ICollection<INamumarkClause>)_items).IsReadOnly;

    public void Add(INamumarkClause item)
    {
        ((ICollection<INamumarkClause>)_items).Add(item);
    }

    public void Clear()
    {
        ((ICollection<INamumarkClause>)_items).Clear();
    }

    public bool Contains(INamumarkClause item)
    {
        return ((ICollection<INamumarkClause>)_items).Contains(item);
    }

    public void CopyTo(INamumarkClause[] array, int arrayIndex)
    {
        ((ICollection<INamumarkClause>)_items).CopyTo(array, arrayIndex);
    }

    public IEnumerator<INamumarkClause> GetEnumerator()
    {
        return ((IEnumerable<INamumarkClause>)_items).GetEnumerator();
    }

    public int IndexOf(INamumarkClause item)
    {
        return ((IList<INamumarkClause>)_items).IndexOf(item);
    }

    public void Insert(int index, INamumarkClause item)
    {
        ((IList<INamumarkClause>)_items).Insert(index, item);
    }

    public bool Remove(INamumarkClause item)
    {
        return ((ICollection<INamumarkClause>)_items).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<INamumarkClause>)_items).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }
}

public abstract class NamumarkClause : NamumarkNode, INamumarkClause
{
    internal NamumarkClause(NamumarkDocument? doc, in StringSegment raw) : this(null, doc, raw) { }

    internal NamumarkClause(NamumarkElement? parent, NamumarkDocument? doc, in StringSegment raw) : base(parent, doc, raw)
    {

    }
}


public interface INamumarkParentNode : INamumarkNode
{
    void RemoveChild(INamumarkNode child);
    NamumarkNodeCollection<INamumarkNode> ChildNodes { get; }
}

public interface INamumarkNode
{
    INamumarkParentNode ParentNode { get; }
    NamumarkElement? ParentElement { get; }
    NamumarkDocument? OwnerDocument { get; }
    string OuterMarkup { get; }
}

public interface INamumarkNode<TParent, TSelf> : INamumarkNode where TParent : INamumarkParentNode<TParent, TSelf> where TSelf : INamumarkNode<TParent, TSelf>
{
    TParent? ParentNode { get; }
}

public abstract class NamumarkNode : INamumarkNode
{
    protected bool _hasModified;
    private INamumarkParentNode? _parent;
    private NamumarkElement? _parentElement;
    private NamumarkDocument? _document;
    private protected StringSegment _rawSource;

    public INamumarkParentNode? ParentNode { get => _parent; internal set => _parent = value; }
    public NamumarkElement? ParentElement { get => _parentElement; internal set => _parentElement = value; }
    public NamumarkDocument? OwnerDocument => _document;
    public string OuterMarkup => _hasModified ? ToMarkup() : _rawSource;

    protected void InvokeModifying() => _hasModified = true;

    internal NamumarkNode(NamumarkDocument? doc, in StringSegment raw) : this(null, doc, raw) { }

    internal NamumarkNode(NamumarkElement? parent, NamumarkDocument? doc, in StringSegment raw)
    {
        _parentElement = parent;
        _document = doc;
        _rawSource = raw;
    }

    protected abstract string ToMarkup();
}

public interface INamumarkClause : INamumarkNode
{
}

internal record struct StringSegment(string Origin, int Index, int Length)
{
    public static readonly StringSegment Empty = new("", 0, 0);
    public int End => Index + Length;

    public override readonly string ToString() => Origin.Substring(Index, Length);
    public  readonly ReadOnlySpan<char> AsSpan() => Origin.AsSpan(Index, Length);

    public static implicit operator StringSegment(string raw) => raw.ToSegmentFromTo(0, raw.Length);
    public static implicit operator string(StringSegment raw) => raw.ToString();
}

internal static class StringExtension
{
    public static StringSegment ToSegmentFromTo(this string raw, int from, int to) => new(raw, from, to - from);

    public static StringSegment ToSegment(this string raw, Range range) => new(raw, range.Index, range.Length);
    public static StringSegment ToSegment(this string raw, int index, int length) => new(raw, index, length);
}