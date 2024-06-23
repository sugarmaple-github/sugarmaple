namespace Sugarmaple.TheSeed.Namumark.Parsing;

using static org.apache.xerces.util.NamespaceSupport;

internal class LineProcessingPos
{
    public ASTNode Parent;
    public bool IsInNewline;
}

internal delegate ASTNode Mediater(string raw, int start, int index, int end, IEnumerable<ASTNode> children, IEnumerable<char> prefixes, LineProcessor processor, out int turnBackIndex);

internal class LineProcessor
{
    //private readonly ProcessorCollector<LineContext> _processors = new(new QuoteProcessor(), new TableProcessor());
    private readonly List<(string, char, Mediater)> _processors = new() { (">", '>', QuoteMediator) };

    private static ASTNode QuoteMediator(string raw, int start, int index, int end, IEnumerable<ASTNode> children, IEnumerable<char> prefixes, LineProcessor processor, out int turnBackIndex)
    {
        var builder = processor.ReviseChildList(raw, start, index, end, children.ToList(), prefixes.Append('>'), out turnBackIndex);
        return builder.ToASTNode(ASTNodeType.Quote);
    }

    public void Process(string str, ASTNode rootNode)
    {
        rootNode = rootNode.Children[0];

        var searchingTargets =
            rootNode.Select(o => o.Children[1])
            .SelectMany(Traverse)
            .Where(o => o.Type == ASTNodeType.Content)
            .ToArray();
        //parentNode
        foreach (var o in searchingTargets)
        {
            var builder = ReviseChildList(str, o.Index, o.Index, o.End, o.Children, Enumerable.Empty<char>(), out _);
            o.Children.Clear();
            o.Children.AddRange(builder.Children);
        }
        //Process(CreateTape(str, rootNode, out var pos), pos);
    }

    private IEnumerable<ASTNode> Traverse(ASTNode rootNode)
    {
        var queue = new Queue<ASTNode>();
        queue.Enqueue(rootNode);
        while (queue.TryDequeue(out var node))
        {
            yield return node;
            foreach (var o in node)
            {
                queue.Enqueue(o);
            }
        }
    }

    private ASTNodeBuilder ReviseChildList(string raw, int start, int index, int end, IReadOnlyList<ASTNode> childList, IEnumerable<char> prefixes, out int turnBackIndex)
    {
        var ret = new ASTNodeBuilder(raw, start, index, end);
        ReviseChildList(ret, childList, prefixes, out turnBackIndex);
        return ret;
    }

    /// <summary>
    /// 테이프와 자식 리스트를 받아서 개행 관련 문법이 적용된 새로운 자식 리스트를 반환합니다.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="childList">이 리스트는 불변합니다.</param>
    /// <param name="prefixes">요구 접두사. 개행 이후 해당 접두사가 없다면 즉시 종료됩니다.</param>
    /// <param name="turnBackIndex">반환 예정 위치입니다. 이 인덱스부터 끝까지 부모가 재수령해야 합니다.</param>
    /// <returns>개정된 리스트입니다.</returns>
    private void ReviseChildList(ASTNodeBuilder builder, IReadOnlyList<ASTNode> childList, IEnumerable<char> prefixes, out int turnBackIndex)
    {
        var nextIndex = 0;
        if (builder.MoveNext())
        {
            CheckPrefix(CheckPrefixAndProgressFirst);
            do
            {
                EmbraceSibling();
                //테이프 처음이라면, 이거나 개행 위치라면.
                if (builder.Current == '\n')
                //현재 접두사 검사 없이 추가 문법 검사
                {
                    if (!CheckPrefix(CheckPrefixAndProgress))
                        break;
                }
            } while (builder.MoveNext());
        }
        turnBackIndex = nextIndex;

        void EmbraceSibling()
        {
            //다음 형제 노드의 인덱스에 접촉하면
            if (nextIndex < childList.Count && builder.Index >= childList[nextIndex].Index)
            {
                //그 형제를 추가하고,
                builder.Add(childList[nextIndex++]);
            }
        }

        bool CheckPrefix(PrefixChecker checker)
        {
            var node = checker(builder.Branch(), childList.Skip(nextIndex), prefixes, out var addingIdx);
            if (node.IsValid)
            {
                builder.Add(node);
                nextIndex += addingIdx;
            }
            return node.IsValid;
        }
    }
    private delegate ASTNode PrefixChecker(ASTNodeBuilder tape, IEnumerable<ASTNode> children, IEnumerable<char> prefixes, out int turnBackIndex);

    private ASTNode CheckPrefixAndProgressFirst(ASTNodeBuilder tape, IEnumerable<ASTNode> children, IEnumerable<char> prefixes, out int turnBackIndex)
    {
        tape.MoveNext();
        turnBackIndex = 0;
        var start = tape.Index;
        foreach ((var open, var prefix, var mediator) in _processors)
        {
            if (tape.Consume(open))
            {
                return mediator(tape.Raw, start, tape.Index, tape.EndIndex,
                    children, prefixes, this, out turnBackIndex);
            }
        }
        return default;
    }

    private ASTNode CheckPrefixAndProgress(ASTNodeBuilder tape, IEnumerable<ASTNode> children, IEnumerable<char> prefixes, out int turnBackIndex)
    {
        tape.MoveNext();
        turnBackIndex = 0;
        if (!tape.MoveNext())//개행 제거
        {
            return tape.ToASTNode(ASTNodeType.Br); //마지막에 위치한 개행일 경우.
        }

        var start = tape.Index;
        var prefixEnumerator = prefixes.GetEnumerator();
    loop:
        foreach ((var open, var prefix, var mediator) in _processors)
        {
            if (tape.Consume(open))
            {
                //접두사가 남아있고, 현재 판별된 접두사가 그것과 다를 때.
                if (prefixEnumerator.MoveNext())
                    //현재 접두사와 동일하면, 새 노드를 만들지 않고 진행.
                    if (prefixEnumerator.Current == prefix)
                        goto loop;
                    else
                        //아니라면 false 반환.
                        return default;
                return mediator(tape.Raw, start, tape.Index, tape.EndIndex,
                    children, prefixes, this, out turnBackIndex);
            }
        }
        return prefixEnumerator.MoveNext() ? default : tape.ToASTNode(ASTNodeType.Br);
    }

    /// <summary>
    /// 트리를 지나다니면서 유효한 지점에만 반환합니다.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="rootNode"></param>
    /// <returns></returns>
    private ASTNodeBuilder CreateTape(string str, ASTNode rootNode, out LineProcessingPos pos)
    {
        var rPos = new LineProcessingPos() { Parent = rootNode };
        pos = rPos;
        var tape = new ASTNodeBuilder(str);
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

    //public void Process(NewStringTape tape, LineProcessingPos pos)
    //{
    //    //한 칸 씩 진행하면서 개행을 만날 때마다 뭔가 처리해줘야 함.
    //    while (tape.MoveNextForStart())
    //    {
    //        if (tape.Current == '\n')
    //        {
    //            pos.IsInNewline = true;
    //            if (tape.MoveNext())
    //            {
    //                OnLineStart(tape, pos);
    //                continue;
    //            }
    //            else return;
    //        }

    //        if (tape.Index == 0 || pos.IsInNewline)
    //        {
    //            OnLineStart(tape, pos);
    //        }
    //    }
    //}

    //private void OnLineStart(NewStringTape tape, LineProcessingPos pos)
    //{
    //    pos.IsInNewline = true;
    //    var parent = pos.Parent;
    //    if (_processors.TryProgress(tape, new(this, pos), out var node))
    //    {
    //        var oldChildren = parent.Children;//TODO: relogic
    //        pos.Parent.Children =
    //            oldChildren.TakeWhile(o => o.End <= node.Index)
    //            .Append(node)
    //            .Concat(oldChildren.SkipWhile(o => node.End > o.Index))
    //            .ToList();
    //    }
    //    pos.IsInNewline = false;
    //}

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
    LineProcessingPos Pos)
;

//internal class QuoteProcessor : NewProcessor<LineContext>
//{
//    public override string Open => ">";

//    protected override ASTNode ProcessInternal(NewStringTape tape, LineContext context)
//    {
//        var start = tape.Start;
//        var resolved = true;
//        //tape.MovingNext += MovedNext;
//        context.Processor.Process(tape, context.Pos);
//        //tape.MovingNext -= MovedNext;
//        return tape.ToASTNode(start, ASTNodeType.Quote);

//        bool MovedNext(NewStringTape o)
//        {
//            if (context.Pos.IsInNewline)
//            {
//                if (!resolved)
//                {
//                    if (o.Next() == '>')
//                    {
//                        resolved = true;
//                        o.Index++;
//                        return true;
//                    }
//                    else
//                        return false;
//                }
//            }
//            else
//            {
//                resolved = false;
//            }
//            return true;
//        }
//    }
//}

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
    public bool TryProgress(ASTNodeBuilder tape, T context, out ASTNode ret)
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

    public bool TryProcess(ASTNodeBuilder state, T context, out ASTNode token)
    {
        token = ProcessInternal(state, context);
        return token.IsValid;
    }

    protected abstract ASTNode ProcessInternal(ASTNodeBuilder state, T context);
}

//다른 parsing layer에서 관리할 예정
//internal class TableProcessor : NewProcessor<LineContext>
//{
//    public override string Open => "||";

//    protected override ASTNode ProcessInternal(NewStringTape state, LineContext context)
//    {
//        //자식 테이프는 start를 진행시켜야 하나, 현재 테이프의 start 값은 여기서 고정되어야 한다.
//        //따라서 이 시점에서 분기를 만들어야 한다. index를 이 위치에 저장하고 진행 됨.
//        var tableStart = state.Start;
//        var rowStart = state.Start;
//        var rows = new List<ASTNode>();

//        state.MovedNext += MovedNext;
//        context.Processor.Process(state, context.Pos);
//        state.MovedNext -= MovedNext;
//        return state.ToASTNode(tableStart, ASTNodeType.Table, rows);

//        bool MovedNext(NewStringTape o)
//        {
//            if (o.ConsumeBeforeNewline("||"))
//            {
//                rows.Add(state.ToASTNode(rowStart, ASTNodeType.TableRow));
//                //TableRow의 확정
//                rowStart = state.Start;
//                if (o.Consume("||"))
//                {
//                }
//                else
//                {
//                    //종료. Table의 확정.
//                    return false;
//                }
//            }
//            return true;
//        }
//    }

//    //protected override ASTNode ProcessInternal(Progress state)
//    //{
//    //    var trTape = state.MainTape.MakeChildFromStart();
//    //    var trList = new List<ASTNode>();
//    //    var tdList = new List<ASTNode>();
//    //    var tableMade = false;

//    //    while (true)
//    //    {
//    //        var tdTape = trTape.MakeChild();
//    //        var dict = ProcessCellAttribute(tdTape);
//    //        if (!state.Creator.TryGetFreeElementList_(tdTape, new("||", false), out var list))
//    //            break;

//    //        tdList.Add(tdTape.ToASTNode(ASTNodeType.Td, list, dict));
//    //        tdTape.Progress(Open.Length);
//    //        tdTape.UpdateToParent();

//    //        if (trTape.IsEnd || trTape.ConsumeIf('\n'))
//    //        {
//    //            tableMade = true;
//    //            trList.Add(trTape.ToASTNode(ASTNodeType.Tr, tdList));
//    //            tdList = new();

//    //            trTape.Check();
//    //            if (!trTape.ConsumeIf(Open)) break;
//    //        }
//    //    }
//    //    if (tableMade)
//    //        return state.MainTape.ToASTNode(ASTNodeType.Table, trList);
//    //    return default;
//    //}

//    //protected ASTNode ProcessInternal_(Progress state)
//    //{
//    //    var table = state.MainTape.MakeChild();
//    //    if (!state.Creator.TryGetFreeElementList_(table, new(o => o.Search("|"), true), out _))
//    //        return default;

//    //    var trTape = table.MakeChild();
//    //    var trList = new List<ASTNode>();
//    //    var tdList = new List<ASTNode>();
//    //    var tableMade = false;

//    //    while (state.Creator.TryGetFreeElementList_(trTape, new(o => o.Search("||\n"), false), out _))
//    //    {
//    //        var tdTape = trTape.MakeChild();
//    //        var dict = ProcessCellAttribute(tdTape);

//    //        //tdList.Add(tdTape.ToASTNode(ASTNodeType.Td, list, dict));
//    //        tdTape.Progress(Open.Length);
//    //        tdTape.UpdateToParent();

//    //        if (trTape.IsEnd || trTape.ConsumeIf('\n'))
//    //        {
//    //            tableMade = true;
//    //            trList.Add(trTape.ToASTNode(ASTNodeType.Tr, tdList));
//    //            tdList = new();

//    //            trTape.Check();
//    //            if (!trTape.ConsumeIf(Open)) break;
//    //        }
//    //    }
//    //    if (tableMade)
//    //        return state.MainTape.ToASTNode(ASTNodeType.Table, trList);
//    //    return default;
//    //}

//    //private static ASTNode ProcessCellAttribute(StringTape tdTape)
//    //{
//    //    var cellAttrTape = tdTape.MakeChild();
//    //    var attributes = new List<ASTNode>();
//    //    while (cellAttrTape.ConsumeIf("||")) ;
//    //    attributes.Add(cellAttrTape.ToASTNode());

//    //    var containing = new HashSet<string>();
//    //    while (cellAttrTape.ConsumeIf('<'))
//    //    {
//    //        var attrKeyStart = cellAttrTape.Index;
//    //        if (cellAttrTape.TryMatch(new Regex(@"\G[:()]>"), out var match))
//    //        {
//    //            if (!TryAdd("align", attrKeyStart, 0, match.Index, match.Length, false)) break;
//    //        }
//    //        else if (cellAttrTape.TryMatch(new Regex(@"\G[-|](\d+)>"), out match))
//    //        {
//    //            if (!TryAdd(match.ValueSpan[0].ToString(), match.Index, 1, match.Groups[1].Index, match.Groups[1].Length, false)) break;
//    //        }
//    //        else if (cellAttrTape.TryMatch(new Regex(@"\G#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})(?:,(?:#[0-9a-fA-F]{6}|[0-9a-fA-F]{3}))?"), out match))
//    //        {
//    //            if (!TryAdd("bgcolor", match.Index, 0, match.Groups[1].Index, match.Groups[1].Length, false)) break;
//    //        }
//    //        else if (cellAttrTape.TryMatch(new Regex(
//    //            @"\G(color|bgcolor|width|height|colcolor|colbgcolor|table ?color|table ?width|table ?align|table ?bgcolor|table ?bordercolor)=(.*?)>")
//    //            , out match))
//    //        {
//    //            if (!TryAdd(match.Groups[1].Value.Replace(" ", null)
//    //                , match.Index, match.Groups[1].Length,
//    //                match.Groups[2].Index, match.Groups[2].Length, true))
//    //                break;
//    //        }
//    //        else break;
//    //        cellAttrTape.UpdateToParent();

//    //        bool TryAdd(string key, int keyIndex, int keyLength, int valueIndex, int valueLength, bool equalExists)
//    //        {
//    //            var wholeIndex = keyIndex - 1;
//    //            var wholeLength = keyLength + valueLength + (equalExists ? 1 : 0);
//    //            if (!containing.Contains(key))
//    //            {
//    //                containing.Add(key);
//    //                attributes.Add(new(0, wholeIndex, wholeLength, new() {
//    //                    new(0, keyIndex, keyLength),
//    //                    new(0, valueIndex, valueLength)}));
//    //                return true;
//    //            }
//    //            return false;
//    //        }
//    //    }
//    //    return cellAttrTape.ToASTNode(ASTNodeType.Td, attributes);
//    //}

//}