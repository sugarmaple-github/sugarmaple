namespace Sugarmaple.TheSeed.Api;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public record struct EditPostResult(string Document, int Rev);
public class SeedApiClient : ISeedApiClient
{
    private readonly JsonClient _client;

    public event Action<EditGetError>? OnGetEditError;
    public event Action<EditPostError>? OnPostEditError;
    public event Action<string>? OnBacklinkError;
    public event Action<string, string>? OnGetEditSuccessfully;
    public event Action<BacklinkResult>? OnBacklink;
    public EventPublisher<EditPostResult> OnPostSuccessfully { get; } = new();
    public event Func<string, string, string>? BeforeEveryPost;
    public string WikiUri { get; }

    public event Action<string>? OnError
    {
        add => _client.OnError += value;
        remove => _client.OnError -= value;
    }

    internal SeedApiClient(string wikiUri)
    {
        WikiUri = wikiUri;
        _client = new(wikiUri);
        _client.UpdateAuthHeader($"Bearer abc");
    }

    /// <summary>
    /// Client 객체를 생성합니다.
    /// </summary>
    /// <param name="wikiUri">접근하고자 하는 위키의 Uri를 작성합니다.</param>
    /// <param name="apiToken">Api Token을 작성합니다.</param>
    public SeedApiClient(string wikiUri, string apiToken) : this(wikiUri)
    {
        UpdateApiToken(apiToken);
    }

    #region Public Method

    /// <summary>
    /// 문서를 열람하고 편집 뷰를 반환합니다.
    /// </summary>
    /// <param name="document">편집할 문서명입니다.</param>
    /// <returns>편집을 시행할 수 있는 뷰를 반환합니다.</returns>
    /// <inheritdoc cref="GuardDocument(string?)"/>
    /// <inheritdoc cref="GuardStatus"/>
    public Task<EditView?> GetEditAsync(string document)
    {
        GuardDocument(document);
        return GetEditAsync_optIn(document);
    }

    private async Task<EditView?> GetEditAsync_optIn(string document)
    {
        var output = await _client.GetEditAsync(document);
        if (output.TryGetValue(out var item))
        {
            (string text, bool exists, string token, string status) = item;
            if (status == null)
            {
                OnGetEditSuccessfully?.Invoke(document, text);
                return new EditView(this, document, text, exists, token);
            }
            OnGetEditError?.Invoke(new(status, true) { Document = document });
            return null;
        }
        OnGetEditError?.Invoke(new(output.Error, true));
        return null;
    }

    /// <summary>
    /// <paramref name="document"/>의 역링크 중에서 <paramref name="namespace" /> 이름공간에 있는 것을 반환합니다.
    /// </summary>
    /// <param name="document">역링크 목록을 확인할 문서명입니다.</param>
    /// <param name="namespace">역링크 목록을 확인할 이름 공간입니다. 만약 해당 이름 공간의 역링크가 없을 경우, 문서 이름공간의 역링크를 출력합니다.</param>
    /// <param name="from">어떤 문자열부터의 역링크 목록을 확인할 것인지 반환합니다.</param>
    /// <param name="flags">역링크의 타입을 정합니다.</param>
    /// <returns>역링크의 결과 객체를 반환합니다.</returns>
    /// <inheritdoc cref="GuardStatus"/>
    /// <inheritdoc cref="GetBacklinkFromAsync(string, string, string, BacklinkFlags)"/>
    public Task<BacklinkResult?> GetBacklinkFromAsync(string document, string @namespace = "", string @from = "", BacklinkFlags flags = BacklinkFlags.All)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(@namespace);
        ArgumentNullException.ThrowIfNull(@from);
        return GetBacklinkFromAsyncIn(document, @namespace, from, flags);

        async Task<BacklinkResult?> GetBacklinkFromAsyncIn(string document, string @namespace = "", string @from = "", BacklinkFlags flags = BacklinkFlags.All)
        {
            var output = await _client.GetBacklinkFromAsync(document, @namespace, from, (int)flags);
            if (output.TryGetValue(out var item))
            {
                GuardStatus(item.Status, nameof(document));
                var ret = new BacklinkResult(_client, document, @namespace, flags, item);
                OnBacklink?.Invoke(ret);
                return ret;
            }
            OnBacklinkError?.Invoke(output.Error);
            return null;
        }
    }

    public Task<EditReport?> PostEditAsync(string document, string text, string log, string token) =>
        PostEditAsync_In(document, text, log, token);

    private async Task<EditReport?> PostEditAsync_In(string document, string text, string log, string token)
    {
        var intercepted = BeforeEveryPost?.Invoke(document, text) ?? text;
        var output = await _client.PostEditAsync(document, intercepted, log, token);
        if (output.TryGetValue(out var item))
        {
            (var status, var rev) = item;
            if (status == "success")
            {
                OnPostSuccessfully?.Invoke(new(document, rev));
                return new(rev);
            }
            OnPostEditError?.Invoke(new(status, true));
            return null;
        }
        OnPostEditError?.Invoke(new(output.Error, false));
        return null;
    }



    /// <summary>
    /// <paramref name="document"/>의 역링크 중에서 <paramref name="namespace" /> 이름공간에 있는 것을 반환합니다.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="namespace"></param>
    /// <param name="until"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public async Task<BacklinkResult> GetBacklinkUntilAsync(string document, string @namespace, string until = "", BacklinkFlags flags = BacklinkFlags.All)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(@namespace);
        ArgumentNullException.ThrowIfNull(until);

        var output = await _client.GetBacklinkUntilAsync(document, @namespace, until, (int)flags);
        if (output.TryGetValue(out var item))
        {
            GuardStatus(item.Status, nameof(document));
            var ret = new BacklinkResult(_client, document, @namespace, flags, item);
            return ret;
        }
        return null;
    }
    internal Task<BacklinkResult> GetBacklinkUntilAsync(string document, SeedNamespace @namespace, string until = "", BacklinkFlags flags = BacklinkFlags.All) =>
        GetBacklinkUntilAsync(document, @namespace.Name, until, flags);

    /// <summary>
    /// API Token을 갱신합니다.
    /// </summary>
    /// <param name="apiToken">갱신할 API 토큰.</param>
    public void UpdateApiToken(string apiToken)
    {
        foreach (var c in apiToken)
        {
            if (!char.IsAscii(c))
                throw new ArgumentException($"{nameof(apiToken)} must contain only ASCII characters.", nameof(apiToken));
        }
        _client.UpdateAuthHeader($"Bearer {apiToken}");
    }
    #endregion

    /// <exception cref="ArgumentException"><paramref name="document"/>가 빈 문자열이거나 길이가 255를 넘습니다.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="document"/>가 null입니다.</exception>
    private static void GuardDocument(string? document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Length == 0)
            throw new ArgumentException("The name of document can't be null or white space.", nameof(document));

        if (document.Length > 255)
            throw new ArgumentException("The name of document can't be over 255", nameof(document));
    }

    /// <exception cref="InvalidApiTokenException">Api Token이 유효하지 않습니다.</exception>
    /// <exception cref="InvalidDocumentException">이 위키에서 유효하지 않는 문서명입니다.</exception>
    /// <exception cref="AccessLevelLacksException">접근 권한이 부족합니다.</exception>
    private static void GuardStatus(string? status, string paramNameForDoc)
    {
        if (status == null) return;
        //if (status == "권한이 부족합니다.")
        //    throw new InvalidApiTokenException($"Api token is not valid.", status);
        if (status == "문서 이름이 올바르지 않습니다.")
            throw new InvalidDocumentException(paramNameForDoc, status);
        //if (status.StartsWith("편집"))
        //    throw new AccessLevelLacksException(status);
    }
}

public class EventPublisher<T>
{
    public event Action<T> Event;

    public void Invoke(T args)
    {
        Event?.Invoke(args);
    }
}

public interface ISeedApiClient
{
    event Action<string, string>? OnGetEditSuccessfully;

    public Task<EditView?> GetEditAsync(string document);
    public Task<BacklinkResult?> GetBacklinkFromAsync(string document, string @namespace = "", string @from = "", BacklinkFlags flags = BacklinkFlags.All);
    public Task<EditReport?> PostEditAsync(string document, string text, string log, string token);
}

public static class SeedApiClientExtensions
{
    private static IEnumerable<BacklinkPair> GetBacklinkOne(this ISeedApiClient self, string document, string @namespace, string from)
    {
        var backlink = self.GetBacklinkFromAsync(document, @namespace, from).Result;
        do
        {
            foreach (var item in backlink.Backlinks)
            {
                yield return item;
            }
        } while (backlink.TryGetNext(out backlink));
    }

    public static IEnumerable<BacklinkPair> GetBacklinks(this ISeedApiClient self, string document, string from, IEnumerable<string> denialNamespaces)
    {
        var first = self.GetBacklinkFromAsync(document, "", from).Result;
        var namespaces = first.Namespaces;
        var ret = Enumerable.Empty<BacklinkPair>();
        if (namespaces.Count == 0)
            return ret;
        if (!denialNamespaces.Contains(namespaces[0].Namespace))
            ret = ret.Concat(first.Backlinks);
        ret = ret.Concat(
                namespaces.Skip(1).ExceptBy(denialNamespaces, o => o.Namespace).SelectMany(o => self.GetBacklinkOne(document, o.Namespace, ""))
            );
        return ret;
    }
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

public class EditPostError
{
    public readonly string Msg;
    public readonly bool IsJson;

    public EditPostError(string msg, bool isJson)
    {
        Msg = msg;
        IsJson = isJson;
    }

    public bool HasSameDocumentContent => Msg == "문서 내용이 같습니다.";
    public bool HasEditConflict => Msg == "편집 도중에 다른 사용자가 먼저 편집을 했습니다.";
    public bool InvalidRequestBody => Msg == "invalid request body";

}

internal class TaskDelayProcessor
{
    private readonly Thread _thread;
    private readonly Stopwatch _stopwatch = new();
    private readonly BlockingCollection<Task> _queue = new();

    public TaskDelayProcessor()
    {
        _thread = new(Operate);
        _thread.Start();
    }

    private void Operate()
    {
        while (true)
        {
            _stopwatch.Restart();
            var task = _queue.Take();
            task.Start();
            task.Wait();
            _stopwatch.Stop();

            var sleepTime = TimeSpan.FromSeconds(1) - _stopwatch.Elapsed;
            if (sleepTime > TimeSpan.Zero)
                Thread.Sleep(sleepTime);
        }
    }

    internal Task<T> Enqueue<T>(Task<T> task)
    {
        var queueTask = task.ContinueWith(t => t.Result);
        _queue.Add(task);
        return queueTask;
    }
}