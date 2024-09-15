namespace Sugarmaple.TheSeed.Namumark;

using System.Collections.Generic;

public interface IParentElement : IElement
{
    IReadOnlyList<IElement> Children { get; }
}

public interface IParentElement<TChild> : IElement, IParentElement where TChild : IElement
{
    new ElementList<TChild> Children { get; }
    IReadOnlyList<IElement> IParentElement.Children => (IReadOnlyList<IElement>)(IReadOnlyList<TChild>)Children;
}

public static class ParentElementExtensions
{
    public static IElement? FirstChild(this IParentElement _this) => _this.Children.Count > 0 ? _this.Children[0] : null;

    public static IElement? FirstChild<TChild>(this IParentElement<TChild> _this) where TChild : IElement
        => _this.Children.Count > 0 ? _this.Children[0] : null;

    public static void AppendChild<TChild>(this IParentElement<TChild> _this, TChild aChild) where TChild : IElement
    {
        if (_this is Document doc)
        {
            aChild.OwnerDocument = doc;
            if (aChild is IParentElement parent)
            {
                var list = new List<IElement>();
                EnumerateNode(parent, list);
                foreach (var o in list)
                    o.OwnerDocument = doc;
            }
        }
        _this.Children.Add(aChild);
    }

    static void EnumerateNode<T>(IParentElement parent, List<T> ret)
    {
        var children = parent.Children.ToArray();
        foreach (var child in children)
        {
            if (child is T asT)
            {
                ret.Add(asT);
            }
            if (child is IParentElement childAsParent)
            {
                EnumerateNode(childAsParent, ret);
            }
        }
    }

    public static void InsertBefore<TChild>(this IParentElement<TChild> _this, TChild newNode, TChild? referenceNode) where TChild : IElement
    {
        var index = referenceNode != null ?
            _this.Children.IndexOf(referenceNode) :
            _this.Children.Count;
        _this.Children.Insert(index, newNode);
    }

    public static void RemoveChild<TChild>(this IParentElement<TChild> _this, TChild self) where TChild : IElement
    {
        _this.Children.Remove(self);
    }

    //public static void ReplaceChild(this IParentElement<TChild> _this, IElement node, IElement child)
    public static void ReplaceChild<TChild>(this IParentElement<TChild> _this, TChild node, TChild child) where TChild : IElement
    {
        var index = _this.Children.IndexOf(child);
        node.AsWeak().Parent = _this;
        child.AsWeak().Parent = null;
        _this.Children[index] = node;
    }
}