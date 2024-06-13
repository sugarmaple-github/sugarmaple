namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;
using System;
using System.IO;

public record struct OrderContext(string From, int Page = 1);
public struct OrderProgress { public int Label; public OrderContext Context; public List<OrderResult> Result; }
public record struct OrderDenied(HashSet<string> Acl);
public record struct OrderResult(OrderDenied Denied);
public struct OrderSaved
{
    public string Script;
    public OrderProgress Progress;
}

public class OrderStarter
{
    public void Start(ref OrderSaved orderSaved, SeedBot bot, StreamWriter writer)
    {
        var label = orderSaved.Progress.Label;
        var commands = FileUtil.Read(Path.Combine("orders", orderSaved.Script)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        Invoke(order, label, new(bot), ref orderSaved.Progress, writer);
    }


    private static JsonSerializer _serializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    public void Invoke(Order order, int start, BotEventHandler bot, ref OrderProgress progress, StreamWriter streamWriter)
    {
        var insts = order.Instructions;
        var result = progress.Result;
        var context = progress.Context;

        var writer = new JsonTextWriter(streamWriter) { Indentation = 4, IndentChar = ' ' };

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
                    _serializer.Serialize(writer, order);
                };

            insts[i].Invoke(bot, context);

            progress.Label = i + 1;
            progress.Context = default;
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
