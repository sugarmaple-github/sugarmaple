namespace Sugarmaple.TheSeed.Namumark.Parsing;


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

