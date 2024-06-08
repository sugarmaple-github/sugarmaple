namespace Sugarmaple.TheSeed.Namumark;

public class NamuNormalizer
{
    public static NamuNormalizer Default { get; } = new();

    public void Normalize(IElement element)
    {
        switch (element)
        {
            case FileLink o:
                o.Reference = "파일:" + o.Reference[3..].Trim();
                break;
        }
        if (element is IParentElement parent)
            foreach (var o in parent.Children)
                Normalize(o);
    }
}