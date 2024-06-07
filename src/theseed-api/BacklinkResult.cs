namespace Sugarmaple.TheSeed.Api;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

public class BacklinkResult
{
    private readonly JsonClient _client;
    private readonly NamespaceCountPair[] _namespaceCountPairs;
    private readonly BacklinkPair[] _backlinkPairs;

    private NamespaceCountPairCollection? _namespaces;
    public NamespaceCountPairCollection Namespaces => _namespaces ??= new(_namespaceCountPairs);

    private BacklinkPairCollection? _backlinks;
    public BacklinkPairCollection Backlinks => _backlinks ??= new(_backlinkPairs);

    private string? _namespace;

    public string Document { get; }
    public BacklinkFlags Flags { get; }


    internal BacklinkResult(JsonClient client, string document, string @namespace, BacklinkFlags flags,
        BacklinkResponse result)
    {
        _client = client;
        Document = document;
        Flags = flags;

        (var resNamespace, var backlinks, var from, var until, _) = result;
        _namespace = @namespace;
        _namespaceCountPairs = resNamespace;
        _backlinkPairs = backlinks;
        From = from;
        Until = until;
    }

    public string? From { get; }
    public string? Until { get; }

    public async Task<BacklinkResult> GetPrevAsync()
    {
        if (Until == null)
            throw new InvalidOperationException("There aren't previous item. (Until == null)");
        var result = await _client.GetBacklinkUntilAsync(Document, _namespace, Until, (int)Flags);
        if (result.TryGetValue(out var item))
        {
            var ret = new BacklinkResult(_client, Document, _namespace, Flags, item);
            return ret;
        }
        return null;
    }

    public async Task<BacklinkResult> GetNextAsync()
    {
        if (From == null)
            throw new InvalidOperationException("There aren't next item. (From == null)");
        var result = await _client.GetBacklinkFromAsync(Document, _namespace, From, (int)Flags);
        if (result.TryGetValue(out var item))
        {
            var ret = new BacklinkResult(_client, Document, _namespace, Flags, item);
            return ret;
        }
        return null;
    }

    public bool TryGetNext([NotNullWhen(true)] out BacklinkResult? result)
    {
        if (From == null)
        {
            result = null;
            return false;
        }
        result = GetNextAsync().Result;
        return true;
    }

    public bool TryGetNextAsync([NotNullWhen(true)] out Task<BacklinkResult>? task)
    {
        if (From == null)
        {
            task = null;
            return false;
        }
        task = GetNextAsync();
        return true;
    }

    private const string DefaultNamespace = "문서";

    private string GetNamespace()
    {
        if (_backlinkPairs.Length == 0)
            return DefaultNamespace;
        var document = _backlinkPairs[0].Document;
        var index = document.IndexOf(':');
        if (index == -1)
            return DefaultNamespace;

        var @namespace = document.AsSpan(0, index);
        foreach ((var curNamespace, _) in _namespaceCountPairs)
        {
            if (@namespace.Equals(curNamespace, StringComparison.Ordinal))
                return new(curNamespace);
        }
        return DefaultNamespace;
    }
}

/// <summary>
/// 역링크와 참조 방식 짝의 리스트입니다.
/// </summary>
public class BacklinkPairCollection : IReadOnlyList<BacklinkPair>
{
    private BacklinkPair[] _backlinkPairs;

    internal BacklinkPairCollection(BacklinkPair[] backlinkPairs)
    {
        _backlinkPairs = backlinkPairs;
    }

    /// <summary>
    /// 정수 인덱스로 역링크에 접근합니다.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public BacklinkPair this[int index] => ((IReadOnlyList<BacklinkPair>)_backlinkPairs)[index];

    /// <summary>
    /// 
    /// </summary>
    public int Count => ((IReadOnlyCollection<BacklinkPair>)_backlinkPairs).Count;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _backlinkPairs.GetEnumerator();
    }

    public IEnumerator<BacklinkPair> GetEnumerator()
    {
        return ((IEnumerable<BacklinkPair>)_backlinkPairs).GetEnumerator();
    }
}

public class NamespaceCountPairCollection : IReadOnlyList<NamespaceCountPair>
{
    private readonly NamespaceCountPair[] _namespaceCountPairs;

    public int Count => ((IReadOnlyCollection<NamespaceCountPair>)_namespaceCountPairs).Count;

    public NamespaceCountPair this[int index] => _namespaceCountPairs[index];

    internal NamespaceCountPairCollection(NamespaceCountPair[] namespaceCountPairs)
    {
        _namespaceCountPairs = namespaceCountPairs;
    }

    public IEnumerator<NamespaceCountPair> GetEnumerator()
    {
        return ((IEnumerable<NamespaceCountPair>)_namespaceCountPairs).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _namespaceCountPairs.GetEnumerator();
    }
}

internal readonly struct SeedNamespace
{
    public readonly string Name;

    public readonly static SeedNamespace
        Document = new("문서"),
        Frame = new("틀"),
        Category = new("분류"),
        File = new("파일"),
        User = new("사용자"),
        Special = new("특수기능"),
        Discussion = new("토론"),
        TrashCan = new("휴지통"),
        Vote = new("투표"),
        System = new("시스템");

    internal SeedNamespace(string name)
    {
        Name = name;
    }

    public string Join(string title) => $"{Name}:{title}";

    public override string ToString()
    {
        return Name;
    }
}
