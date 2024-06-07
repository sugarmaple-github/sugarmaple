namespace Sugarmaple.TheSeed.Namumark;
using System.Diagnostics;

[DebuggerDisplay($"{{{nameof(OuterMarkup)}}}")]
public abstract class Clause : ChildElement<IParentElement<Clause>, Clause>
{
}

public abstract class ChildElement<TParent, TSelf> : Element, IChildElement<TParent, TSelf>
    where TParent : IParentElement<TSelf>
    where TSelf : ChildElement<TParent, TSelf>
{
    private TParent? _parent;

    public void ReplaceWith(TSelf clause)
    {
        if (Parent == null)
            throw new Exception();
        Parent.ReplaceChild(clause, (TSelf)this);
    }

    public void Remove()
    {
        Parent?.RemoveChild((TSelf)this);
    }

    public TSelf? PreviousSibling
    {
        get
        {
            var children = Parent.Children;
            var index = children.IndexOf((TSelf)this);
            return 0 <= index - 1 ? children[index - 1] : null;
        }
    }
    public TSelf? NextSibling
    {
        get
        {
            var children = Parent.Children;
            var index = children.IndexOf((TSelf)this);
            return index + 1 < children.Count ? children[index + 1] : null;
        }
    }

    public new TParent? Parent { get => _parent; set => _parent = value; }

    protected sealed override IParentElement? ParentInner { get => _parent; set => _parent = (TParent)value; }

    //public override Element? Parent { get => this.Parent; set => this.Parent; }
}

public interface IElement
{
    //we need to make it not set
    IParentElement? Parent { get; }

    IElement? PreviousSibling
    {
        get
        {
            if (Parent == null) return null;
            var children = Parent.Children;
            var index = children.IndexOf(this);
            return 0 <= index - 1 ? children[index - 1] : null;
        }
    }

    IElement? NextSibling
    {
        get
        {
            if (Parent == null) return null;
            var children = Parent.Children;
            var index = children.IndexOf(this);
            return index + 1 < children.Count ? children[index + 1] : null;
        }
    }

    string LocalName { get; }
    Document? OwnerDocument { get; set; }
    string OuterMarkup { get; }
    void Normalize();
}