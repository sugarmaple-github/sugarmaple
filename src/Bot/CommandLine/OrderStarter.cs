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
    public OrderContext Context { get; set; }
    public OrderResult Result { get; set; }
}
public record struct OrderDenied(HashSet<string> Acl);
public record struct OrderResult(OrderDenied Denied);
[JsonObject]
public struct OrderSaved
{
    [JsonProperty]
    public string Script { get; set; }
    [JsonProperty]
    public OrderProgress Progress { get; set; }
}

public class OrderStarter
{
    public void Start(string orderName, SeedBot bot)
    {
        var progresssPath = Path.Combine("tasks", orderName);
        var order = FileUtil.GetDeserializedJson<OrderSaved>(progresssPath, _serializer);

        var reportPath = Path.Combine("reports", orderName);

        //using var progressFileStream = FileUtil.Create(progresssPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        //using var resultFileStream = FileUtil.Create(reportPath, FileMode.Create, FileAccess.ReadWrite);

        Start(ref order, bot, progresssPath, reportPath);
    }

    public void Start(ref OrderSaved orderSaved, SeedBot bot, string progress, string result)
    {
        var label = orderSaved.Progress.Label;
        var commands = FileUtil.Read(Path.Combine("orders", orderSaved.Script)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        Invoke(order, label, new(bot), orderSaved, progress, result);
    }


    private static JsonSerializer _serializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    public void Invoke(Order order, int start, BotEventHandler bot, OrderSaved saved, string progressStream, string reportStream)
    {
        var insts = order.Instructions;
        var progress = saved.Progress;
        var result = progress.Result;
        var context = progress.Context;

        var report = new List<OrderResult>();
        for (int i = start; i < insts.Length; i++)
        {
            bot.OnLackOfPermission +=
            o =>
            {
                result.Denied.Acl.Add(o.Document);
            };
            bot.OnGetEditSuccessfully +=
                (document, _) =>
                {
                    context.From = document;
                    WriteJson(progressStream, saved);
                };

            insts[i].Invoke(bot, context);

            progress.Label = i + 1;
            progress.Context = new();
            report.Add(progress.Result);
            progress.Result = new();
            WriteJson(reportStream, report);
            bot.RemoveEvent();
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

public class BotEventHandler
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
}
