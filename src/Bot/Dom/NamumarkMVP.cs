namespace Sugarmaple.TheSeed.Namumark;

using Sugarmaple.TheSeed.Namumark;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;


//internal delegate void ParseCallbackMVP(NamumarkProcess process, int index, char context, out Token token);

internal record struct Range(int Index, int Length)
{
    public static Range FromTo(int from, int to) => new(from, to - from);
}

public class WikiBracket : ParentClause
{
    public string Tag { get; set; } = "";
}

public class Literal : Clause
{
    internal Literal(in StringSegment rawSource)
    {
    }
}

public class Text : Clause
{
    internal Text(in StringSegment rawSource)
    {
        MarkupRawCache.Add(this, rawSource);
    }
}

public class ExternalLink : ParentClause, IParentLink
{
    private string _reference = "";

    public string Reference { get => _reference; set => ChangeMember(ref _reference, value); }
}

public interface IReferer : IElement
{
    string Reference { get; set; }
    void ReplaceWith(Clause clause);
}

public interface IParentLink : IReferer
{
    ElementList<Clause> Children { get; }
}

public class Redirect : Clause, IReferer
{
    private string _reference = "";
    private string? _anchor;

    public string Reference { get => _reference; set => ChangeMember(ref _reference, value); }
    public string? Anchor { get => _anchor; set => ChangeMember(ref _anchor, value); }
}

public class CategoryLink : Clause, IReferer
{
    private string _reference = "";
    private string? _display;
    private bool _blur;

    public string Reference
    {
        get => _reference; set => ChangeMember(ref _reference, value);
    }
    public string? Display
    {
        get => _display; set => ChangeMember(ref _display, value);
    }
    public bool Blur { get => _blur; set => _blur = value; }
}

public class Bold : ParentClause
{
}

public class Italic : ParentClause
{
}

public class InternalLink : ParentClause, IParentLink
{
    private string _reference = "";
    private string? _anchor;

    public string Reference
    {
        get => _reference; set => ChangeMember(ref _reference, value);
    }

    public string? Anchor
    {
        get => _anchor; set => ChangeMember(ref _anchor, value);
    }
}

public class FileAttributes : IDictionary<string, string>
{
    Dictionary<string, string> _spec = new();

    public string? Width => _spec["width"];
    public string? Height => _spec["height"];
    public FileAlign Align { get; set; }
    public FileTheme Theme { get; set; }

    public string ToMarkup()
    {
        var sb = new StringBuilder();
        sb.AppendJoin('&', _spec.Select(o => $"{o.Key}={o.Value}"));
        return sb.ToString();
    }


    #region IDict
    public string this[string key] { get => ((IDictionary<string, string>)_spec)[key]; set => ((IDictionary<string, string>)_spec)[key] = value; }

    public ICollection<string> Keys => ((IDictionary<string, string>)_spec).Keys;

    public ICollection<string> Values => ((IDictionary<string, string>)_spec).Values;

    public int Count => ((ICollection<KeyValuePair<string, string>>)_spec).Count;

    public bool IsReadOnly => ((ICollection<KeyValuePair<string, string>>)_spec).IsReadOnly;

    public void Add(string key, string value)
    {
        ((IDictionary<string, string>)_spec).Add(key, value);
    }

    public void Add(KeyValuePair<string, string> item)
    {
        ((ICollection<KeyValuePair<string, string>>)_spec).Add(item);
    }

    public void Clear()
    {
        ((ICollection<KeyValuePair<string, string>>)_spec).Clear();
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        return ((ICollection<KeyValuePair<string, string>>)_spec).Contains(item);
    }

    public bool ContainsKey(string key)
    {
        return ((IDictionary<string, string>)_spec).ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, string>>)_spec).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, string>>)_spec).GetEnumerator();
    }

    public bool Remove(string key)
    {
        return ((IDictionary<string, string>)_spec).Remove(key);
    }

    public bool Remove(KeyValuePair<string, string> item)
    {
        return ((ICollection<KeyValuePair<string, string>>)_spec).Remove(item);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        return ((IDictionary<string, string>)_spec).TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_spec).GetEnumerator();
    }
    #endregion
}

public class FileLink : Clause, IReferer
{
    private string _reference;
    public FileAttributes Attributes { get; init; } = new();

    public string Reference
    {
        get => _reference; set
        {
            {
                _reference = value;
                NotifyChange();
            }
        }
    }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public FileAlign Align { get; set; }
    public FileTheme Theme { get; set; }
}
