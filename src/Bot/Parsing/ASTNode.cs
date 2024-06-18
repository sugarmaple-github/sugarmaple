namespace Sugarmaple.TheSeed.Namumark.Parsing;
using Sugarmaple.TheSeed.Namumark;
using System.Collections;

internal record struct ASTNode : IEnumerable<ASTNode>
{
    public ASTNodeType Type;
    public int Index;
    public int Length;
    public List<ASTNode> Children = new();

    public int End => Index + Length;

    public bool IsValid => Length > 0;

    public ASTNode(Element? Element)
    {
    }

    public ASTNode() : this(null)
    {

    }
    public ASTNode(ASTNodeType Type, int Index, int Length) : this()
    {
        this.Type = Type;
        this.Index = Index;
        this.Length = Length;
        Children = new();
    }

    public void Add(ASTNode child) => Children.Add(child);

    public ASTNode(ASTNodeType Type, int Index, int Length, List<ASTNode> Children) : this()
    {
        this.Type = Type;
        this.Index = Index;
        this.Length = Length;
        this.Children = Children;
    }

    public override string ToString() => $"({Type}), i:{Index}, l:{Length}, {
        (NamumarkProcess._recentOne != null ? $"res:\"{NamumarkProcess._recentOne.Substring(Index, Length)}\""  :"")}";

    public IEnumerator<ASTNode> GetEnumerator()
    {
        return ((IEnumerable<ASTNode>)Children).GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Children).GetEnumerator();
    }
}
