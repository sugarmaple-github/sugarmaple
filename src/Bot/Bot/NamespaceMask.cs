namespace Sugarmaple.Bot;

public readonly struct NamespaceMask
{
    public readonly int Value;

    public NamespaceMask(int value)
    {
        Value = value;
    }

    public static readonly NamespaceMask Document = new(1 << 0);
    public static readonly NamespaceMask Frame = new(1 << 1);
    public static readonly NamespaceMask Category = new(1 << 2);
    public static readonly NamespaceMask File = new(1 << 3);
    public static readonly NamespaceMask User = new(1 << 4);
    public static readonly NamespaceMask Special = new(1 << 5);
    public static readonly NamespaceMask Wiki = new(1 << 6);

    public static IReadOnlyCollection<string> DefaultNamespaces =
        new[] { "문서", "틀", "분류", "파일", "사용자", "특수기능" };

    public List<string> ToNames(string[] wikiNamespaces)
    {
        var ret = new List<string>();
        var idx = 0;
        var bit = 1;
        var compared = Value;
        while (compared != 0)
        {
            if ((compared & bit) != 0)
            {
                ret.Add(wikiNamespaces[idx]);
                compared -= bit;
            }
            bit <<= 1;
            idx++;
        }
        return ret;
    }

    public static NamespaceMask operator ~(NamespaceMask value) => new(~value.Value);
}