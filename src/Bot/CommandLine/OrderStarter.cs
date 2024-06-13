namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;
using System;

public record struct OrderContext(int Page, string From);
public struct OrderProgress { public int Label; public OrderContext Context; }
public record struct OrderDenied(HashSet<string> Acl);
public record struct OrderResult(OrderDenied Denied);
public struct OrderSaved
{
    public string Script;
    public OrderProgress Progress;
    public OrderResult Result;
}

public class OrderStarter
{
    private readonly SeedBot _bot;
    private readonly string _path;
    [Obsolete]
    private readonly JObject _json;

    private readonly JsonSerializer _serializer;
    private readonly JsonWriter _writer;
    private OrderSaved _saved;

    private Action _onLabelEnd;

    public event Action<int>? OnSearch;

    public event Action<string, string> OnGetEditSuccessfully
    {
        add
        {
            _bot.OnGetEditSuccessfully += value;
            _onLabelEnd += () => _bot.OnGetEditSuccessfully -= value;
        }
        remove { _bot.OnGetEditSuccessfully -= value; }
    }

    public event Action<EditPostResult> OnPostSuccessfully
    {
        add
        {
            _bot.OnPostSuccessfully.Event += value;
            _onLabelEnd += () => _bot.OnPostSuccessfully.Event -= value;
        }
        remove { _bot.OnPostSuccessfully.Event -= value; }
    }

    public OrderStarter(SeedBot bot, string path, JObject element)
    {
        _bot = bot;
        _path = path;
        _json = element;

        var fileStream = FileUtil.Create(_path);
        var streamWriter = new StreamWriter(fileStream);
        _writer = new JsonTextWriter(streamWriter) { Indentation = 4, IndentChar = ' ' };
        _serializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    }

    public void Start()
    {
        var label = _saved.Progress.Label;
        var commands = FileUtil.Read(Path.Combine("orders", _saved.Script)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        Invoke(order, label);
    }

    public void Invoke(Order order, int start)
    {
        var insts = order.Instructions;
        _bot.OnLackOfPermission +=
            o =>
            {
                _saved.Result.Denied.Acl.Add(o.Document);
                Save();
            };
        for (int i = start; i < insts.Length; i++)
        {
            insts[i].Invoke(_bot, this);
            OnLabelEnd(i);
        }
    }

    internal int TryGetProgressInt(string key, int defaultValue)
    {
        var context = _json["progress"]!["context"];
        if (context?[key] != null)
            defaultValue = context![key]!.Value<int>()!;
        return defaultValue;
    }

    internal string TryGetProgress(string key, string value)
    {
        var context = _json["progress"]!["context"];
        if (context?[key] != null && context?[key]?.Value<string>() != null)
            value = context![key]!.Value<string>()!;
        return value;
    }

    internal void SaveProgress(string key, JToken value)
    {
        _json["progress"]!["context"]![key] = value;
        Save();
    }

    ///구현 예정. 스타터를 통해 이벤트를 등록하면, 레이블이 끝날 때 자동으로 소거됨.
    internal void AddEvent<T>(EventPublisher<T> target, Action<T> registered)
    {
        target.Event += registered;
        _onLabelEnd += () => target.Event -= registered;
    }

    /// <summary>
    /// 한 레이블이 끝날 때 호출되는 명령입니다.
    /// </summary>
    /// <param name="label"></param>
    private void OnLabelEnd(int label)
    {
        _saved.Progress = new() { Label = label + 1 };
        _onLabelEnd?.Invoke();
        Save();
    }

    private void Save()
    {
        _serializer.Serialize(_writer, _saved);
    }
}
