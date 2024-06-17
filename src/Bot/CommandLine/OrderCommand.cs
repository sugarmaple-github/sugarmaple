namespace Sugarmaple.Bot.CommandLine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;
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

public class OrderCommand : Command
{
    internal OrderCommand(SeedBot bot) : base("order")
    {
        var executeCommand = new Command("execute");
        Add(executeCommand);
        var taskNameArgument = new Argument<string>();
        executeCommand.Add(taskNameArgument);
        executeCommand.SetHandler((task) => Progress(task, bot), taskNameArgument);

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

    private static void Progress(string orderName, SeedBot bot)
    {
        Console.Clear();
        ConsoleMessage.Default.ShowMessage("OrderStart", orderName);

        var starter = new OrderStarter();
        starter.Start(orderName, bot);
    }
}

internal class CommandCompiler
{
    public static CommandCompiler Default = new();
    public OrderAtomCommand Command = new();

    public Order Build(string[] commands)
    {
        foreach (var line in commands)
        {
            Command.Invoke(line);
        }
        return new Order(Command.GetOrder().ToArray());
    }
}

public delegate Task OrderDelegate(BotEventHandler bot, OrderContext starter);

public class Order
{
    private readonly OrderDelegate[] _insts;

    public Order(OrderDelegate[] insts)
    {
        _insts = insts;
    }

    public OrderDelegate[] Instructions => _insts;
}