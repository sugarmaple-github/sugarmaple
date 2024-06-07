namespace Sugarmaple.XPath;

using Sugarmaple.TheSeed.Namumark;
using System.Xml;
using System.Xml.XPath;

public static class Extensions
{
    public static IElement SelectSingleNode(this IElement element, string xpath, IXmlNamespaceResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(xpath);
        var nav = new NamumarkNavigator(element.OwnerDocument, element);
        var found = nav.SelectSingleNode(xpath, new XmlNamespaceManager(new NameTable()));
        return ((NamumarkNavigator)found!).CurrentNode;
    }

    public static List<IElement> SelectNodes(this IElement element, string xpath, IXmlNamespaceResolver resolver, bool ignoreNamespaces)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(xpath);
        var nav = new NamumarkNavigator(element.OwnerDocument, element);
        var found = nav.Select(xpath, new XmlNamespaceManager(new NameTable()));
        var ret = new List<IElement>();
        while (found.MoveNext())
        {
            var navigator = (NamumarkNavigator)found.Current;
            var node = navigator.CurrentNode;
            ret.Add(node);
        }
        return ret;
    }
}

internal class NamumarkNavigator : XPathNavigator
{
    private readonly Document _document;
    private IElement _currentNode;

    public NamumarkNavigator(Document document, IElement currentNode)
    {
        _document = document;
        _currentNode = currentNode;
    }

    public override string BaseURI => _document.BaseUri;

    public override bool IsEmptyElement => throw new NotImplementedException();

    public override string LocalName => _currentNode.LocalName;

    public override string Name => throw new NotImplementedException();

    public override string NamespaceURI => "";
    // TODO: Implement Namespace URI well

    public override XmlNameTable NameTable => throw new NotImplementedException();

    public override XPathNodeType NodeType
    {
        get
        {
            return _currentNode switch
            {
                Text => XPathNodeType.Text,
                IElement => XPathNodeType.Element,
                _ => throw new NotImplementedException()
            };
        }
    }

    public override string Prefix => throw new NotImplementedException();

    public override string Value => throw new NotImplementedException();

    public IElement CurrentNode => _currentNode;

    public override XPathNavigator Clone()
    {
        return new NamumarkNavigator(_document, _currentNode);
    }

    public override bool IsSamePosition(XPathNavigator other)
    {
        return other is NamumarkNavigator navigator && navigator._currentNode == _currentNode;
    }

    public override bool MoveTo(XPathNavigator other)
    {
        if (other is NamumarkNavigator navigator && navigator._document == _document)
        {
            _currentNode = navigator._currentNode;
            return true;
        }
        return false;
    }

    public override bool MoveToFirstAttribute()
    {
        throw new NotImplementedException();
    }

    public override bool MoveToFirstChild()
    {
        if (_currentNode is IParentElement parent && parent.FirstChild() != null)
        {
            _currentNode = parent.FirstChild();
            return true;
        }
        return false;
    }

    public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
    {
        return false;
    }

    public override bool MoveToId(string id)
    {
        throw new NotImplementedException();
    }

    public override bool MoveToNext()
    {
        if (_currentNode.NextSibling != null)
        {
            _currentNode = _currentNode.NextSibling;
            return true;
        }
        return false;
    }

    public override bool MoveToNextAttribute()
    {
        throw new NotImplementedException();
    }

    public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
    {
        return false;
    }

    public override bool MoveToParent()
    {
        if (_currentNode.Parent != null)
        {
            _currentNode = _currentNode.Parent;
            return true;
        }
        return false;
    }

    public override bool MoveToPrevious()
    {
        if (_currentNode.PreviousSibling != null)
        {
            _currentNode = _currentNode.PreviousSibling;
            return true;
        }
        return false;
    }
}
