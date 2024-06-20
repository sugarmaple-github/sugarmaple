namespace Sugarmaple.TheSeed.Namumark;

using Sugarmaple.TheSeed.Namumark.Parsing;

public class DocumentFactory
{
    public static DocumentFactory Default { get; } = new();

    public Document Parse(string content)
    {
        var precess = new NamumarkProcess(content);
        var astTree = precess.ParseAsToken();
        var lineProcessr = new LineProcessor();
        lineProcessr.Process(content, astTree);

        var parser = new Parser();
        parser._raw = content;
        var doc = (Document)parser.Parse(astTree);
        return doc;
    }
}

