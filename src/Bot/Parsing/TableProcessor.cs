namespace Sugarmaple.TheSeed.Namumark.Parsing;
using System.Text.RegularExpressions;


internal class HorizontalLineProcessor : Processor
{
    public override string Open => "----";

    protected override ASTNode ProcessInternal(Progress state)
    {

        return default;
    }
}

internal class FontProcessor : Processor
{
    public string _open;
    public ASTNodeType _type;

    public FontProcessor(string open, ASTNodeType type)
    {
        _open = open;
        _type = type;
    }

    public override string Open => _open;

    protected override ASTNode ProcessInternal(Progress state)
    {
        if (state.Creator.TryGetFreeElementList(state.MainTape, new Closer(Open, true), out var token))
            return state.MainTape.ToASTNode(_type, token);
        return default;
    }
}
internal class TableProcessor : Processor
{
    public override string Open => "||";

    protected override ASTNode ProcessInternal(Progress state)
    {
        var trTape = state.MainTape.MakeChildFromStart();
        var trList = new List<ASTNode>();
        var tdList = new List<ASTNode>();
        var tableMade = false;

        while (true)
        {
            var tdTape = trTape.MakeChild();
            var dict = ProcessCellAttribute(tdTape);
            if (!state.Creator.TryGetFreeElementList_(tdTape, new("||", false), out var list))
                break;

            tdList.Add(tdTape.ToASTNode(ASTNodeType.Td, list, dict));
            tdTape.Progress(Open.Length);
            tdTape.UpdateToParent();

            if (trTape.IsEnd || trTape.ConsumeIf('\n'))
            {
                tableMade = true;
                trList.Add(trTape.ToASTNode(ASTNodeType.Tr, tdList));
                tdList = new();

                trTape.Check();
                if (!trTape.ConsumeIf(Open)) break;
            }
        }
        if (tableMade)
            return state.MainTape.ToASTNode(ASTNodeType.Table, trList);
        return default;
    }

    protected ASTNode ProcessInternal_(Progress state)
    {
        var table = state.MainTape.MakeChild();
        if (!state.Creator.TryGetFreeElementList_(table, new(o => o.Search("|"), true), out _))
            return default;

        var trTape = table.MakeChild();
        var trList = new List<ASTNode>();
        var tdList = new List<ASTNode>();
        var tableMade = false;

        while (state.Creator.TryGetFreeElementList_(trTape, new(o => o.Search("||\n"), false), out _))
        {
            var tdTape = trTape.MakeChild();
            var dict = ProcessCellAttribute(tdTape);

            //tdList.Add(tdTape.ToASTNode(ASTNodeType.Td, list, dict));
            tdTape.Progress(Open.Length);
            tdTape.UpdateToParent();

            if (trTape.IsEnd || trTape.ConsumeIf('\n'))
            {
                tableMade = true;
                trList.Add(trTape.ToASTNode(ASTNodeType.Tr, tdList));
                tdList = new();

                trTape.Check();
                if (!trTape.ConsumeIf(Open)) break;
            }
        }
        if (tableMade)
            return state.MainTape.ToASTNode(ASTNodeType.Table, trList);
        return default;
    }

    private static ASTNode ProcessCellAttribute(StringTape tdTape)
    {
        var cellAttrTape = tdTape.MakeChild();
        var attributes = new List<ASTNode>();
        while (cellAttrTape.ConsumeIf("||")) ;
        attributes.Add(cellAttrTape.ToASTNode());

        var containing = new HashSet<string>();
        while (cellAttrTape.ConsumeIf('<'))
        {
            var attrKeyStart = cellAttrTape.Index;
            if (cellAttrTape.TryMatch(new Regex(@"\G[:()]>"), out var match))
            {
                if (!TryAdd("align", attrKeyStart, 0, match.Index, match.Length, false)) break;
            }
            else if (cellAttrTape.TryMatch(new Regex(@"\G[-|](\d+)>"), out match))
            {
                if (!TryAdd(match.ValueSpan[0].ToString(), match.Index, 1, match.Groups[1].Index, match.Groups[1].Length, false)) break;
            }
            else if (cellAttrTape.TryMatch(new Regex(@"\G#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})(?:,(?:#[0-9a-fA-F]{6}|[0-9a-fA-F]{3}))?"), out match))
            {
                if (!TryAdd("bgcolor", match.Index, 0, match.Groups[1].Index, match.Groups[1].Length, false)) break;
            }
            else if (cellAttrTape.TryMatch(new Regex(
                @"\G(color|bgcolor|width|height|colcolor|colbgcolor|table ?color|table ?width|table ?align|table ?bgcolor|table ?bordercolor)=(.*?)>")
                , out match))
            {
                if (!TryAdd(match.Groups[1].Value.Replace(" ", null)
                    , match.Index, match.Groups[1].Length,
                    match.Groups[2].Index, match.Groups[2].Length, true))
                    break;
            }
            else break;
            cellAttrTape.UpdateToParent();

            bool TryAdd(string key, int keyIndex, int keyLength, int valueIndex, int valueLength, bool equalExists)
            {
                var wholeIndex = keyIndex - 1;
                var wholeLength = keyLength + valueLength + (equalExists ? 1 : 0);
                if (!containing.Contains(key))
                {
                    containing.Add(key);
                    attributes.Add(new(0, wholeIndex, wholeLength, new() {
                        new(0, keyIndex, keyLength),
                        new(0, valueIndex, valueLength)}));
                    return true;
                }
                return false;
            }
        }
        return cellAttrTape.ToASTNode(ASTNodeType.Td, attributes);
    }

}
