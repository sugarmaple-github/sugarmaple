namespace Sugarmaple.TheSeed.Namumark;

using Sugarmaple.TheSeed.Namumark.Parsing;

public class DocumentFactory
{
    public static DocumentFactory Default { get; } = new();

    public Document Parse(string content)
    {
        var precess = new NamumarkProcess(content);
        var astTree = precess.ParseAsToken();

        var parser = new Parser();
        parser._raw = content;
        var doc = (Document)parser.Parse(astTree);
        return doc;
    }
}
internal class LineProcessor
{
    string _source;
    ASTNode _curNode;
    Progress _progress;

    ProcessorCollector _collector;

    public ASTNode Process()
    {
        throw new Exception();
        //_collector.TryProgress(_progress, out var ret);
    }

    private void Process(in ASTNode node)
    {
        _collector.TryProgress(_progress, out var ret);
    }

    //한 칸 씩 진행하면서 개행을 만날 때마다 뭔가 처리해줘야 함.
}

internal class ProcessorCollector
{
    List<Processor> _processors;

    public bool TryProgress(Progress progress, out ASTNode ret)
    {
        ret = default;
        foreach (var pro in _processors)
        {
            if (progress.MainTape.Search(pro.Open) && pro.TryProcess(progress, out ret))
                break;
        }
        return ret.Length > 0;
    }
}