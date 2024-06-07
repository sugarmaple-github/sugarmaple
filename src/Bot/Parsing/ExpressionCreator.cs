namespace Sugarmaple.TheSeed.Namumark.Parsing;

using System.Diagnostics.CodeAnalysis;

internal record struct Closer(string? Raw, bool Singleline)
{
    public Func<StringTape, bool> Func { get; set; }

    public Closer(Func<StringTape, bool> func, bool Singleline) : this((string?)null, Singleline)
    {
        Func = func;
    }

    public readonly bool HasFound(StringTape tape)
    {
        return Raw != null && tape.Search(Raw);
    }
}


internal class ExpressionCreator //이 클래스는 _raw, _dict, _index, _context를 받은 뒤, 해당 구문에 따라 List<>
{
    internal delegate ASTNode ProcessorDelegate(Progress creator);

    private static readonly List<Processor> _processors = new()
    {
        new LinkProcessor(),
        new MacroProcessor(),
        new BraceProcessor(),
        //new FontProcessor("'''", ASTNodeType.Bold),
        //new FontProcessor("''", ASTNodeType.Italic),
    };
    private static readonly List<Processor> _newLineProcessors = new()
    {
        //new TableProcessor(),

    };
    private static readonly Processor HeadingProcessor = new HeadingProcessor();

    public ExpressionCreator()
    {
    }

    #region FreeElementList
    public ASTNode GetFreeElementList(StringTape tape)
    {
        return GetFreeElementList(tape, default)!;
    }

    public ASTNode GetFreeElementList(StringTape inTape, Closer closer)
    {
        var ret = new List<ASTNode>();
        var tape = inTape.MakeChild();
        while (!tape.IsEnd && !(ret.Count > 0 && ret[^1].Type == ASTNodeType.Heading))
        {
            if (closer.HasFound(tape))
            {
                var node = tape.ToASTNode(ASTNodeType.Content, ret);
                inTape.Progress(closer.Raw.Length);
                return node;
            }

            if (!ProcessSingleStep(tape, ret, closer))
            {
                tape.Index = tape.Start;
                return default;
            }
        }
        tape.UpdateToParent();
        return tape.ToASTNode(ASTNodeType.Content, ret);
    }

    [Obsolete]
    public bool TryGetFreeElementList(StringTape tape, Closer closer, out ASTNode output)
    {
        output = GetFreeElementList(tape, closer);
        return output.Type == ASTNodeType.Content;
    }

    [Obsolete]
    public bool TryGetFreeElementList_(StringTape _tape, Closer closer, out ASTNode output)
    {
        output = GetFreeElementList(_tape, closer);
        if (closer.Raw != null)
            _tape.Index -= closer.Raw.Length;
        return output.Type == ASTNodeType.Content;
    }

    #endregion
    private bool ProcessSingleStep(StringTape tape, List<ASTNode> nodes, Closer closer)
    {
        var curTape = tape.MakeChild();
        var progress = new Progress(this, curTape);
        if (((curTape.Index == 0 || curTape.SearchRelatively(-1, '\n')) && TryProcessNewline(progress, closer, out var node)) ||
            TryProgressInline(progress, out node))
        {
            nodes.Add(node);
        }
        else
            tape.Progress(1);

        if (tape.IsEnd && closer.Raw != null)
            return false;
        return true;
    }

    public ASTNode TokenizeOne(StringTape tape)
    {
        {
            var curTape = tape.MakeChild();
            var progress = new Progress(this, curTape);
            if ((curTape.IsNewLine() && TryProcessNewline_(progress, out var node)) ||
                TryProgressInline(progress, out node))
                return node;
        }
        tape.Progress(1);
        return default;
    }

    private static bool TryProgressInline(Progress progress, out ASTNode ret)
    {
        ret = default;
        foreach (var pro in _processors)
        {
            if (progress.MainTape.Search(pro.Open) && pro.TryProcess(progress, out ret))
                break;
        }
        return ret.Length > 0;
    }

    private bool TryProcessNewline_(Progress progress, out ASTNode ret)
    {
        ret = default;
        return !(progress.MainTape.Search("=") && HeadingProcessor.TryProcess(progress, out ret));
    }

    [Obsolete]
    private bool TryProcessNewline(Progress progress, Closer closer, out ASTNode ret)
    {
        ret = default;
        if (!closer.Singleline &&
            !(progress.MainTape.Search("=") && HeadingProcessor.TryProcess(progress, out ret)))
        {
            foreach (var pro in _newLineProcessors)
                if (progress.MainTape.Search(pro.Open))
                    if (pro.TryProcess(progress, out ret))
                        break;
        }
        return ret.Length > 0;
    }
}

internal static class ExpressionCreatorExtensions
{
    public static IEnumerable<ASTNode> LazyParse(this ExpressionCreator self, StringTape tape)
    {
        ASTNode node;
        while (!tape.IsEnd)
        {
            node = self.TokenizeOne(tape);
            if (node.Type == ASTNodeType.None) continue;
            yield return node;
        }
    }

    public static List<ASTNode>? CloserParse(this ExpressionCreator self, StringTape tape, Closer closer)
    {
        var ret = new List<ASTNode>();
        while (!tape.IsEnd)
        {
            if (closer.HasFound(tape))
                return ret;
            if (tape.Search('\n') && closer.Singleline)
                return null;

            var node = self.TokenizeOne(tape);
            if (node.Type == ASTNodeType.None)
                continue;
            ret.Add(node);
        }
        return null;
    }

    public static bool TryCloserParseList(this ExpressionCreator self, StringTape tape, Closer closer, [MaybeNullWhen(false)] out List<ASTNode> result)
        => (result = self.CloserParse(tape, closer)) != null;

    public static bool TryCloserParse(this ExpressionCreator self, StringTape tape, Closer closer, [MaybeNullWhen(false)] out ASTNode result)
    {
        if (self.TryCloserParseList(tape, closer, out var ret))
        {
            var node = tape.ToASTNode(ASTNodeType.Content, ret);
            tape.Progress(closer.Raw.Length);
            tape.UpdateToParent();
            result = node;
            return true;
        }
        result = default;
        return false;
    }
}