namespace Sugarmaple.TheSeed.Namumark.Parsing;

using Sugarmaple.TheSeed.Namumark;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

internal class StringTape
{
    public readonly string Raw;
    public int Start;
    public int Index;
    public readonly int EndIndex;
    public readonly StringTape? Parent;

    public StringTape(string raw) : this(raw, 0, raw.Length) { }

    public StringTape(string raw, int index, int endIndex)
    {
        Raw = raw;
        Start = Index = index;
        EndIndex = endIndex;
    }

    public StringTape(string raw, int index, int endIndex, StringTape parent)
    {
        Raw = raw;
        Start = Index = index;
        EndIndex = endIndex;
        Parent = parent;
    }

    public void Check()
    {
        Start = Index;
    }

    public StringTape CheckAndUse()
    {
        return new(Raw, Index, EndIndex);
    }

    public bool IsEnd => Index >= EndIndex;

    public bool ConsumeIf(char c)
    {
        var cond = Raw[Index] == c;
        if (cond)
            Index++;
        return cond;
    }
    public bool ConsumeIf(string c)
    {
        var cond = Search(c);
        if (cond)
            Index += c.Length;
        return cond;
    }

    public bool Search(string str) => Search(Index, str);
    public bool Search(char c) => Search(Index, c);
    public bool Search(int index, char c) => 0 <= index && index < Raw.Length && Raw[index] == c;
    public bool Search(int index, string str) => Raw.AsSpan(index).StartsWith(str);
    public bool ConsumeTo(char c)
    {
        var index = Index;
        while (index < EndIndex)
            if (Raw[index++] == c)
            {
                Index = index;
                return true;
            }
        return false;
    }
    public string? ConsumeUntilFind(params string[] pool)
    {
        while (Index < EndIndex)
        {
            foreach (var o in pool)
                if (Search(o))
                    return o;
            Index++;
        }
        return null;
    }

    public bool TryMatch(Regex regex, out Match match)
    {
        match = regex.Match(Raw, Index, EndIndex - Index);
        Index += match.Length;
        return match.Success;
    }

    public bool TryMatch_(Regex regex, out Match match)
    {
        match = regex.Match(Raw, Index, EndIndex - Index);
        return match.Success;
    }

    public bool IsNewLine() => Index == 0 || SearchRelatively(-1, '\n');

    public bool SearchRelatively(int pos, char c) => Search(Index + pos, c);

    public StringSegment ToSegmentUntil(int newIndex)
    {
        return Raw.ToSegmentFromTo(Index, newIndex);
    }

    public bool ConsumeWhile(char c)
    {
        while (Index < EndIndex)
        {
            if (Search(c))
                return true;
            Index++;
        }
        return false;
    }

    public string GetStringToken(char end)
    {
        var sb = new StringBuilder();
        while (!IsEnd)
        {
            var curChar = Raw[Index];
            Index++;
            if (curChar == end)
                break;
            if (curChar == '\\')
            {
                if (++Index < Raw.Length)
                    sb.Append(Raw[Index]);
                continue;
            }
            sb.Append(curChar);
        }
        return sb.ToString();
    }

    public void Trim()
    {
        while (Index < Raw.Length && Raw[Index] == ' ')
            Index++;
    }

    public override string ToString()
    {
        return Raw[Start..Index];
    }

    public string Front => Raw[Index..EndIndex];

    public void UpdateToParent()
    {
        Parent.Index = Math.Max(Parent.Index, Index);
    }

    internal void Progress(int v)
    {
        Index += v;
    }

    internal void ProgressTo(StringTape tape)
    {
        Index = tape.Index;
    }

    internal StringTape MakeSibling()
    {
        return new(Raw, Index, EndIndex, Parent);
    }

    internal StringTape MakeChild()
    {
        return new(Raw, Index, EndIndex, this);
    }

    internal StringTape MakeChildFromStart()
    {
        return new(Raw, Start, EndIndex, this) { Index = Index };
    }

    internal StringTape MakeChildInside()
    {
        return new(Raw, Start, Index, this);
    }
}


internal static class StringTapeExtensions
{
    public static bool Regex(this StringTape tape, [StringSyntax(StringSyntaxAttribute.Regex)] string regex)
    {
        var re = new Regex(regex);
        var match = re.Match(tape.Raw, tape.Index, tape.EndIndex - tape.Index);
        return match.Success;
    }

    public static ASTNode ToASTNode(this StringTape tape, params ASTNode[] children)
    {
        return tape.ToASTNode(ASTNodeType.None, children.ToList());
    }

    public static ASTNode ToASTNode(this StringTape tape, ASTNodeType type, params ASTNode[] children)
    {
        return tape.ToASTNode(type, children.ToList());
    }

    public static ASTNode ToASTNode(this StringTape tape, ASTNodeType type, List<ASTNode> children)
    {
        if (tape.Parent != null)
            tape.UpdateToParent();
        return new(type, tape.Start, tape.Index - tape.Start, children);
    }
}