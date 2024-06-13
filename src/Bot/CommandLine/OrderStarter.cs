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
    public List<OrderResult> Result { get; set; } = new();
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
        var path = Path.Combine("tasks", orderName);
        var order = FileUtil.GetDeserializedJson<OrderSaved>(path, new() { ContractResolver = new CamelCasePropertyNamesContractResolver() });

        using var fileStream = FileUtil.Create(path);
        using var streamWriter = new StreamWriter(fileStream);

        Start(ref order, bot, streamWriter);
    }

    public void Start(ref OrderSaved orderSaved, SeedBot bot, StreamWriter writer)
    {
        var label = orderSaved.Progress.Label;
        var commands = FileUtil.Read(Path.Combine("orders", orderSaved.Script)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        Invoke(order, label, new(bot), orderSaved, writer);
    }


    private static JsonSerializer _serializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    public void Invoke(Order order, int start, BotEventHandler bot, OrderSaved saved, StreamWriter streamWriter)
    {
        var insts = order.Instructions;
        var result = saved.Progress.Result;
        var context = saved.Progress.Context;

        using var writer = new JsonTextWriter(streamWriter);

        for (int i = start; i < insts.Length; i++)
        {
            bot.OnLackOfPermission +=
            o =>
            {
                result[i].Denied.Acl.Add(o.Document);
            };
            bot.OnGetEditSuccessfully +=
                (document, _) =>
                {
                    context.From = document;
                    _serializer.Serialize(writer, saved);
                };

            insts[i].Invoke(bot, context);

            saved.Progress.Label = i + 1;
            saved.Progress.Context = default;
            bot.RemoveEvent();
        }
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
