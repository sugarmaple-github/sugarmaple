namespace Sugarmaple.TheSeed.Namumark.Parsing;

using System.Text.RegularExpressions;

internal static class RegexExtensions
{
    public static ASTNode ToToken(this Group group, ASTNodeType type = ASTNodeType.None)
    {
        return new ASTNode { Index = group.Index, Length = group.Length, Type = type };
    }
}

internal abstract class Processor
{
    public abstract string Open { get; }

    private Progress state;

    public ASTNode Process(Progress state)
    {
        this.state = state;
        //state.MainTape = state.OriginTape.CheckAndUse();
        state.MainTape.Progress(Open.Length);
        state.InnerStart = state.MainTape.Index;
        return ProcessInternal(state);
    }

    public bool TryProcess(Progress state, out ASTNode token)
    {
        token = Process(state);
        return token.Length > 0;
    }

    protected abstract ASTNode ProcessInternal(Progress state);

    protected StringTape ToTape(Group g, StringTape parent = null) => new(state.OriginTape.Raw, g.Index, g.Index + g.Length, parent);
}

