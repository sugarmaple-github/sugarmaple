namespace Sugarmaple.TheSeed.Namumark.Parsing;

using MoreLinq;

internal class LineProcessor
{
    private readonly ProcessorCollector<LineContext> _processors = new(new QuoteProcessor());

    public void Process(string str, ASTNode docNode) => Process(new NewStringTape(str), docNode);

    public void Process(NewStringTape tape, ASTNode node)
    {
        //한 칸 씩 진행하면서 개행을 만날 때마다 뭔가 처리해줘야 함.
        OnLineStart(tape, node);
        while (tape.MoveNext())
        {
            if (tape.Current == '\n')
            {
                OnLineStart(tape, node);
            }
        }
    }

    private void OnLineStart(NewStringTape tape, ASTNode root)
    {
        var parent = FindParentOfIndex(tape.Index, root);
        if (CanGetChildren(parent.Type) &&
            _processors.TryProgress(tape, new(this, parent), out var node))
        {
            var oldChildren = parent.Children;
            parent.Children =
                oldChildren.TakeWhile(o => o.End <= node.Index)
                .Append(node)
                .Concat(oldChildren.SkipWhile(o => node.End > o.Index))
                .ToList();
        }
    }

    bool CanGetChildren(ASTNodeType nodeType) => false;

    /// <summary>
    /// 해당 인덱스를 포함하는 최하위 부모를 가져옵니다.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ASTNode FindParentOfIndex(int index, ASTNode root)
    {
        throw new NotImplementedException();
    }
}

internal record LineContext(
    LineProcessor Processor,
    ASTNode Parent)
;

internal class QuoteProcessor : NewProcessor<LineContext>
{
    public override string Open => ">";

    protected override ASTNode ProcessInternal(NewStringTape tape, LineContext context)
    {
        tape.OnMoveNext += OnMoveNext;
        context.Processor.Process(tape, context.Parent);
        tape.OnMoveNext -= OnMoveNext;
        return tape.ToASTNode(ASTNodeType.Quote);

        static bool OnMoveNext(NewStringTape o) => !(o.Current == '\n' && o.Next() == '>');
    }
}

internal class ProcessorCollector<T>
{
    IEnumerable<NewProcessor<T>> _processors;

    public ProcessorCollector(params NewProcessor<T>[] processors)
    {
        _processors = processors;
    }

    /// <summary>
    /// 테이프를 받아서 node를 ret로 반환합니다.
    /// </summary>
    /// <param name="tape"></param>
    /// <param name="context"></param>
    /// <param name="ret"></param>
    /// <returns></returns>
    public bool TryProgress(NewStringTape tape, T context, out ASTNode ret)
    {
        ret = default;
        foreach (var pro in _processors)
        {
            if (tape.StartsWith(pro.Open) && pro.TryProcess(tape, context, out ret))
                break;
        }
        return ret.Length > 0;
    }
}

internal abstract class NewProcessor<T>
{
    public abstract string Open { get; }

    public bool TryProcess(NewStringTape state, T context, out ASTNode token)
    {
        token = ProcessInternal(state, context);
        return token.IsValid;
    }

    protected abstract ASTNode ProcessInternal(NewStringTape state, T context);
}