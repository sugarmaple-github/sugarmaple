namespace Sugarmaple.TheSeed.Namumark.Parsing;

using Newtonsoft.Json.Linq;
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


internal class ASTNodeBuilder
{
    public readonly string Raw;
    public int Start = 0;
    public int Index { get; set; }
    public readonly int EndIndex;
    public List<ASTNode> Children { get; } = new();

    private readonly FuncExecutor<ASTNodeBuilder> _movedNext = new();
    public event Func<ASTNodeBuilder, bool> MovedNext
    {
        add => _movedNext.Add(value);
        remove => _movedNext.Remove(value);
    }
    private readonly FuncExecutor<ASTNodeBuilder> _movingNext = new();
    public event Func<ASTNodeBuilder, bool> MovingNext
    {
        add => _movingNext.Add(value);
        remove => _movingNext.Remove(value);
    }
    public event Action<ASTNodeBuilder>? Ended;

    public ASTNodeBuilder(string raw) : this(raw, -1, raw.Length) { }

    public ASTNodeBuilder(string raw, int index, int endIndex)
    {
        Raw = raw;
        Start = index;
        Index = index - 1;
        EndIndex = endIndex;
    }

    public ASTNodeBuilder(string raw, int start, int index, int endIndex)
    {
        Raw = raw;
        Start = start;
        Index = index - 1;
        EndIndex = endIndex;
    }

    public char Current => Raw[Index];

    public bool MoveNext()
    {
        if (!_movingNext.Invoke(this))
            return false;

        if (Index >= EndIndex)
        {
            Ended?.Invoke(this);
            return false;
        }
        Index++;
        Index = Math.Min(Index, EndIndex);
        return _movedNext.Invoke(this);
    }

    public bool MoveNextForStart(int size = 1)
    {
        if (!_movingNext.Invoke(this))
            return false;

        if (Start >= EndIndex)
        {
            Ended?.Invoke(this);
            return false;
        }
        Start += size;
        Start = Math.Min(Start, EndIndex);
        Index = Math.Max(Start, Index);
        var ret = _movedNext.Invoke(this);
        Start = Index;
        return ret;
    }

    internal char? Next()
    {
        return EndIndex < Index + 1 ? Raw[Index + 1] : null;
    }

    public override string ToString()
    {
        return $"current: {Current}, contains: {Raw[Start..Index]}, front: {Front}";
    }

    internal void Add(ASTNode node)
    {
        Index = Math.Max(node.End - 1, Index);
        Children.Add(node);
    }

    public string Front => Raw[Index..EndIndex];
}


internal static class StringTapeExtensions
{
    public static bool StartsWith(this ASTNodeBuilder self, string value)
    {
        return self.Raw.AsSpan(self.Index).StartsWith(value);
    }


    /// <summary>
    /// Index 위치에 value 값이 있다면 true를 반환하고 value의 길이만큼 진행합니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool Consume(this ASTNodeBuilder self, string value)
    {
        if (self.Raw.AsSpan(self.Index).StartsWith(value))
        {
            self.Index += value.Length;
            return true;
        }
        return false;
    }

    public static bool Consume(this ASTNodeBuilder self, char value)
    {
        if (self.Raw.AsSpan(self.Index)[0] == value)
        {
            self.Index++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 개행 혹은 문자열 끝 이전에 value 값이 있다면 value의 길이만큼 진행합니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ConsumeBeforeNewline(this ASTNodeBuilder self, string value)
    {
        var newIndex = self.Index + value.Length;
        if (self.Raw.AsSpan(self.Index).StartsWith(value) && (newIndex >= self.EndIndex || self.Raw[newIndex] == '\n'))
        {
            self.Index = newIndex;
            self.Consume('\n');
            return true;
        }
        return false;
    }

    public static ASTNodeBuilder Branch(this ASTNodeBuilder self)
    {
        return new(self.Raw, self.Index, self.EndIndex);
    }

    public static bool Search(this ASTNodeBuilder self, string str) => self.Search(self.Index, str);
    public static bool Search(this ASTNodeBuilder self, char c) => self.Search(self.Index, c);
    public static bool Search(this ASTNodeBuilder self, int index, char c) => 0 <= index && index < self.Raw.Length && self.Raw[index] == c;
    public static bool Search(this ASTNodeBuilder self, int index, string str) => self.Raw.AsSpan(index).StartsWith(str);



    /// <summary>
    /// 해당 문자를 찾을 때까지 이동합니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="c"></param>
    /// <returns>찾지 못하면 false를 반환합니다.</returns>
    public static bool ConsumeTo(this ASTNodeBuilder self, char c)
    {
        var index = self.Index;
        while (index < self.EndIndex)
            if (self.Raw[index++] == c)
            {
                self.Index = index;
                return true;
            }
        return false;
    }

    public static ASTNode ToASTNode(this ASTNodeBuilder tape, params ASTNode[] children)
    {
        return tape.ToASTNode(ASTNodeType.None, children.ToList());
    }

    /// <summary>
    /// 테이프의 Index와 Start 값으로 추상 트리 노드를 생성합니다.
    /// </summary>
    /// <param name="tape"></param>
    /// <param name="type"></param>
    /// <param name="children"></param>
    /// <returns></returns>
    public static ASTNode ToASTNode(this ASTNodeBuilder tape, ASTNodeType type)
    {
        return tape.ToASTNode(type, tape.Children);
    }


    public static ASTNode ToASTNode(this ASTNodeBuilder tape, ASTNodeType type, List<ASTNode> children)
    {
        //if (tape.Parent != null)
        //    tape.UpdateToParent();
        return new(type, tape.Start, tape.Index - tape.Start, children);
    }

    public static ASTNode ToASTNode(this ASTNodeBuilder tape, int start, ASTNodeType type)
    {
        //if (tape.Parent != null)
        //    tape.UpdateToParent();
        return new(type, start, tape.Index - start, tape.Children);
    }

    public static ASTNode ToASTNode(this ASTNodeBuilder tape, int start, ASTNodeType type, List<ASTNode> children)
    {
        //if (tape.Parent != null)
        //    tape.UpdateToParent();
        return new(type, start, tape.Index - start, children);
    }


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

public class FuncExecutor<T>
{
    private readonly List<Func<T, bool>> _funcs = new List<Func<T, bool>>();

    // 함수 추가 메서드
    public void Add(Func<T, bool> func)
    {
        _funcs.Add(func);
    }

    public void Remove(Func<T, bool> func)
    {
        if (_funcs[^1] == func)
            _funcs.RemoveAt(_funcs.Count - 1);
        _funcs.Remove(func);
    }

    // 함수를 순차적으로 실행하여 true가 반환되면 멈춤
    public bool Invoke(T input)
    {
        foreach (var func in _funcs)
        {
            if (!func(input))
            {
                return false;
            }
        }
        return true;
    }
}