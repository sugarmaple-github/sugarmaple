namespace Sugarmaple.TheSeed.Namumark;
using System.Collections.Generic;

public class NamumarkDocument
{
    private List<NamumarkParagraph> _paragraphs;

    internal NamumarkDocument(List<NamumarkParagraph> paragraphs, string raw)
    {
        _paragraphs = paragraphs;
    }

    public NamumarkWikiElement DocumentElement { get; }
    // public static NamumarkDocument Parse(string namumark)
    //{
    //
    // }

    //Evaluate<T>(string xpathExpression, NamumarkNode contextNode, namespaceResolver, resultType, result)
    /*public T Evaluate<T>(string xpathExpression, NamumarkNode contextNode = this) where T : NamumarkNode
    {

    }

    public IEnumerable<T> EvaluateEnumerable<T>(string xpathExpression, NamumarkNode contextNode = null) where T : NamumarkNode
    {
        NamumarkElement element = DocumentElement;
        do
        {
            if (element is NamumarkParentElement parent)
            {
                foreach (var elem in parent.Children)
                {

                }
            }
        } while ();
    }*/
}
