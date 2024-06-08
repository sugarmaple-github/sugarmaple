namespace Sugarmaple.TheSeed.Namumark;

using System.Text;

public class Document : FixedClauseParent<Document, Paragraph>, IDisposable
{
    public string BaseUri { get; internal set; }

    protected override IParentElement? ParentInner { get => null; set => throw new InvalidOperationException("Document Element can't use Parent"); }

    public void Dispose()
    {
        foreach (var item in QuerySelectorAll<Element>("*"))
        {
            MarkupRawCache.Remove(item);
        }
    }

    public IEnumerable<Clause> QuerySelectorAll(string selectors) => QuerySelectorAll<Clause>(selectors);
    public IEnumerable<T> QuerySelectorAll<T>(string selectors)
    {
        if (selectors != "*") throw new NotImplementedException("only available when selectors is *");
        var ret = new List<T>();
        EnumerateNode(this, ret);
        return ret;

        static void EnumerateNode(IParentElement parent, List<T> ret)
        {
            var children = parent.Children.ToArray();
            foreach (var child in children)
            {
                if (child is T asT)
                {
                    ret.Add(asT);
                }
                if (child is IParentElement childAsParent)
                {
                    EnumerateNode(childAsParent, ret);
                }
            }
        }
    }
}

public class Paragraph : FixedElement<Document, Paragraph, IHeadingLevel>
{
    public Heading Heading => (Heading)Children[0];
    public HeadingContent Content => (HeadingContent)Children[1];

    public Paragraph()
    {
        this.AppendChild(new Heading());
        this.AppendChild(new HeadingContent());
    }
}

public interface IHeadingLevel : IChildElement<Paragraph, IHeadingLevel>
{

}

public class Heading : ExclusiveParentClause<Paragraph, IHeadingLevel>, IHeadingLevel
{
    public int Level { get; set; }
    public bool Folded { get; set; }
}

public class HeadingContent : ExclusiveParentClause<Paragraph, IHeadingLevel>, IHeadingLevel
{
}

internal static class MarkupFormatterExtensions
{
    public static StringBuilder AppendIf(
        this StringBuilder @this,
        bool condition,
        char value)
    {
        if (@this == null)
        {
            throw new ArgumentNullException("this");
        }

        if (condition)
        {
            @this.Append(value);
        }

        return @this;
    }

    public static StringBuilder AppendIf(
        this StringBuilder @this,
        bool condition,
        string str)
    {
        if (@this == null)
        {
            throw new ArgumentNullException("this");
        }

        if (condition)
        {
            @this.Append(str);
        }

        return @this;
    }
}