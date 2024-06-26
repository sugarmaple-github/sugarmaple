﻿namespace Sugarmaple.TheSeed.Api;
using System;

/// <summary>
/// 제공받은 편집 토큰과 클라이언트의 편집 기능을 관리하는 객체입니다.
/// </summary>
public class EditView
{
    private readonly SeedApiClient _client;
    public readonly string Document;
    private string? _token;

    internal EditView(SeedApiClient client,
        string document, string text, bool exists, string token)
    {
        _client = client;
        Document = document;
        Text = text;
        Exist = exists;
        _token = token;
    }

    /// <summary>
    /// 열람한 문서의 내용입니다.
    /// </summary>
    public string Text { get; private set; }
    /// <summary>
    /// 해당 문서의 존재 여부입니다.
    /// </summary>
    public bool Exist { get; private set; }

    public string Token => _token;

    /// <summary>
    /// 기존 문서를 재열람합니다. text와 token이 갱신됩니다.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidApiTokenException"></exception>
    internal async Task GetReviewAsync()
    {
        throw new NotImplementedException();
        //var output = await _client.GetEditAsync(Document);
        //if (output.TryGetValue(out var item))
        //{
        //    (string text, bool exists, string token, string status) = item;
        //    if (status == "문서 이름이 올바르지 않습니다.")
        //        throw new InvalidApiTokenException();

        //    Text = text;
        //    Exist = exists;
        //    _token = token;
        //}
    }

    /// <summary>
    /// 편집을 제출하고 토큰을 파기합니다. 같은 문서를 추가로 편집을 위해서는 Review를 호출해야 합니다.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="log"></param>
    /// <returns>편집 정보를 반환합니다.</returns>
    /// <exception cref="ArgumentNullException">매개변수에 null값이 제공될 경우.</exception>
    public Task<EditReport?> PostEditAsync(string text, string log)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(log);
        if (_token == null) throw new InvalidOperationException("The edit token was expired. You need to get another edit token. Call \"Review()\" first.");

        return _client.PostEditAsync(Document, text, log, _token);
    }
}

public class EditReport
{
    internal EditReport(int rev)
    {
        Rev = rev;
    }

    public int Rev { get; private set; }
}