namespace Sugarmaple.TheSeed.Namumark;

using System.Collections.Generic;
using System.Diagnostics;

public abstract class ParentClause : Clause, IParentElement<Clause>
{
    private ElementList<Clause> _children;

    public ElementList<Clause> Children
    {
        get => _children; init
        {
            _children = value;
            _children.Parent = this;
            foreach (var o in _children)
                o.Parent = this;
        }
    }

    private protected ParentClause() => Children = new(this);
    private protected ParentClause(ElementList<Clause> children)
    {
        Children = children;
        foreach (var c in Children)
            c.Parent = this;
    }
}

/// <summary>
/// 자식과 자신의 관계가 고정된 자유 원소입니다.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
/// <typeparam name="TChild"></typeparam>
public abstract class ParentFreeElement<TSelf, TChild> : Clause, IParentElement<TChild>
    where TSelf : ParentFreeElement<TSelf, TChild>
    where TChild : IChildElement<TSelf, TChild>
{
    public ElementList<TChild> Children { get; }

    private protected ParentFreeElement() => Children = new(this);

    private protected ParentFreeElement(ElementList<TChild> children)
    {
        Children = children;
        foreach (var c in Children)
            c.Parent = (TSelf)this;
    }
}

/// <summary>
/// 이 클래스의 부모는 고정되지 않았고, 자식은 고정되었습니다. 이 클래스의 자식은 반드시 이 클래스만을 부모로 갖습니다.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
/// <typeparam name="TChild"></typeparam>
public abstract class FixedClauseParent<TSelf, TChild> : Element, IParentElement<TChild>
    where TSelf : FixedClauseParent<TSelf, TChild>
    where TChild : IChildElement<TSelf, TChild>
{
    public ElementList<TChild> Children { get; }

    private protected FixedClauseParent() => Children = new(this);

    private protected FixedClauseParent(ElementList<TChild> children)
    {
        Children = children;
        foreach (var c in Children)
            c.Parent = (TSelf)this;
    }
}


public interface IFixedClause<TParent, TSelf, TChild> : IChildElement<TParent, TSelf>, IParentElement<TChild>
    where TParent : IParentElement<TSelf>
    where TSelf : IFixedClause<TParent, TSelf, TChild>
    where TChild : IChildElement<TSelf, TChild>
{
}

public static class CollectionExtension
{
    public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
    {
        int i = 0;
        foreach (T element in self)
        {
            if (Equals(element, elementToFind))
                return i;
            i++;
        }
        return -1;
    }
}

/// <summary>
/// 특정 클래스만을 부모로 취급하는 자식의 인터페이스입니다.
/// </summary>
/// <typeparam name="TParent"></typeparam>
/// <typeparam name="TSelf"></typeparam>
public interface IChildElement<TParent, out TSelf> : IElement
    where TParent : IParentElement<TSelf>
    where TSelf : IChildElement<TParent, TSelf>
{
    new TParent? Parent { get; internal set; }
    IParentElement? IElement.Parent => Parent;
}

/// <summary>
/// 부모만이 고정된 구문입니다.
/// </summary>
/// <typeparam name="TParent"></typeparam>
/// <typeparam name="TSelf"></typeparam>
public abstract class ExclusiveParentClause<TParent, TSelf> : Element, IChildElement<TParent, TSelf>, IParentElement<Clause>
    where TParent : IParentElement<TSelf>
    where TSelf : IChildElement<TParent, TSelf>
{
    private TParent? _parent;
    private ElementList<Clause> _children;

    public ElementList<Clause> Children
    {
        get => _children; init
        {
            _children = value;
            _children.Parent = this;
            foreach (var o in _children)
                o.Parent = this;
        }
    }
    public new TParent Parent { get => _parent; set => _parent = value; }

    private protected ExclusiveParentClause() => Children = new(this);
    private protected ExclusiveParentClause(ElementList<Clause> children)
    {
        Children = children;
        foreach (var c in Children)
            c.Parent = this;
    }

    protected override IParentElement? ParentInner { get => _parent; set => _parent = (TParent)value; }
}

public abstract class FixedElement<TParent, TSelf, TChild>
    : FixedClauseParent<TSelf, TChild>, IFixedClause<TParent, TSelf, TChild>
    where TParent : IParentElement<TSelf>
    where TSelf : FixedElement<TParent, TSelf, TChild>
    where TChild : IChildElement<TSelf, TChild>
{
    private TParent? _parent;
    [Obsolete]
    private bool _hasModified;
    [Obsolete]
    internal bool HasModified { get => _hasModified; set => _hasModified = value; }

    public void ReplaceWith(TSelf clause)
    {
        if (_parent == null)
            throw new Exception();
        _parent.ReplaceChild(clause, (TSelf)this);
    }

    //[Obsolete]
    //public string OuterMarkup => _hasModified ? ToMarkup() : _rawSource;
    public TSelf? PreviousSibling
    {
        get
        {
            var children = _parent.Children;
            var index = children.IndexOf((TSelf)this);
            return 0 <= index - 1 ? children[index - 1] : null;
        }
    }
    public TSelf? NextSibling
    {
        get
        {
            var children = _parent.Children;
            var index = children.IndexOf((TSelf)this);
            return index + 1 < children.Count ? children[index + 1] : null;
        }
    }

    TParent? IChildElement<TParent, TSelf>.Parent { get => _parent; set => _parent = value; }

    protected override IParentElement? ParentInner { get => _parent; set => _parent = (TParent)value; }

    public void Remove()
    {
        _parent?.RemoveChild((TSelf)this);
        _parent = default;
    }
}