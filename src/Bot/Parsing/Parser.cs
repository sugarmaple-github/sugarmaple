namespace Sugarmaple.TheSeed.Namumark.Parsing;

using System.CommandLine;

internal class Parser
{
    public string _raw;

    public Element Parse(ASTNode token)
    {
        Element elem = token.Type switch
        {
            ASTNodeType.Document => ParseDocument(token),
            ASTNodeType.Literal => ParseLiteral(token),
            ASTNodeType.InternalLink => ParseInternalLink(token),
            ASTNodeType.ExternalLink => ParseExternalLink(token),
            ASTNodeType.Category => ParseCategoryLink(token),
            ASTNodeType.FileLink => ParseFileLink(token),
            ASTNodeType.TableOfContents => new TableOfContents(),
            ASTNodeType.Br => new BrMacro(),
            ASTNodeType.Macro => ParseMacro(token),
            ASTNodeType.Text => new Text(new StringSegment(_raw, token.Index, token.Length)),
            ASTNodeType.Table => ParseTable(token),
            ASTNodeType.MarkupBracket => ParseMarkupBracket(token),
            ASTNodeType.Bold => new Bold { Children = ParseChildAsElementList(token, 0) },
            ASTNodeType.Italic => new Italic { Children = ParseChildAsElementList(token, 0) },
            ASTNodeType.Redirect => new Redirect
            {
                Reference = ParseChildAsString(token, 0),
                Anchor = ParseChildAsNullableString(token, 1)
            },
            _ => throw new NotImplementedException(),
        };
        MarkupRawCache.Add(elem, _raw.ToSegment(token.Index, token.Length));
        return elem;
    }

    private CategoryLink ParseCategoryLink(ASTNode token) => new()
    {
        Reference = ParseChildAsString(token, 0),
        Blur = ParseChildAsSpan(token, 1).Length > 0,
        Display = ParseChildAsString(token, 2),
    };

    private ExternalLink ParseExternalLink(ASTNode token) => new()
    {
        Children = ParseChildAsElementList(token, 0),
        Reference = ParseChildAsString(token, 1)!,
    };

    private InternalLink ParseInternalLink(ASTNode token) => new()
    {
        Children = ParseChildAsElementList(token, 0),
        Reference = ParseChildAsString(token, 1)!,
        Anchor = ParseChildAsString(token, 2),
    };

    private Table ParseTable(ASTNode node)
    {
        var ret = new Table();
        foreach (var trNode in node.Children)
        {
            var tr = new TableRow();
            foreach (var tdNode in trNode.Children)
            {
                var td = ParseTableData(tdNode);
                tr.AppendChild(td);
            }
            ret.AppendChild(tr);
        }
        return ret;
    }

    private KeyValuePair<string, string> ParseTDAttribute(ASTNode node)
    {
        return new(ParseChildAsString(node, 0), ParseChildAsString(node, 1));
    }

    private TableAttribute ParseTDAttributes(ASTNode node)
    {
        var ret = new TableAttribute();
        foreach (var o in node.Children.Skip(1))
        {
            (var key, var value) = ParseTDAttribute(o);
            ret.Add(key, value);
        }
        var pipeColSpan = (node.Children[0].Length / 2).ToString();
        ret.Add("-", pipeColSpan);
        MarkupRawCache.Add(ret, _raw.ToSegment(node.Index, node.Length));
        return ret;
    }

    private TableData ParseTableData(ASTNode node)
    {
        var attr = ParseTDAttributes(node.Children[1]);
        var td = new TableData { Attributes = attr };
        foreach (var o in ParseElementList(node.Children[0]))
            td.AppendChild(o);
        return td;
    }

    private Literal ParseLiteral(ASTNode token)
    {
        var ret = new Literal(ParseChildAsString(token, 0));
        return ret;
    }

    private WikiBracket ParseMarkupBracket(ASTNode token)
        => new()
        {
            Tag = ParseChildAsString(token, 0),
            Children = ParseChildAsElementList(token, 1)
        };

    public Document ParseDocument(ASTNode node)
    {
        var ret = new Document();
        foreach (var child in node.Children[0].Children)
            ret.AppendChild(ParseParagraph(child));
        return ret;
    }

    private Heading ParseHeading(ASTNode node)
    {
        Heading h = (node.Children != null && node.Children.Count > 2) ? new()
        {
            Children = ParseChildAsElementList(node, 0),
            Level = ParseChildAsSpan(node, 1).Length,
            Folded = ParseChildAsSpan(node, 2).Length > 0,
        } : new() { Level = 0 };
        MarkupRawCache.Add(h, _raw.ToSegment(node.Index, node.Length));
        return h;
    }

    private Paragraph ParseParagraph(ASTNode node)
    {
        var ret = new Paragraph();
        ret.ReplaceChild(ParseHeading(node.Children[0]), ret.Heading);
        ret.ReplaceChild(ParseContent(node.Children[1]), ret.Content);
        MarkupRawCache.Add(ret, _raw.ToSegment(node.Index, node.Length));
        return ret;
    }

    private HeadingContent ParseContent(ASTNode node)
    {
        var ret = new HeadingContent { Children = ParseElementList(node) };
        return ret;
    }

    private ElementList<Clause> ParseElementList(ASTNode node)
    {
        var list = new List<Clause>();
        var last = node.Index;
        foreach (var child in node.Children)
        {
            if (last < child.Index)
                list.Add(new Text(_raw[last..child.Index]));
            list.Add((Clause)Parse(child));
            last = child.Index + child.Length;
        }
        if (last < node.Index + node.Length)
            list.Add(new Text(_raw[last..(node.Index + node.Length)]));
        return new(list);
    }

    private ElementList<Clause> ParseChildAsElementList(ASTNode node, int index)
    {
        return ParseElementList(node.Children[index]);
    }

    private Macro ParseMacro(ASTNode node)
    {
        var tagName = ParseChildAsSpan(node, 0);
        Span<char> lowerTagName = stackalloc char[tagName.Length];
        var argument = ParseChildAsString(node, 1);

        tagName.ToLower(lowerTagName, System.Globalization.CultureInfo.CurrentCulture);
        Macro ret = lowerTagName switch
        {
            "include" => Create<Include>(),
            "목차" or "tableofcontents" => Create<Include>(),
            "br" => Create<BrMacro>(),
            _ => throw new NotImplementedException(),
        };
        return ret;

        T Create<T>() where T : Macro, new()
        {
            return new T
            {
                Argument = argument
            };
        }

        //var ret = new Include { Reference = ParseChildAsString(node, 0) };
        //for (int i = 1; i < node.Children.Count; i += 2)
        //    ret.Arguments[ParseChildAsString(node, i)] = ParseChildAsString(node, i + 1);
        //return ret;
    }

    private FileLink ParseFileLink(ASTNode node)
    {
        var ret = new FileLink
        {
            Reference = ParseChildAsString(node, 0),
            Attributes = ParseChildAsFileAttributes(node, 1),
            //Width = ParseChildAsString(node, 1),
            //Height = ParseChildAsString(node, 2),
            //Align = ToAlign(ParseChildAsSpan(node, 3)),
            //Theme = ToTheme(ParseChildAsSpan(node, 4)),
        };
        return ret;
    }

    private FileAttributes ParseChildAsFileAttributes(ASTNode node, int v)
    {
        var ret = new FileAttributes();
        var curNode = node.Children[v];
        foreach (var item in curNode.Children)
        {
            ret.Add(ParseChildAsString(item, 0), ParseChildAsString(item, 1));
        }
        return ret;
    }

    public string? ParseAsString(ASTNode node)
    {
        if (node.Length < 0) return null;
        return _raw.Substring(node.Index, node.Length);
    }

    public string? ParseChildAsNullableString(ASTNode node, int index)
    {
        if (node.Children.Count <= index)
            return null;
        return ParseAsString(node.Children[index]);
    }

    public string? ParseChildAsString(ASTNode node, int start)
    {
        return ParseAsString(node.Children[start]);
    }

    public ReadOnlySpan<char> ParseChildAsSpan(ASTNode node, int start)
    {
        return _raw.AsSpan(node.Children[start].Index, node.Children[start].Length);
    }

    public int ParseAsInt(ASTNode node)
    {
        return int.Parse(_raw.AsSpan(node.Index, node.Length));
    }

    private static FileAlign ToAlign(ReadOnlySpan<char> value)
    {
        return value switch
        {
            "left" => FileAlign.Left,
            "center" => FileAlign.Center,
            "right" => FileAlign.Right,
            "top" => FileAlign.Top,
            "bottom" => FileAlign.Bottom,
            _ => FileAlign.None,
        };
    }

    private static FileTheme ToTheme(ReadOnlySpan<char> value)
    {
        return value switch
        {
            "light" => FileTheme.Light,
            "dark" => FileTheme.Dark,
            _ => FileTheme.None,
        };
    }
}


public enum ASTNodeType
{
    None,
    Document,
    Redirect,
    Table,
    InternalLink,
    FileLink,
    Heading,
    Macro,
    Tr, //n
    Td, //n
    ExternalLink,
    Content,
    Paragraph,
    Literal,
    TableOfContents,
    Br,
    Text,
    TableAlign,
    ColSpan,
    RowSpan,
    Align,
    Color,
    Bgcolor,
    Width,
    Height,
    Colcolor,
    Colbgcolor,
    Tablewidth,
    Tablealign,
    Tablebgcolor,
    Tablebordercolor,
    Category,
    Bold,
    Italic,
    MarkupBracket,
    Quote,
}