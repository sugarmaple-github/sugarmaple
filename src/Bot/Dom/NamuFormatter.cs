namespace Sugarmaple.TheSeed.Namumark;

using System;
using System.Diagnostics;
using System.Text;

public class NamuFormatter
{
    public static NamuFormatter Default { get; } = new();

    public string ToMarkup(IElement e)
    {
        var sb = new StringBuilder();
        Write_(e, sb);
        return sb.ToString();
    }

    public void Write_(IElement e, StringBuilder sb)
    {
        if (MarkupRawCache.TryGetValue(e, out var cached))
        {
            sb.Append(cached.AsSpan());
            return;
        }
        switch (e)
        {
            case Heading o:
                Write(o, sb);
                break;
            case Redirect o:
                Write(o, sb);
                break;
            case InternalLink o:
                Write(o, sb);
                break;
            case ExternalLink o:
                Write(o, sb);
                break;
            case CategoryLink o:
                Write(o, sb);
                break;
            case FileLink o:
                Write(o, sb);
                break;
            case BrMacro:
                sb.Append("[br]");
                break;
            case TableOfContents:
                sb.Append("[목차]");
                break;
            case Include o:
                Write(o, sb);
                break;
            case WikiBracket o:
                Write(o, sb);
                break;
            case Bold o:
                sb.Append("'''");
                WriteParent(o, sb);
                sb.Append("'''");
                break;
            case Italic o:
                sb.Append("''");
                WriteParent(o, sb);
                sb.Append("''");
                break;
            case TableRow o:
                Write(o, sb);
                break;
            case TableData o:
                Write(o, sb);
                break;
            case IParentElement o:
                WriteParent(o, sb);
                break;
            default:
                Debug.Fail("마크업이 정의되지 않은 DOM이 있습니다.");
                break;
        }
    }

    private void Write(TableData o, StringBuilder sb)
    {
        sb.Append(o.Attributes.OuterMarkup);
        WriteParent(o, sb);
    }

    private void Write(TableRow o, StringBuilder sb)
    {
        sb.Append("||").AppendJoin("||", o.Children.Select(o => o.OuterMarkup)).Append("||\n");
    }

    private void Write(CategoryLink o, StringBuilder sb)
    {
        sb.Append("[[")
            .Append(o.Reference).AppendIf(o.Blur, "#blur")
            .AppendIf(o.Display != null, $"|{o.Display}")
            .Append("]]");
    }

    private void Write(ExternalLink o, StringBuilder sb)
    {
        sb.Append("[[").Append(o.Reference);
        if (o.Children.Count > 0)
        {
            sb.Append('|');
            WriteParent(o, sb);
        }
        sb.Append("]]");
    }

    private void Write(WikiBracket o, StringBuilder sb)
    {
        sb.Append("{{{").Append(o.Tag);
        WriteParent(o, sb);
        sb.Append("}}}");
    }

    private void Write(Redirect o, StringBuilder sb)
    {
        sb.Append("#redirect ").Append(o.Reference);
        if (o.Anchor != null)
        {
            sb.Append('#').Append(o.Anchor);
        }
    }

    public void Write(FileLink o, StringBuilder sb)
    {
        sb.Append("[[").Append(o.Reference);
        if (o.Attributes.Count > 0)
            sb.Append('|').Append(o.Attributes.ToMarkup());
        sb.Append("]]");
    }


    public void Write(InternalLink o, StringBuilder sb)
    {
        sb.Append("[[").Append(o.Reference);
        if (!string.IsNullOrEmpty(o.Anchor))
        {
            sb.Append('#').Append(o.Anchor);
        }
        if (o.Children.Count > 0 && !(
            o.Children.Count == 1 && o.Children[0] is Text text && text.OuterMarkup == o.Reference))
        {
            sb.Append('|');
            sb.AppendJoin(null, o.Children.Select(o => o.OuterMarkup));
        }
        sb.Append("]]");
    }

    public void WriteParent(IParentElement list, StringBuilder sb)
    {
        foreach (var item in list.Children)
        {
            Write_(item, sb);
        }
    }

    public void Write(Include o, StringBuilder sb)
    {
        sb.Append("[include(")
            .Append(Escape(o.Reference))
            .AppendJoin(null, o.Arguments.Select(o => $", {o.Key}={Escape(o.Value)}"))
            .Append(")]");
        static string Escape(string str)
        {
            return str.Replace(",", @"\,");
        }
    }

    public void Write(Heading o, StringBuilder sb)
    {
        sb.Append('=', o.Level)
                .AppendIf(o.Folded, "#")
                .AppendIf(o.Level > 0, ' ');
        WriteParent((IParentElement)o, sb);
        sb.AppendIf(o.Level > 0, ' ')
        .AppendIf(o.Folded, "#")
        .Append('=', o.Level).Append('\n');
    }
}
