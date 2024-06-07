namespace Sugarmaple.TheSeed.Namumark;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[Obsolete]
internal record struct ParseContext_Old(string Origin, ParseState State, Match Match, NamumarkDocument Doc)
{
    public readonly StringSegment StringSegment => new(Origin, Match.Index, Match.Length);
    public readonly Group GetGroup(string key) => Match.Groups[key];
}
[Obsolete]

internal delegate NamumarkElement? ParseCallback_Old(in ParseContext_Old context);

internal class ParseContext
{
    public string Origin;
    public ParseState State;
    public int Index;
    public NamumarkDocument Doc;

    public ParseContext(string origin, ParseState state, int index, NamumarkDocument doc)
    {
        Origin = origin;
        State = state;
        Index = index;
        Doc = doc;
    }
}

internal record struct ProcessResult(NamumarkElement? Element, int NextPos);

internal delegate ProcessResult ParseCallback(in ParseContext context);

internal class RegexBlock
{
    private readonly Regex _regex;
    private readonly Dictionary<string, ParseCallback_Old> _callbackDict = new();

    public RegexBlock(params BracketInfo[] brackets)
    {
        _regex = new(string.Join('|', brackets.Select(o => $@"(?<{o.Name}>{o.Open})")));
        _callbackDict = new(brackets.Select(o => new KeyValuePair<string, ParseCallback_Old>(o.Name, o.Callback)));
    }
}

public enum FileAlign
{
    None,
    Left,
    Center,
    Right,
    Top,
    Bottom,
    Middle,
}

public enum FileTheme
{
    None,
    Light,
    Dark,
}

public class NamumarkParser
{
    public static readonly NamumarkParser Default;

    private readonly Dictionary<char, ParseCallback> _mainDict = new();

    private readonly Regex _mainRegex;
    private readonly Dictionary<string, ParseCallback_Old> _callbackDict = new();
    private readonly RegexBlock _square;
    private readonly Regex
        _categoryLinkRegex,
        _regularLinkRegex,
        _macroRegex,
        _macroArgRegex,
        _fileArgRegex,
        _fileWidthArgRegex,
        _fileHeightArgRegex,
        _fileAlignRegex,
        _fileThemeRegex;

    public const string
        Age = "age",
        Br = "br",
        Clearfix = "clearfix",
        Date = "date",
        Datetime = "datetime",
        Dday = "dday",
        Footnote = "footnote",
        Include = "include",
        Kakaotv = "kakaotv",
        Navertv = "navertv",
        Nicovideo = "nicovideo",
        Pagecount = "pagecount",
        Ruby = "ruby",
        Tableofcontents = "tableofcontents",
        Youtube = "youtube",
        FootnoteKor = "각주",
        TableofcontentsKor = "목차";

    public static readonly string[] Names;

    static NamumarkParser()
    {
        Names = new string[] {
            Age, Br, Clearfix, Date, Datetime, Dday, Footnote, Include, Kakaotv,
            Navertv, Nicovideo, Pagecount, Ruby, Tableofcontents, Youtube, FootnoteKor, TableofcontentsKor };
        Default = new();
    }

    public NamumarkParser()
    {
        _mainDict = new()
        {
            //{ '[', o => }
        };

        var heading = new BracketInfo("Heading", @"^(?<HeadingLevel>={1,6})(?<HeadingHidden>#?) (?<Content>[^\n]*) \'HeadingHidden'\'HeadingLevel'\n*?$", ParseHeading);

        _categoryLinkRegex = new(@"(?<Doc>[^\n]*?) *(?:#(?<Blur>blur)?[^\n]*?)?(?:\|(?<Args>.*?))?]]");
        _regularLinkRegex = new(@"(?<Doc>[^\n]*?) *(?:#(?<Anchor>[^\n]*?))?(?<Tail>]]|\|)");
        _macroRegex = new(@$"(?<Name>{string.Join('|', Names)})(?:\((?<Args>.*?)\))?\]");
        _macroArgRegex = new(@"(?<Main>[ \n]*[^ \n]*?)(?<Arg>,[^ \n]*?=[^ \n]*?)*");
        _fileArgRegex = new(@"(?<Doc>[^\n]*?) *(?:#[^\n]*?)(?:\|(?<Arg>.*?))?]]");
        _fileWidthArgRegex = new(@"(?<=^|&)width= *(?<Num>\d+)(?<Percent>%)?");
        _fileHeightArgRegex = new(@"(?<=^|&)height= *(?<Num>\d+)(?<Percent>%)?");
        _fileAlignRegex = new(@"(?<=^|&)align=(?<Value>[^&]*)", RegexOptions.Singleline | RegexOptions.RightToLeft);
        _fileThemeRegex = new(@"(?<=^|&)theme=(?<Value>[^&]*)", RegexOptions.Singleline | RegexOptions.RightToLeft);


        var squareBracket = new BracketInfo("Square", @"\[", (in ParseContext_Old o) =>
            {
                var nextIndex = o.Match.Index + 1;
                if (nextIndex == o.Origin.Length)
                    return null;
                var nextChar = o.Origin[nextIndex];
                if (nextChar == '[')
                {
                    nextIndex++;
                    var next = o.Origin.AsSpan(nextIndex);

                    if (next.StartsWith("분류:"))
                    {
                        var match = _categoryLinkRegex.Match(o.Origin, nextIndex);
                        if (!match.Success) return null;
                        return new NamumarkCategoryLinkElement(match.Groups["Doc"].Value, match.Groups["Blur"].Success, o.Doc, o.StringSegment);
                    }
                    else if (next.StartsWith("파일:"))
                    {
                        var match = _fileArgRegex.Match(o.Origin, nextIndex + 3);
                        if (!match.Success) return null;
                        var docGroup = match.Groups["Doc"];
                        var argGroup = match.Groups["Arg"];
                        return CreateFileLink(docGroup.Value, o.Origin, argGroup.Index, argGroup.Length, o.Doc, o.StringSegment);
                    }
                    else if (_regularLinkRegex.Match(o.Origin, nextIndex) is var match && match.Success)
                    {
                        if (match.Groups["Tail"].ValueSpan[0] == '|')
                            o.State.LinkStack.Push(new(match.Groups["Doc"].Value, match.Groups["Anchor"].Value, new(), o.Match.Index));
                        else return new NamumarkWikiLinkElement(match.Groups["Doc"].Value, match.Groups["Anchor"].Value,
                            new() { new NamumarkTextNode(o.Doc, new(o.Origin, o.Match.Index, match.Length + 2)) }, o.Doc, o.StringSegment);
                        //return new NamumarkWikiLinkElement();
                    }
                    else return null;
                }
                else if (_macroRegex.Match(o.Origin, nextIndex) is var match && match.Success)
                {
                    var name = match.Groups["Name"].ValueSpan;
                    var args = match.Groups["Args"].ValueSpan;
                    return ProcessMacro(o.Origin, o.Doc, o.Match.Index, name, args);
                }
                return null;
            }
            );


        var styleLinkClose = new BracketInfo("LinkClose", @"\]\]", (in ParseContext_Old o) =>
        {
            if (o.State.LinkStack.TryPeek(out var c))
            {
                return new NamumarkWikiLinkElement(c.TargetDocument, c.Anchor, c.Children, o.Doc, new(o.Origin, c.Index, o.Match.Index + o.Match.Length - c.Index));
            }
            return null;
        });

        var hr = new BracketInfo("HorizontalLine", @"^-{4,9}$",
            (in ParseContext_Old o) =>
                new NamumarkHorizontalLineElement(o.Doc, o.StringSegment));

        var tableStartOrEnd = new BracketInfo("TableStartOrEnd", @"^\|\|$",
            (in ParseContext_Old o) =>
            {
                ref var state = ref o.State.TableState;
                if (state is TableState.None or TableState.CaptionStart)
                    state = TableState.RowStart;
                else if (state is TableState.RowStart)
                {
                    state = TableState.None;
                }
                return null;
            });
        var tableStart = new BracketInfo("TableStart", @"^\|",
            (in ParseContext_Old o) =>
               { o.State.TableState = TableState.CaptionStart; return null; });


        var array = new[] { heading, squareBracket, hr };
        _mainRegex = new(string.Join('|', array.Select(o => $@"(?<{o.Name}>{o.Open})")));
        _callbackDict = new(array.Select(o => new KeyValuePair<string, ParseCallback_Old>(o.Name, o.Callback)));
    }

    public NamumarkDocument Parse(string namumark)
    {
        var paragraphs = new List<NamumarkParagraph>();
        var doc = new NamumarkDocument(paragraphs, namumark);
        Parse(namumark, 0, namumark.Length, paragraphs, doc);
        return doc;
    }

    public void Parse(string namumark, int index, int length, List<NamumarkParagraph> paragraphs, NamumarkDocument doc)
    {
        var heading = new NamumarkHeadingElement(0, false, new(), doc, default);

        var clauses = new List<INamumarkClause>();
        var paragraphStart = 0;
        var contentStart = 0;

        var context = new ParseContext(namumark, new(), index, doc);
        while (context.Index < length)
        {
            var result = _mainDict[namumark[index]](context);
            var element = result.Element;
            if (element is NamumarkHeadingElement newHeading)
            {
                var contentRaw = namumark.ToSegmentFromTo(contentStart, context.Index);
                var content = new NamumarkParagraphContentElement(clauses, doc, contentRaw);
                clauses = new();

                var paragraphRaw = namumark.ToSegmentFromTo(paragraphStart, context.Index);
                var paragraph = new NamumarkParagraph(heading, content, doc, paragraphRaw);
                heading = newHeading;
                paragraphStart = contentRaw.End;
                contentStart = result.NextPos;
                paragraphs.Add(paragraph);
            }
            else if (element is INamumarkClause clause)
            {
                clauses.Add(clause);
            }
            context.Index = result.NextPos;
        }
    }

    private NamumarkFileLinkElement CreateFileLink(string refDocument, string source, int argStart, int argLength, NamumarkDocument doc, in StringSegment segment)
    {
        var widthMatch = _fileWidthArgRegex.Match(source, argStart, argLength);
        var widthNumCapture = widthMatch.Groups["Num"].Captures[^1];
        var widthPercentCapture = widthMatch.Groups["Percent"].Captures[^1];
        var widthValue = new PercentOrNum(int.Parse(widthNumCapture.ValueSpan), widthPercentCapture.Length > 0);

        var heightMatch = _fileWidthArgRegex.Match(source, argStart, argLength);
        var heightNumCapture = heightMatch.Groups["Num"].Captures[^1];
        var heightPercentCapture = heightMatch.Groups["Percent"].Captures[^1];
        var heightValue = new PercentOrNum(int.Parse(heightNumCapture.ValueSpan), heightPercentCapture.Length > 0);

        var alignMatch = _fileAlignRegex.Match(source, argStart, argLength);
        var alignValueGroup = alignMatch.Groups["Value"].ValueSpan;
        var alignValue = alignValueGroup switch
        {
            "left" => FileAlign.Left,
            "center" => FileAlign.Center,
            "right" => FileAlign.Right,
            "top" => FileAlign.Top,
            "bottom" => FileAlign.Bottom,
            _ => FileAlign.None,
        };

        var themeMatch = _fileThemeRegex.Match(source, argStart, argLength);
        var themeValueGroup = themeMatch.Groups["Value"].ValueSpan;
        var themeValue = themeValueGroup switch
        {
            "light" => FileTheme.Light,
            "dark" => FileTheme.Dark,
            _ => FileTheme.None,
        };
        return new NamumarkFileLinkElement(refDocument, widthValue, heightValue, alignValue, themeValue, doc, segment);
    }

    /*public List<INamumarkChildNode> ParseChildren(string namumark, int index)
    {
        var elements = new List<INamumarkChildNode>();
        int nextIndex = index;
        var paragraphStart = 0;
        var contentStart = 0;

        var context = new ParseContext(namumark, new(), Match.Empty, doc);
        Match match;
        do
        {
            context.Match = match = _mainRegex.Match(namumark, nextIndex, length);
            var groups = match.Groups;
            var matchEnd = match.Index + match.Length;
            foreach (Group group in groups)
            {
                if (group.Success)
                {
                    var element = _callbackDict[group.Name](context);
                    if (element is NamumarkHeadingElement newHeading)
                    {
                        var contentRaw = new StringSegment(namumark, contentStart, matchEnd - contentStart);
                        var content = new NamumarkHeadingContentElement(elements, doc, contentRaw);
                        var paragraphRaw = new StringSegment(namumark, paragraphStart, contentRaw.End - paragraphStart);
                        var paragraph = new NamumarkParagraph(heading, content, doc, paragraphRaw);
                        heading = newHeading;
                        paragraphStart = contentRaw.End;
                        contentStart = matchEnd;
                        paragraphs.Add(paragraph);
                    }
                    else
                    {
                        elements.Add(element);
                    }
                }
            }
        } while (match.Success);
        return elements;
    }*/

    private NamumarkHeadingElement ParseHeading(in ParseContext_Old context)
    {
        (string origin, ParseState state, Match match, NamumarkDocument doc) = context;
        var segment = new StringSegment(origin, match.Index, match.Length);
        var groups = match.Groups;
        var level = groups["HeadingLevel"].ValueSpan.Length;
        var hidden = groups["HeadingHidden"].Success;
        var children = ParseLine(origin, match.Index, match.Length);
        var element = new NamumarkHeadingElement(level, hidden, children, doc, in segment);
        return element;
    }

    private NamumarkElement ProcessMacro(string origin, NamumarkDocument doc, int start, ReadOnlySpan<char> name, ReadOnlySpan<char> args)
    {
        if (name.Equals(Include, StringComparison.InvariantCultureIgnoreCase))
        {
            var match = _macroArgRegex.Match(origin);
            var argDict = new Dictionary<string, string>();
            var nameCaptures = match.Groups["Name"].Captures;
            var valueCaptures = match.Groups["Value"].Captures;
            for (int i = 0; i < nameCaptures.Count; i++)
            {
                argDict[nameCaptures[i].Value] = valueCaptures[i].Value;
            }
            return new NamumarkIncludeMacroElement(match.Groups["Main"].Value, argDict, doc, new(origin, start, match.Index + match.Length));
        }
        return null; //need to make other case;
    }

    private List<INamumarkClause> ParseLine(string origin, int index, int length)
    {
        throw new NotImplementedException();
    }
}
