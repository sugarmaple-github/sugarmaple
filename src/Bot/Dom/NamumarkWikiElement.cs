namespace Sugarmaple.TheSeed.Namumark;
using System.Collections.Generic;

public class NamumarkWikiElement : NamumarkParentElement<NamumarkParagraph>
{

    internal NamumarkWikiElement(List<NamumarkParagraph> paragraphs, NamumarkDocument doc, string raw) : base(paragraphs, doc, raw)
    {
    }

    // public static NamumarkDocument Parse(string namumark)
    //{
    //
    // }

    //Evaluate<T>(string xpathExpression, NamumarkNode contextNode, namespaceResolver, resultType, result)
}
