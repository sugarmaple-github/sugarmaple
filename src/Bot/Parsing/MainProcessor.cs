namespace Sugarmaple.TheSeed.Namumark.Parsing;
using System.Text.RegularExpressions;

//Processor들은 파싱에 필요한 정보들만 추출해냅니다.

internal class HeadingProcessor : Processor
{
    public override string Open => "";

    protected override ASTNode ProcessInternal(Progress state)
    {
        if (state.MainTape.TryMatch(
            new Regex(@"\G(?<HeadingLevel>={1,6})(?<HeadingHidden>#?) (?<Content>[^\n]*) \'HeadingHidden'\'HeadingLevel'\r?$\n?",
            RegexOptions.Singleline | RegexOptions.Multiline), out var match))
        {
            var contentGroup = match.Groups["Content"];
            var level = match.Groups["HeadingLevel"].ToToken();
            var folded = match.Groups["HeadingHidden"].ToToken();

            var newTape = ToTape(contentGroup);
            var list = state.Creator.GetFreeElementList(newTape);
            return state.MainTape.ToASTNode(ASTNodeType.Heading, list, level, folded);
        }
        return default;
    }
}

internal class BraceProcessor : Processor
{
    public override string Open => "{{{";

    protected override ASTNode ProcessInternal(Progress state)
    {
        var level = 1;
        string? found;
        var innerTape = state.MainTape.MakeChild();
        while (true)
        {
            found = innerTape.ConsumeUntilFind("{{{", "}}}");
            if (found == "{{{")
            {
                level++;
            }
            else if (found == "}}}")
            {
                if (--level == 0)
                    break;
            }
            else if (found == null)
                return default;

            innerTape.Progress(3);
        }

        var tag = innerTape.MakeChildInside();
        if (tag.ConsumeIf("#!wiki ") && tag.ConsumeTo('\n') ||
            tag.ConsumeIf("#!folding ") && tag.ConsumeTo('\n') ||
            tag.Regex("\\G[+-][1-5][ \\n]") ||
            tag.Regex("\\G#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})(,#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}))?[ \\n]") ||
            tag.Regex("\\G#white[ \\n]"))
        {
            var innerMarkup = tag.MakeSibling();
            if (state.Creator.TryGetFreeElementList(innerMarkup, default, out var markup))
            {
                innerTape.UpdateToParent();
                state.MainTape.Progress(3);
                return state.MainTape.ToASTNode(ASTNodeType.MarkupBracket,
                    markup, tag.ToASTNode());
            }
        }

        return state.MainTape.ToASTNode(ASTNodeType.Literal,
            innerTape.ToASTNode());
    }
}

internal class MacroProcessor : Processor
{
    public override string Open => "[";

    protected override ASTNode ProcessInternal(Progress state)
    {
        if (state.MainTape.TryMatch(new Regex(@"\G(br|include|tableofcontents|목차)(?:\((.*?)\))?]", RegexOptions.IgnoreCase), out var match))
        {
            var tagName = match.Groups[1];
            var argGroup = match.Groups[2];
            return state.MainTape.ToASTNode(ASTNodeType.Macro,
                new ASTNode(ASTNodeType.None, tagName.Index, tagName.Length),
                new ASTNode(ASTNodeType.None, argGroup.Index, argGroup.Length)
                );
        }
        return default;
    }

    private ASTNode CreateMacro(ReadOnlySpan<char> tagName, StringTape argument)
    {
        if (Equals(tagName, "include"))
        {
            return CreateInclude(argument);
        }
        else if (Equals(tagName, "목차") || Equals(tagName, "tableofcontents"))
        {
            return argument.Parent.ToASTNode(ASTNodeType.TableOfContents);
        }
        else if (tagName.Equals("br", StringComparison.OrdinalIgnoreCase))
        {
            return argument.Parent.ToASTNode(ASTNodeType.Br);
        }
        throw new NotImplementedException();

        static bool Equals(ReadOnlySpan<char> tagName, string name)
        {
            return tagName.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

    }

    private static ASTNode CreateInclude(StringTape tape)
    {
        var reference = tape.GetStringToken(',');
        reference = reference.Trim();
        var args = new Dictionary<string, string>();
        while (!tape.IsEnd)
        {
            tape.Trim();
            var name = tape.GetStringToken('=');
            tape.Trim();
            var value = tape.GetStringToken(',');
            args[name] = value;
        }
        var ret = new Include(reference, args);
        return new ASTNode(ret);
    }
}

internal class LinkProcessor : Processor
{
    public override string Open => "[[";

    protected override ASTNode ProcessInternal(Progress state)
    {
        var tape = state.MainTape;
        if (tape.TryMatch(NamuRegex.Link, out var match))
        {
            var reference = ToTape(match.Groups["ref"], tape);
            var isNonParent = match.Groups["end"].ValueSpan[0] == ']';
            if (isNonParent)
                return CreateLink(tape, reference);
            if (state.Creator.TryCloserParse(tape.MakeChild(), new("]]", true), out var list))
                return CreateLink(tape, reference, list);
        }
        return default;
    }

    private static ASTNode CreateLink(StringTape wholeTape, StringTape refValue)
    {
        return CreateLink(wholeTape, refValue, new());
    }

    private static ASTNode CreateLink(StringTape wholeTape, StringTape refAndAnchor, ASTNode children)
    {
        ASTNode ret;
        if (refAndAnchor.Search("http:") || refAndAnchor.Search("https:"))
        {
            ret = wholeTape.ToASTNode(ASTNodeType.ExternalLink,
                children,
                new(0, refAndAnchor.Index, refAndAnchor.EndIndex - refAndAnchor.Index)
            );
        }
        else if (refAndAnchor.Search("파일:"))
        {
            refAndAnchor.ConsumeWhile('#');
            ret = wholeTape.ToASTNode(ASTNodeType.FileLink, refAndAnchor.ToASTNode(),
                CreateFileLink(new(refAndAnchor.Raw, children.Index, children.Index + children.Length))
            );
        }
        else if (refAndAnchor.Search("분류:"))
        {
            refAndAnchor.ConsumeWhile('#');
            ret = wholeTape.ToASTNode(ASTNodeType.Category,
                refAndAnchor.ToASTNode(),
                new(0, refAndAnchor.Index + 1, refAndAnchor.EndIndex - refAndAnchor.Index + 1),
                new(0, children.Index, children.Length)
            );
        }
        else
        {
            refAndAnchor.ConsumeWhile('#');
            ret = wholeTape.ToASTNode(ASTNodeType.InternalLink,
                children,
                refAndAnchor.ToASTNode(),
                new(0, refAndAnchor.Index + 1, refAndAnchor.EndIndex - (refAndAnchor.Index + 1))
            );
        }
        return ret;
    }

    private static ASTNode CreateFileLink(StringTape tape)
    {
        var list = new List<ASTNode>();
        var widthRegex = new Regex(@"(?<=\G|&)(width)= *(?<Num>\d+%?)");
        if (tape.TryMatch_(widthRegex, out var match))
            list.Add(new(0, match.Index, match.Length) {
                match.Groups[1].ToToken(),
                match.Groups[2].ToToken()
            });

        var heightRegex = new Regex(@"(?<=\G|&)(height)= *(?<Num>\d+%?)");
        if (tape.TryMatch_(heightRegex, out match))
            list.Add(new(0, match.Index, match.Length) {
                match.Groups[1].ToToken(),
                match.Groups[2].ToToken()
            });

        var alignRegex = new Regex(@"(?<=\G|&)(align)=(?<value>left|center|middle|right|top|bottom)");
        if (tape.TryMatch_(alignRegex, out match))
        {
            list.Add(new(0, match.Index, match.Length) {
                match.Groups[1].ToToken(),
                match.Groups[2].ToToken()
            });
        }

        var themeRegex = new Regex(@"(?<=\G|&)(theme)=(?<value>light|dark)");
        if (tape.TryMatch_(themeRegex, out match))
        {
            list.Add(new(0, match.Index, match.Length) {
                match.Groups[1].ToToken(),
                match.Groups[2].ToToken()
            });
        }

        return tape.ToASTNode(0, list);
    }
}