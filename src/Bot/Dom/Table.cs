namespace Sugarmaple.TheSeed.Namumark;

using System.Text;

public class Table : ParentFreeElement<Table, TableRow>
{
    string tablewidth;
    FileAlign tablealign;
    string tablebordercolor;
    string tablebgcolor;
}

public class TableRow : FixedElement<Table, TableRow, TableData>
{
    public void RemoveSucceeding()
    {
        string? tableWidth = null;
        string? tableAlign = null;
        string? tableBgColor = null;
        string? tableBorderColor = null;
        string? colColor = null;
        string? colBgColor = null;

        foreach (TableData o in Children)
        {
            tableWidth ??= o.TableWidth;
            tableAlign ??= o.TableAlign;
            tableBgColor ??= o.TableBgColor;
            tableBorderColor ??= o.TableBorderColor;
            colColor ??= o.ColColor;
            colBgColor ??= o.ColBgColor;
        }

        var successor = NextSibling;
        if (successor != null)
        {
            var td = successor.Children[0];
            td.TableWidth ??= tableWidth;
            td.TableAlign ??= tableAlign;
            td.TableBgColor ??= tableBgColor;
            td.TableBorderColor ??= tableBorderColor;
            td.ColColor ??= colColor;
            td.ColBgColor ??= colBgColor;
        }
        Remove();
    }
}

public class TableAttribute
{
    private readonly Dictionary<string, string> _dict = new();
    private int _rowSpan = 1;
    private int _colSpan = 1;

    public void Add(string key, string value)
    {
        _dict.TryAdd(key, value);
    }

    public string ToMarkup()
    {
        var sb = new StringBuilder();
        if (_rowSpan > 1)
            sb.Append($"<-{_rowSpan}>");
        if (_colSpan > 1)
            sb.Append($"<|{_colSpan}>");
        return sb.ToString();
    }

    public string OuterMarkup
    {
        get
        {
            if (MarkupRawCache.TryGetValue(this, out var cached))
                return cached;
            var ret = ToMarkup();
            MarkupRawCache.Add(this, ret);
            return ret;
        }
    }
}

public class TableData : ExclusiveParentClause<TableRow, TableData>
{
    private int _rowSpan = 1;
    private int _colSpan = 1;

    private string? _bgColor;
    private string? _tableWidth;
    private string? _tableAlign;
    private string? _tableBgColor;
    private string? _tableBorderColor;
    private string? _width;
    private string? _height;
    private string? _color;
    private string? _colColor;
    private string? _colBgColor;

    public int RowSpan { get => _rowSpan; set => ChangeMember(ref _rowSpan, value); }
    public int ColSpan { get => _colSpan; set => ChangeMember(ref _colSpan, value); }
    public string? TableWidth { get => _tableWidth; internal set => ChangeMember(ref _tableWidth, value); }
    public string? TableAlign { get => _tableAlign; internal set => ChangeMember(ref _tableAlign, value); }
    public string? TableBgColor { get => _tableBgColor; internal set => ChangeMember(ref _tableBgColor, value); }
    public string? TableBorderColor { get => _tableBorderColor; internal set => ChangeMember(ref _tableBorderColor, value); }
    public string? Width { get => _width; internal set => ChangeMember(ref _width, value); }
    public string? Height { get => _height; internal set => ChangeMember(ref _height, value); }
    public string? BgColor { get => _bgColor; internal set => ChangeMember(ref _bgColor, value); }
    public string? Color { get => _color; internal set => ChangeMember(ref _color, value); }
    public string? ColColor { get => _colColor; internal set => ChangeMember(ref _colColor, value); }
    public string? ColBgColor { get => _colBgColor; internal set => ChangeMember(ref _colBgColor, value); }
    public TableAttribute Attributes { get; internal init; } = new();
}