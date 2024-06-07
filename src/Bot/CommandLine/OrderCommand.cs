namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Crawler;
using System;
using System.CommandLine;
using System.Runtime.CompilerServices;

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
        ConsoleMessage.Default.ShowMessage("OrderStart", orderName);

        var path = Path.Combine("tasks", orderName);
        var text = FileUtil.Read(path);
        var json = JObject.Parse(text)!;
        var starter = new OrderStarter(bot, path, json);
        starter.Start();
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

public class OrderStarter
{
    private readonly SeedBot _bot;
    private readonly string _path;
    private readonly JObject _json;

    private Action _onLabelEnd;

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
    }

    public void Start()
    {
        var scriptPath = _json.Value<string>("script")!; //TODO: null control

        var progress = _json["progress"];
        var label = progress!.Value<int>("label");
        var commands = FileUtil.Read(Path.Combine("orders", scriptPath)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        Invoke(order, label);
    }

    public void Invoke(Order order, int start)
    {
        var insts = order.Instructions;
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
        _json["progress"]!["label"] = label + 1;
        _json["progress"]!["context"]!["from"] = "";
        _onLabelEnd?.Invoke();
        Save();
    }

    private void Save()
    {
        using var fileStream = FileUtil.Create(_path);
        using var streamWriter = new StreamWriter(fileStream);
        using var jsonWriter = new JsonTextWriter(streamWriter) { Indentation = 4, IndentChar = ' ' };
        _json.WriteTo(jsonWriter);
    }
}

public delegate void OrderDelegate(SeedBot bot, OrderStarter starter);

ref struct EventRegister<T>
{
    ref EventPublisher<T> _action;
    Action<T> _target;

    public EventRegister(ref EventPublisher<T> action, Action<T> target)
    {
        _action = ref action;
        _target = target;
        _action.Event += target;
    }

    public void Dispose()
    {
        _action.Event -= _target;
    }
}

public class Order
{
    private readonly OrderDelegate[] _insts;

    public Order(OrderDelegate[] insts)
    {
        _insts = insts;
    }

    public OrderDelegate[] Instructions => _insts;
}