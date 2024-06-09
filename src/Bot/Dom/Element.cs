namespace Sugarmaple.TheSeed.Namumark;

using System.ComponentModel;
using System.Diagnostics;

internal interface IWeakElement
{
    IParentElement? Parent { get; set; }
}

[DebuggerDisplay("{" + nameof(OuterMarkup) + "}")]
public abstract class Element : IElement, IWeakElement, IChangeTracking
{
    private Document? _ownerDocument;

    public string OuterMarkup
    {
        get
        {
            return NamuFormatter.Default.ToMarkup(this);
        }
    }


    public IParentElement? Parent { get => ParentInner; set => ParentInner = value; }

    protected abstract IParentElement? ParentInner { get; set; }

    public string LocalName
    {
        get
        {
            return this switch
            {
                BrMacro => "br",
                Include => "include",
                Table => "table",
                TableRow => "tr",
                TableData => "td",
                InternalLink => "inlink",
                ExternalLink => "outlink",
                FileLink => "image",
                Literal => "literal",
                _ => throw new NotImplementedException($"{GetType().Name} is not implemented "),
            };
        }
    }

    public Document? OwnerDocument { get => _ownerDocument; set => _ownerDocument = value; }

    public bool IsChanged { get; private set; }

    protected void ChangeMember<T>(ref T member, T value)
    {
        if (EqualityComparer<T>.Default.Equals(member, value))
        {
            NotifyChange();
            member = value;
        }
    }

    internal void NotifyChange()
    {
        IsChanged = true;
        MarkupRawCache.Remove(this);
        if (Parent is Element e)
            e.NotifyChange();
    }

    public void AcceptChanges()
    {
        IsChanged = false;
    }
}

public static class ElementExtensions
{
    /// <summary>
    /// 이스케이핑 없이 문자열을 해당 원소 앞에 삽입합니다. 마크업 문법의 일관성을 보장하지 않습니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="text"></param>
    public static void Append(this Clause self, string text)
    {
        self.Parent.Children.Insert(self.Parent.Children.IndexOf(self), new Text(text));
    }

    internal static Element AsWeak(this IElement self)
    {
        return (Element)self;
    }

    internal static void NotifyModifying(this IElement self)
    {
        //_hasModified = true;
        MarkupRawCache.Remove(self);
        self.Parent?.NotifyModifying();
    }

    //internal static void SetParent(this IElement self, this IElement self)
    //{
    //    //_hasModified = true;
    //    MarkupRawCache.Remove(self);
    //    self.Parent?.NotifyModifying();
    //}
}

class ElementProxy
{
    public string Display
    {
        get { return _content.OuterMarkup; }
    }

    private Element _content;

    public ElementProxy(Element content)
    {
        _content = content;
    }
}