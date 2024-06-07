namespace Sugarmaple.TheSeed.Api;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

internal class JsonClient
{
    private readonly HttpClient _client = new();
    private readonly TaskDelayProcessor _delayProcessor = new();

    readonly HttpClientHandler _handler = new() { UseCookies = false };

    public JsonClient(string baseAddress)
    {
        _client = new(_handler)
        {
            BaseAddress = new Uri(baseAddress)
        };
    }

    private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions _deserializerOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly MediaTypeHeaderValue _contentType = MediaTypeHeaderValue.Parse("application/json");
    private int _attemptCount = 0;
    private const int MaxAttempt = 5;

    public event Action<string>? OnError;

    internal HttpClient InternalClient => _client;

    public Task<Option<TOut>> GetAsync<TOut>(string uri) => AddQueue(() => GetAsyncIn<TOut>(uri));
    private async Task<Option<TOut>> GetAsyncIn<TOut>(string uri)
    {
        Option<TOut> deserialized;
        do
        {
            var result = await _client.GetAsync(uri);
            var stream = result.Content.ReadAsStream();
            deserialized = await DeserializeAsync<TOut>(stream);
        } while (deserialized.TryGetError(out string? error) && IsRetryValid(error));
        return deserialized;
    }

    public Task<Option<TOut>> PostAsync<TOut, TPost>(string uri, TPost data) => AddQueue(() => PostAsyncIn<TOut, TPost>(uri, data));
    private async Task<Option<TOut>> PostAsyncIn<TOut, TPost>(string uri, TPost data)
    {
        Option<TOut> deserialized;
        do
        {
            var content = JsonContent.Create(data, _contentType, _serializerOptions);
            var result = await _client.PostAsync(uri, content);
            var stream = result.Content.ReadAsStream();
            deserialized = await DeserializeAsync<TOut>(stream);
        } while (deserialized.TryGetError(out string? error) && IsRetryValid(error));

        return deserialized;
    }

    private async Task<Option<TOut>> DeserializeAsync<TOut>(Stream stream)
    {
        _attemptCount++;
        try
        {
            var result = await JsonSerializer.DeserializeAsync<TOut>(stream, _deserializerOptions);
            _attemptCount = 0;
            return new(result!);
        }
        catch (JsonException)
        {
            var error = ToString(stream);
            OnError?.Invoke(error);
            return new(error);
        }
    }

    private bool IsRetryValid(string content)
    {
        if (_attemptCount < MaxAttempt && content.Contains("502 Bad Gateway")) return true;
        return false;
    }

    private static string ToString(Stream stream)
    {
        stream.Position = 0;
        string content;
        using (var reader = new StreamReader(stream))
        {
            content = reader.ReadToEnd();
        }
        return content;
    }

    internal Task<string> GetLiteralAsync(string uri) => _client.GetAsync(uri).Result.Content.ReadAsStringAsync();

    public void UpdateAuthHeader(string value) => _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(value);

    private Task<T> GetTask<T>(Func<T> func) => new Task<T>(func);

    private Task<T> AddQueue<T>(Func<Task<T>> task)
    {
        return _delayProcessor.Enqueue(GetTask(() => task().Result));
    }
}

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

public class EditGetResult : IUnion<EditView, EditGetError>
{
    private readonly Union<EditView, EditGetError> _inner;

    public EditGetResult(Union<EditView, EditGetError> inner)
    {
        _inner = inner;
    }

    public bool HasTruth => _inner.HasTruth;

    public bool Match([NotNullWhen(true)] out EditView? trueValue, [NotNullWhen(false)] out EditGetError? falseValue)
    {
        return _inner.Match(out trueValue, out falseValue);
    }

    public bool TryGetLeft([NotNullWhen(true)] out EditView? trueValue)
    {
        return _inner.TryGetLeft(out trueValue);
    }

    public bool TryGetRight([NotNullWhen(true)] out EditGetError? falseValue)
    {
        return _inner.TryGetRight(out falseValue);
    }

    public static implicit operator EditGetResult(EditView value) => new(value);
    public static implicit operator EditGetResult(EditGetError value) => new(value);
}

public class EditPostResult_old : IUnion<EditReport, EditPostError>
{
    private Union<EditReport, EditPostError> _innerValue;

    public EditPostResult_old(Union<EditReport, EditPostError> innerValue)
    {
        _innerValue = innerValue;
    }

    public bool Match([NotNullWhen(true)] out EditReport? trueValue, [NotNullWhen(false)] out EditPostError? falseValue)
    {
        return _innerValue.Match(out trueValue, out falseValue);
    }

    public bool TryGetLeft([NotNullWhen(true)] out EditReport? trueValue)
    {
        return _innerValue.TryGetLeft(out trueValue);
    }

    public bool TryGetRight([NotNullWhen(true)] out EditPostError? falseValue)
    {
        return _innerValue.TryGetRight(out falseValue);
    }

    public static implicit operator EditPostResult_old(EditPostError value) => new(value);
    public static implicit operator EditPostResult_old(EditReport value) => new(value);
}

public interface IUnion<TTrue, TFalse> where TTrue : class where TFalse : class
{
    public bool Match([NotNullWhen(true)] out TTrue? trueValue, [NotNullWhen(false)] out TFalse? falseValue);

    public bool TryGetLeft([NotNullWhen(true)] out TTrue? trueValue);

    public bool TryGetRight([NotNullWhen(true)] out TFalse? falseValue);
}

public readonly struct Union<TTrue, TFalse> : IUnion<TTrue, TFalse> where TTrue : class where TFalse : class
{
    private readonly TTrue? _trueValue;
    private readonly TFalse? _falseValue;
    public readonly bool HasTruth;

    private Union(TTrue? trueValue = null, TFalse? falseValue = null)
    {
        _trueValue = trueValue;
        _falseValue = falseValue;
        HasTruth = trueValue != null;
    }

    public bool Match([NotNullWhen(true)] out TTrue? trueValue, [NotNullWhen(false)] out TFalse? falseValue)
    {
        trueValue = _trueValue;
        falseValue = _falseValue;
        return _trueValue != null;
    }

    public bool TryGetLeft([NotNullWhen(true)] out TTrue? trueValue)
    {
        trueValue = _trueValue;
        return _trueValue != null;
    }

    public bool TryGetRight([NotNullWhen(true)] out TFalse? falseValue)
    {
        falseValue = _falseValue;
        return _falseValue != null;
    }

    public static implicit operator Union<TTrue, TFalse>(TTrue value)
    {
        var ret = new Union<TTrue, TFalse>(trueValue: value);
        return ret;
    }

    public static implicit operator Union<TTrue, TFalse>(TFalse value)
    {
        var ret = new Union<TTrue, TFalse>(falseValue: value);
        return ret;
    }
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