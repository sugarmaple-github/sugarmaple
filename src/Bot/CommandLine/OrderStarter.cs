namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;
using System;
using System.IO;

public class OrderContext
{
    public string From { get; set; }
    public int Page { get; set; } = 1;
}
public class OrderProgress
{
    public int Label { get; set; }
    public OrderContext Context { get; set; } = new();
}
public class OrderDenied
{
    public HashSet<string> Acl { get; } = new();
    public HashSet<string> Bug { get; } = new();
}
public class OrderResult
{
    public List<EditPostResult> Accepted { get; } = new();
    public OrderDenied Denied { get; } = new();
}
[JsonObject]
public struct OrderSaved
{
    [JsonProperty]
    public string Script { get; set; }
    [JsonProperty]
    public OrderProgress Progress { get; set; }
    [JsonProperty]
    public OrderResult Result { get; set; }
}

public class OrderStarter
{
    public Task Start(string orderName, SeedBot bot)
    {
        var progresssPath = Path.Combine("tasks", orderName);
        var order = FileUtil.GetDeserializedJson<OrderSaved>(progresssPath, _serializer);

        var reportPath = Path.Combine("reports", orderName);

        //using var progressFileStream = FileUtil.Create(progresssPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        //using var resultFileStream = FileUtil.Create(reportPath, FileMode.Create, FileAccess.ReadWrite);

        return Start(ref order, bot, progresssPath, reportPath);
    }

    public Task Start(ref OrderSaved orderSaved, SeedBot bot, string progress, string result)
    {
        var label = orderSaved.Progress.Label;
        var commands = FileUtil.Read(Path.Combine("orders", orderSaved.Script)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        return Invoke(order, label, bot, orderSaved, progress, result);
    }


    private static JsonSerializer _serializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    public async Task Invoke(Order order, int start, SeedBot bot_, OrderSaved saved, string progressStream, string reportStream)
    {
        using var bot = new BotEventHandler(bot_);
        var insts = order.Instructions;
        var progress = saved.Progress;
        bot.OnLackOfPermission +=
            o =>
            {
                saved.Result.Denied.Acl.Add(o.Document);
            };
        bot.OnGetEditSuccessfully +=
            (document, _) =>
            {
                progress.Context.From = document;
                WriteJson(progressStream, saved);
            };
        bot.OnPostSuccessfully += saved.Result.Accepted.Add;
        bot.OnPostEditError += o =>
        {
            if (o.InvalidRequestBody)
            {
                saved.Result.Denied.Bug.Add(o.Document);
                WriteJson(progressStream, saved);
            }
        };


        var report = new List<OrderResult>();
        for (int i = start; i < insts.Length; i++)
        {
            await insts[i].Invoke(bot, progress.Context);

            report.Add(saved.Result);
            WriteJson(reportStream, report);

            progress.Label = i + 1;
            progress.Context = new();
            saved.Result = new();
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        using var stream = FileUtil.Create(path, FileMode.Truncate, FileAccess.Write);
        using var streamWriter = new StreamWriter(stream);
        using var jsonWriter = new JsonTextWriter(streamWriter);
        _serializer.Serialize(jsonWriter, value);
    }

    private static void WriteJson<T>(Stream stream, T value)
    {
        stream.Position = 0;
        stream.SetLength(0);
        var streamWriter = new StreamWriter(stream);
        var jsonWriter = new JsonTextWriter(streamWriter);
        _serializer.Serialize(jsonWriter, value);
    }
}

public class BotEventHandler : IDisposable
{
    private readonly SeedBot _bot;
    private Action? _removeEvent;

    public SeedBot Bot => _bot;

    public BotEventHandler(SeedBot bot)
    {
        _bot = bot;
    }

    public void RemoveEvent()
    {
        _removeEvent?.Invoke();
    }

    public void Dispose() => RemoveEvent();

    public event Action<EditGetError> OnLackOfPermission
    {
        add
        {
            _bot.OnLackOfPermission += value;
            _removeEvent += () => _bot.OnLackOfPermission -= value;
        }
        remove { _bot.OnLackOfPermission -= value; }
    }

    public event Action<string, string> OnGetEditSuccessfully
    {
        add
        {
            _bot.OnGetEditSuccessfully += value;
            _removeEvent += () => _bot.OnGetEditSuccessfully -= value;
        }
        remove { _bot.OnGetEditSuccessfully -= value; }
    }

    public event Action<EditPostResult> OnPostSuccessfully
    {
        add
        {
            _bot.OnPostSuccessfully.Event += value;
            _removeEvent += () => _bot.OnPostSuccessfully.Event -= value;
        }
        remove { _bot.OnPostSuccessfully.Event -= value; }
    }

    public event Action<EditPostError> OnPostEditError
    {
        add
        {
            _bot.OnPostEditError += value;
            _removeEvent += () => _bot.OnPostEditError -= value;
        }
        remove { _bot.OnPostEditError -= value; }
    }

}
