namespace Sugarmaple.Bot.CommandLine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Namumark;
using System;
using System.CommandLine;

public class ConsoleMessage
{
    public Dictionary<string, string> _dicts = new Dictionary<string, string>
    {
        { "OrderStart", "'{0}' 의뢰를 처리합니다." },
        { "LackOfPermission", "ACL 권한이 부족하여 수정하지 못했습니다. : {0}" }
    };

    public static readonly ConsoleMessage Default = new ConsoleMessage();

    public string GetMessage(string key) => _dicts[key];
    public void ShowMessage(string key, params object?[]? args) => Console.WriteLine(GetMessage(key), args);
}

internal static class DefaultBot
{
    public static ConsoleBotHandler Handler { get; set; }
}

public class OrderCommand : Command
{
    internal OrderCommand() : base("order")
    {
        var executeCommand = new Command("execute");
        Add(executeCommand);

        var taskNameArgument = new Argument<string>();
        executeCommand.Add(taskNameArgument);

        var checkingOption = new System.CommandLine.Option<bool>("--check", () => true);
        executeCommand.Add(checkingOption);

        executeCommand.SetHandler(async (task, checking) => await Progress(task, DefaultBot.Handler, checking), taskNameArgument, checkingOption);

        var resetCmd = new Command("reset");
        Add(resetCmd);
        resetCmd.Add(taskNameArgument);
        resetCmd.SetHandler((task) =>
        {
            var path = Path.Combine("tasks", task);
            var text = FileUtil.Read(path);
            var json = JObject.Parse(text)!;
            var progress = json["progress"];
            progress["label"] = 0;
            progress["context"]["from"] = "";
            Save(path, json);
        }, taskNameArgument);
    }

    private void Save(string path, JObject json)
    {
        using var fileStream = FileUtil.Create(path);
        using var streamWriter = new StreamWriter(fileStream);
        using var jsonWriter = new JsonTextWriter(streamWriter) { Indentation = 4, IndentChar = ' ' };
        json.WriteTo(jsonWriter);
    }

    private static Task Progress(string orderName, ConsoleBotHandler handler, bool checking)
    {
        Console.Clear();
        ConsoleMessage.Default.ShowMessage("OrderStart", orderName);

        handler.CheckEditMode = checking;

        var starter = new OrderStarter();
        return starter.Start(orderName, handler.Bot);
    }
}

internal class CommandCompiler
{
    public static CommandCompiler Default = new();

    //public List<OrderDelegate> Queue = new();

    public Order Build(string[] commands)
    {
        var context = new OrderCompileInfo();
        var atomCommand = new OrderAtomCommand(context);
        for (int i = 0; i < commands.Length; i++)
        {
            var line = commands[i];
            context.Label = i;
            atomCommand.Invoke(line);
        }
        return new Order(context.Processors.ToArray());
    }

    public void TreatEnumerator(IEnumerable<string> docs)
    {

    }
}

public delegate Task OrderDelegate(OrderContext context);

public class OrderContext
{
    public BotEventHandler Bot;
    public OrderContinueInfo Starter;
    public IAsyncEnumerable<InternalLink> Queue;
    public int Index;

    public Func<string, string>? LogMaker { get; internal set; }
    public Action Saver { get; internal set; }

    internal string CreateLog(string arg)
    {
        return LogMaker(arg);
    }

    internal void SaveLabel()
    {
        Saver();
    }
}

public class Order
{
    private readonly (OrderDelegate, int)[] _insts;

    public Order((OrderDelegate, int)[] insts)
    {
        _insts = insts;
    }

    public (OrderDelegate, int)[] Instructions => _insts;
}