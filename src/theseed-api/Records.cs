namespace Sugarmaple.TheSeed.Api;
using Sugarmaple.Text;
using System.Diagnostics.CodeAnalysis;

internal record struct EditParameter(string Text, string Log, string Token);

public record BacklinkResponse(
    NamespaceCountPair[] Namespaces,
    BacklinkPair[] Backlinks,
    string From,
    string Until,
    string? Status);

/// <summary>
/// 이름공간과 그 수입니다.
/// </summary>
/// <param name="Namespace"></param>
/// <param name="Count"></param>
public record struct NamespaceCountPair(string Namespace, int Count);
/// <summary>
/// 역링크 검색 결과로 반환된 단일 역링크 항목입니다.
/// </summary>
/// <param name="Document"></param>
/// <param name="Flags"></param>
public record struct BacklinkPair(string Document, string Flags);
public record EditResponse(string Status, int Rev);
public record ViewResponse(string Text, bool Exists, string Token, string Status);

public class EditGetError
{
    string _data;
    bool _isJson;

    public EditGetError(string data, bool isJson)
    {
        _data = data;
        _isJson = isJson;
    }

    public string Data => _data;
    public bool IsJson => _isJson;
    public string Document { get; init; }

    public bool IsLackOfPermission => _data.StartsWith("편집 권한이 부족합니다.");
}

/// <summary>
/// 역링크 검색 옵션을 지정합니다.
/// </summary>
[Flags]
public enum BacklinkFlags : byte
{
    /// <summary>
    /// 모든 결과를 봅니다.
    /// </summary>
    All = 0,
    /// <summary>
    /// 링크된 역링크만 검색합니다.
    /// </summary>
    Link = 1,
    /// <summary>
    /// 파일로서 참조하는 역링크만 검색합니다.
    /// </summary>
    File = 2,
    /// <summary>
    /// include된 역링크만 검색합니다.
    /// </summary>
    Include = 4,
    /// <summary>
    /// 리다이렉트하는 역링크만 검색합니다.
    /// </summary>
    Redirect = 8,
}

public readonly struct Option<TOut>
{
    public TOut? Item { get; } = default;
    private readonly string? _error;
    public string Error => _error;

    public bool TryGetValue([NotNullWhen(true)] out TOut? then)
    {
        var ret = _error == null;
        then = Item;
        return ret;
    }

    public bool TryGetError([NotNullWhen(true)] out string? then)
    {
        var ret = _error != null;
        then = _error;
        return ret;
    }

    public Option(TOut item)
    {
        Item = item;
        _error = null;
    }

    public Option(string error)
    {
        _error = error;
    }
}