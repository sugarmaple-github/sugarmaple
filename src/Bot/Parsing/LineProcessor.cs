using Sugarmaple.TheSeed.Namumark;

namespace Sugarmaple.TheSeed.Namumark.Parsing;

using Sugarmaple.TheSeed.Namumark.Parsing;

internal class LineProcessor
{
    private readonly ProcessorCollector<LineContext> _processors = new(new QuoteProcessor());

    public void Process(BriefStringTape tape, ASTNode node)
    {
        //한 칸 씩 진행하면서 개행을 만날 때마다 뭔가 처리해줘야 함.
        OnLineStart(tape, node);
        while (tape.TryProgress())
        {
            if (tape.Current == '\n')
            {
                OnLineStart(tape, node);
            }
        }
    }

    private void OnLineStart(BriefStringTape tape, ASTNode parent)
    {
        _processors.TryProgress(tape, new(_processors, parent, ""), out parent);
    }
}

internal record LineContext(
    ProcessorCollector<LineContext> Collector,
    ASTNode ParentNode,
    string LinePrefix)
;

internal class QuoteProcessor : NewProcessor<LineContext>
{
    public override string Open => ">";

    protected override ASTNode ProcessInternal(BriefStringTape tape, LineContext context)
    {
        //다음 개행을 찾고 그 이후 한 칸 이동한 뒤 현재 값이 '>'인 경우.
        var node = new ASTNode(ASTNodeType.Quote, tape.Start, tape.Index - tape.Start);

        while (tape.ConsumeTo('\n') && tape.Search(context.LinePrefix) && tape.TryProgress() && tape.Current == '>')
        {
            node.Add(new ASTNode(ASTNodeType.Br, tape.Index - 1, 2));
        }

        var children = context.ParentNode.Children;
        var moving = children.SkipWhile(o => o.Index < tape.Start).TakeWhile(o => o.Index + o.Length < tape.Index);
        node.Children.AddRange(moving);
        children.RemoveAll(o => o.Index < tape.Start || tape.Index < o.Index + o.Length);

        //ProcessorCollector가 타겟 캐릭터를 찾으면, 그 캐릭터의 프로세서를 실행
        context.Collector.TryProgress(tape, context with { ParentNode = node, LinePrefix = context.LinePrefix + ">" }, out _);
        return node;
    }
}

internal class ProcessorCollector<T>
{
    IEnumerable<NewProcessor<T>> _processors;

    public ProcessorCollector(params NewProcessor<T>[] processors)
    {
        _processors = processors;
    }

    public bool TryProgress(BriefStringTape tape, T context, out ASTNode ret)
    {
        ret = default;
        foreach (var pro in _processors)
        {
            if (tape.Search(pro.Open) && pro.TryProcess(tape, context, out ret))
                break;
        }
        return ret.Length > 0;
    }
}

internal abstract class NewProcessor<T>
{
    public abstract string Open { get; }

    public bool TryProcess(BriefStringTape state, T context, out ASTNode token)
    {
        token = ProcessInternal(state, context);
        return token.IsValid;
    }

    protected abstract ASTNode ProcessInternal(BriefStringTape state, T context);
}