namespace Sugarmaple.TheSeed.Api;

using Sugarmaple.Text;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;


/// <summary>
/// 기본 API(https://doc.theseed.io/)에서 제공하는 기능들만 제공하는 클라이언트입니다.
/// </summary>
public class SeedApiClient : ISeedApiClient
{
    private readonly JsonClient _client;

    public event Action<Option<ViewResponse>>? GotEdit;
    public event Action<string, EditGetError>? GotEditError;
    public event Action<string, string>? GotEditSuccessfully;
    public event Action<EditPostResult>? PostedSuccessfully;
    public event Action<EditPostError>? PostedEditError;
    public event Action<string>? GotBacklinkError;
    public event Action<string, Option<BacklinkResponse>>? GotBacklink;

    public event Func<string, string, string>? ApiPosting;

    public string WikiUri { get; }

    /// <summary>
    /// Client 객체를 생성합니다.
    /// </summary>
    /// <param name="wikiUri">접근하고자 하는 위키의 Uri를 작성합니다.</param>
    /// <param name="apiToken">Api Token을 작성합니다.</param>
    public SeedApiClient(string wikiUri, string apiToken)
    {
        WikiUri = wikiUri;
        _client = new($"{wikiUri}/api/");
        _client.UpdateAuthHeader($"Bearer {apiToken}");
    }

    #region Public Method
    /// <summary>
    /// 문서를 열람하고 편집 뷰를 반환합니다.
    /// </summary>
    /// <param name="document">편집할 문서명입니다.</param>
    /// <returns>편집을 시행할 수 있는 뷰를 반환합니다.</returns>
    public async Task<Option<ViewResponse>> GetEditAsync(string document)
    {
        var output = await _client.GetAsync<ViewResponse>($"edit/{document}");
        if (output.TryGetValue(out var item))
        {
            (string text, bool exists, string token) = item;
            GotEditSuccessfully?.Invoke(document, text);
        }
        else
            GotEditError?.Invoke(document, new(output.Error));
        return output;
    }

    /// <summary>
    /// https://doc.theseed.io/#d17f65c0ee
    /// </summary>
    /// <param name="document">보내는 문서입니다.</param>
    /// <param name="text">편집 내용</param>
    /// <param name="log">편집 요약</param>
    /// <param name="token">편집 토큰</param>
    /// <returns></returns>
    public async Task<Option<EditResponse>> PostEditAsync(string document, string text, string log, string token)
    {
        var intercepted = ApiPosting?.Invoke(document, text) ?? text;
        var output = await _client.PostAsync<EditResponse, EditParameter>($"edit/{document}", new(text, log, token));
        if (output.TryGetValue(out var item))
        {
            (var status, var rev) = item;
            if (status == "success")
            {
                PostedSuccessfully?.Invoke(new(document, rev));
            }
            else
                PostedEditError?.Invoke(new(status, true, document));
        }
        else
            PostedEditError?.Invoke(new(output.Error, false, document));
        return output;
    }

    /// <summary>
    /// <paramref name="document"/>의 역링크 중에서 <paramref name="namespace" /> 이름공간에 있는 것을 반환합니다.
    /// </summary>
    /// <param name="document">역링크 목록을 확인할 문서명입니다.</param>
    /// <param name="namespace">역링크 목록을 확인할 이름 공간입니다. 만약 해당 이름 공간의 역링크가 없을 경우, 문서 이름공간의 역링크를 출력합니다.</param>
    /// <param name="from">어떤 문자열부터의 역링크 목록을 확인할 것인지 반환합니다.</param>
    /// <param name="flag">역링크의 타입을 정합니다.</param>
    /// <returns>역링크의 결과 객체를 반환합니다.</returns>
    /// <inheritdoc cref="GetBacklinkFromAsync(string, string, string, BacklinkFlags)"/>
    public async Task<Option<BacklinkResponse>> GetBacklinkFromAsync(string document, string @namespace = "", string @from = "", BacklinkFlags flag = BacklinkFlags.All)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(@namespace);
        ArgumentNullException.ThrowIfNull(@from);

        var queryStr = GetQueryCollection(new() {
                { nameof(@namespace),   @namespace              },
                { nameof(@from),        @from                   },
                { nameof(flag),         ((int)flag).ToString()  }});
        var output = await _client.GetAsync<BacklinkResponse>($"backlink/{document}?{queryStr!}");
        GotBacklink?.Invoke(document, output);
        return output;
    }

    /// <summary>
    /// <paramref name="document"/>의 역링크 중에서 <paramref name="namespace" /> 이름공간에 있는 것을 반환합니다.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="namespace"></param>
    /// <param name="until"></param>
    /// <param name="flag"></param>
    /// <returns></returns>
    public async Task<Option<BacklinkResponse>> GetBacklinkUntilAsync(string document, string @namespace, string until = "", BacklinkFlags flag = BacklinkFlags.All)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(@namespace);
        ArgumentNullException.ThrowIfNull(until);

        var queryStr = GetQueryCollection(new() {
                { nameof(@namespace),   @namespace              },
                { nameof(until),        until                   },
                { nameof(flag),         ((int)flag).ToString()  }});
        var output = await _client.GetAsync<BacklinkResponse>($"backlink/{document}?{queryStr}");
        return output;
    }

    private static NameValueCollection GetQueryCollection(NameValueCollection queries)
    {
        var queryStr = System.Web.HttpUtility.ParseQueryString("");
        queryStr.Add(queries);
        return queryStr;
    }
    #endregion
}

public interface ISeedApiClient
{
    event Action<string, string>? GotEditSuccessfully;

    public Task<Option<ViewResponse>> GetEditAsync(string document);
    public Task<Option<BacklinkResponse>> GetBacklinkFromAsync(string document, string @namespace = "", string @from = "", BacklinkFlags flags = BacklinkFlags.All);
    public Task<Option<EditResponse>> PostEditAsync(string document, string text, string log, string token);
}

public static class SeedApiClientExtensions
{
    private static IAsyncEnumerable<BacklinkPair> GetBacklinkOne(this ISeedApiClient self, string document, string @namespace, string from)
    {
        return Inner(from);

        async IAsyncEnumerable<BacklinkPair> Inner(string? from)
        {
            if (from == null) yield break;
            var backlinkOpt = await self.GetBacklinkFromAsync(document, @namespace, from);
            if (backlinkOpt.Item == null) yield break;
            await foreach (var o in backlinkOpt.Item.Backlinks.ToAsyncEnumerable().Concat(Inner(backlinkOpt.Item.From)))
                yield return o;
        }
    }
    //값만 바꾸고 반복해서 구현

    public async static IAsyncEnumerable<BacklinkPair> GetBacklinksAsync(this ISeedApiClient self, string document, string from, IEnumerable<string> denialNamespaces)
    {
        var firstOpt = await self.GetBacklinkFromAsync(document, "", from);
        if (firstOpt.Item == null) yield break;
        var first = firstOpt.Item;
        var namespaces = first.Namespaces;
        if (namespaces.Length == 0) yield break;

        var ret = Enumerable.Empty<BacklinkPair>().ToAsyncEnumerable();
        if (!denialNamespaces.Contains(namespaces[0].Namespace))
        {
            ret = ret.Concat(first.Backlinks.ToAsyncEnumerable());
            if (first.From != null)
            {
                ret = ret.Concat(self.GetBacklinkOne(document, namespaces[0].Namespace, first.From));
            }
        }

        ret = ret.Concat(
            namespaces.Skip(1).ExceptBy(denialNamespaces, o => o.Namespace).ToAsyncEnumerable()
            .SelectMany(o => self.GetBacklinkOne(document, o.Namespace, "")));

        await foreach (var o in ret)
            yield return o;
    }
}



public class EditPostError
{
    public readonly string Msg;
    public readonly bool IsJson;
    public readonly string Document;

    public EditPostError(string msg, bool isJson, string document)
    {
        Msg = msg;
        IsJson = isJson;
        Document = document;
    }

    public bool HasSameDocumentContent => Msg == "문서 내용이 같습니다.";
    public bool HasEditConflict => Msg == "편집 도중에 다른 사용자가 먼저 편집을 했습니다.";
    public bool InvalidRequestBody => Msg == "invalid request body";

}
