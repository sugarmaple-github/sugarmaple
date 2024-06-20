namespace Sugarmaple.TheSeed.Namumark.Parsing;

using System.Text.RegularExpressions;

internal class LineProcessingPos
{
    public ASTNode Parent;
}

internal class LineProcessor
{
    private readonly ProcessorCollector<LineContext> _processors = new(new QuoteProcessor(), new TableProcessor());

    public void Process(string str, ASTNode rootNode)
    {
        rootNode = rootNode.Children[0];
        //parentNode
        Process(CreateTape(str, rootNode, out var pos), pos);
    }

    /// <summary>
    /// 트리를 지나다니면서 유효한 지점에만 반환합니다.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="rootNode"></param>
    /// <returns></returns>
    private NewStringTape CreateTape(string str, ASTNode rootNode, out LineProcessingPos pos)
    {
        var rPos = new LineProcessingPos() { Parent = rootNode };
        pos = rPos;
        var tape = new NewStringTape(str);
        var stack = new Stack<(ASTNode parent, int NextSiblingIndex, int NextPos)>();
        var preSiblingIndex = -1; //가장 최근에 지나친 형제 인덱스
        tape.MovedNext += o =>
        {
            while (preSiblingIndex < rPos.Parent.Children.Count - 1)
            {
                var siblings = rPos.Parent.Children;
                var nextSibling = siblings[preSiblingIndex + 1];
                if (o.Index >= nextSibling.Index) //현재 인덱스가 다음 형제 인덱스에 도달했다면
                {
                    if (nextSibling.Type == ASTNodeType.Content || (CanGetChildren(nextSibling.Type) && nextSibling.Children.Count > 0) ||
                        (nextSibling.Children != null && nextSibling.Children.Count > 0
                        && nextSibling.Children[0].Type == ASTNodeType.Content)) //그것이 자식을 가질 수 있다면
                    {
                        stack.Push((rPos.Parent, preSiblingIndex + 1, nextSibling.End)); //해당 형제를 부모로 갖지 않을 때, 재할당 받아야 할 값을 저장.
                        rPos.Parent = nextSibling; //해당 형제를 부모로
                        preSiblingIndex = -1;
                    }
                    else //없으면 점프 해야 함.
                    {
                        o.Index = nextSibling.End;
                        preSiblingIndex++;
                    }
                }
                else break;
            }
            return true;
        };

        tape.Ended += o =>
        {
            if (stack.TryPop(out var lastLayer))
            {
                (rPos.Parent, preSiblingIndex, o.Index) = lastLayer;
            }
        };

        return tape;
    }

    public void Process(NewStringTape tape, LineProcessingPos pos)
    {
        //한 칸 씩 진행하면서 개행을 만날 때마다 뭔가 처리해줘야 함.
        while (tape.MoveNextForStart())
        {
            if (tape.Index == 0 || (tape.Current == '\n' && tape.MoveNext()))
            {
                OnLineStart(tape, pos);
            }
        }
    }

    private void OnLineStart(NewStringTape tape, LineProcessingPos pos)
    {
        var parent = pos.Parent;
        if (_processors.TryProgress(tape, new(this, pos), out var node))
        {
            var oldChildren = parent.Children;
            parent.Children =
                oldChildren.TakeWhile(o => o.End <= node.Index)
                .Append(node)
                .Concat(oldChildren.SkipWhile(o => node.End > o.Index))
                .ToList();
        }
    }

    bool CanGetChildren(ASTNodeType nodeType) => nodeType is
        ASTNodeType.Document or ASTNodeType.Paragraph or ASTNodeType.Content or ASTNodeType.Heading;

    /// <summary>
    /// 해당 인덱스를 포함하는 최하위 부모를 가져옵니다.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ASTNode FindNeareastParent(int index, ASTNode root)
    {
        throw new NotImplementedException();
    }
}

internal record LineContext(
    LineProcessor Processor,
    LineProcessingPos Parent)
;

internal class QuoteProcessor : NewProcessor<LineContext>
{
    public override string Open => ">";

    protected override ASTNode ProcessInternal(NewStringTape tape, LineContext context)
    {
        tape.MovedNext += OnMoveNext;
        context.Processor.Process(tape, context.Parent);
        tape.MovedNext -= OnMoveNext;
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
            if (tape.Consume(pro.Open) && pro.TryProcess(tape, context, out ret))
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

internal class TableProcessor : NewProcessor<LineContext>
{
    public override string Open => "||";

    protected override ASTNode ProcessInternal(NewStringTape state, LineContext context)
    {
        //자식 테이프는 start를 진행시켜야 하나, 현재 테이프의 start 값은 여기서 고정되어야 한다.
        //따라서 이 시점에서 분기를 만들어야 한다. index를 이 위치에 저장하고 진행 됨.
        var tableStart = state.Start;
        var rowStart = state.Start;
        var rows = new List<ASTNode>();

        state.MovedNext += MovedNext;
        context.Processor.Process(state, context.Parent);
        state.MovedNext -= MovedNext;
        return state.ToASTNode(tableStart, ASTNodeType.Table, rows);

        bool MovedNext(NewStringTape o)
        {
            if (o.ConsumeBeforeNewline("||"))
            {
                rows.Add(state.ToASTNode(rowStart, ASTNodeType.TableRow));
                //TableRow의 확정
                rowStart = state.Start;
                if (o.Consume("||"))
                {
                }
                else
                {
                    //종료. Table의 확정.
                    return false;
                }
            }
            return true;
        }
    }

    //protected override ASTNode ProcessInternal(Progress state)
    //{
    //    var trTape = state.MainTape.MakeChildFromStart();
    //    var trList = new List<ASTNode>();
    //    var tdList = new List<ASTNode>();
    //    var tableMade = false;

    //    while (true)
    //    {
    //        var tdTape = trTape.MakeChild();
    //        var dict = ProcessCellAttribute(tdTape);
    //        if (!state.Creator.TryGetFreeElementList_(tdTape, new("||", false), out var list))
    //            break;

    //        tdList.Add(tdTape.ToASTNode(ASTNodeType.Td, list, dict));
    //        tdTape.Progress(Open.Length);
    //        tdTape.UpdateToParent();

    //        if (trTape.IsEnd || trTape.ConsumeIf('\n'))
    //        {
    //            tableMade = true;
    //            trList.Add(trTape.ToASTNode(ASTNodeType.Tr, tdList));
    //            tdList = new();

    //            trTape.Check();
    //            if (!trTape.ConsumeIf(Open)) break;
    //        }
    //    }
    //    if (tableMade)
    //        return state.MainTape.ToASTNode(ASTNodeType.Table, trList);
    //    return default;
    //}

    //protected ASTNode ProcessInternal_(Progress state)
    //{
    //    var table = state.MainTape.MakeChild();
    //    if (!state.Creator.TryGetFreeElementList_(table, new(o => o.Search("|"), true), out _))
    //        return default;

    //    var trTape = table.MakeChild();
    //    var trList = new List<ASTNode>();
    //    var tdList = new List<ASTNode>();
    //    var tableMade = false;

    //    while (state.Creator.TryGetFreeElementList_(trTape, new(o => o.Search("||\n"), false), out _))
    //    {
    //        var tdTape = trTape.MakeChild();
    //        var dict = ProcessCellAttribute(tdTape);

    //        //tdList.Add(tdTape.ToASTNode(ASTNodeType.Td, list, dict));
    //        tdTape.Progress(Open.Length);
    //        tdTape.UpdateToParent();

    //        if (trTape.IsEnd || trTape.ConsumeIf('\n'))
    //        {
    //            tableMade = true;
    //            trList.Add(trTape.ToASTNode(ASTNodeType.Tr, tdList));
    //            tdList = new();

    //            trTape.Check();
    //            if (!trTape.ConsumeIf(Open)) break;
    //        }
    //    }
    //    if (tableMade)
    //        return state.MainTape.ToASTNode(ASTNodeType.Table, trList);
    //    return default;
    //}

    //private static ASTNode ProcessCellAttribute(StringTape tdTape)
    //{
    //    var cellAttrTape = tdTape.MakeChild();
    //    var attributes = new List<ASTNode>();
    //    while (cellAttrTape.ConsumeIf("||")) ;
    //    attributes.Add(cellAttrTape.ToASTNode());

    //    var containing = new HashSet<string>();
    //    while (cellAttrTape.ConsumeIf('<'))
    //    {
    //        var attrKeyStart = cellAttrTape.Index;
    //        if (cellAttrTape.TryMatch(new Regex(@"\G[:()]>"), out var match))
    //        {
    //            if (!TryAdd("align", attrKeyStart, 0, match.Index, match.Length, false)) break;
    //        }
    //        else if (cellAttrTape.TryMatch(new Regex(@"\G[-|](\d+)>"), out match))
    //        {
    //            if (!TryAdd(match.ValueSpan[0].ToString(), match.Index, 1, match.Groups[1].Index, match.Groups[1].Length, false)) break;
    //        }
    //        else if (cellAttrTape.TryMatch(new Regex(@"\G#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})(?:,(?:#[0-9a-fA-F]{6}|[0-9a-fA-F]{3}))?"), out match))
    //        {
    //            if (!TryAdd("bgcolor", match.Index, 0, match.Groups[1].Index, match.Groups[1].Length, false)) break;
    //        }
    //        else if (cellAttrTape.TryMatch(new Regex(
    //            @"\G(color|bgcolor|width|height|colcolor|colbgcolor|table ?color|table ?width|table ?align|table ?bgcolor|table ?bordercolor)=(.*?)>")
    //            , out match))
    //        {
    //            if (!TryAdd(match.Groups[1].Value.Replace(" ", null)
    //                , match.Index, match.Groups[1].Length,
    //                match.Groups[2].Index, match.Groups[2].Length, true))
    //                break;
    //        }
    //        else break;
    //        cellAttrTape.UpdateToParent();

    //        bool TryAdd(string key, int keyIndex, int keyLength, int valueIndex, int valueLength, bool equalExists)
    //        {
    //            var wholeIndex = keyIndex - 1;
    //            var wholeLength = keyLength + valueLength + (equalExists ? 1 : 0);
    //            if (!containing.Contains(key))
    //            {
    //                containing.Add(key);
    //                attributes.Add(new(0, wholeIndex, wholeLength, new() {
    //                    new(0, keyIndex, keyLength),
    //                    new(0, valueIndex, valueLength)}));
    //                return true;
    //            }
    //            return false;
    //        }
    //    }
    //    return cellAttrTape.ToASTNode(ASTNodeType.Td, attributes);
    //}

}