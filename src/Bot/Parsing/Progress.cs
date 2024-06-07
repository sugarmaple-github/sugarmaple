namespace Sugarmaple.TheSeed.Namumark.Parsing;

internal class Progress
{
    public Progress(ExpressionCreator creator, StringTape tape)
    {
        Creator = creator;
        MainTape = OriginTape = tape;
    }

    public ExpressionCreator Creator { get; init; }
    public StringTape OriginTape { get; init; }
    public int InnerStart;
    public StringTape MainTape;

    public StringTape MakeTape(int index, int endIndex) => new(MainTape.Raw, index, endIndex);
}
