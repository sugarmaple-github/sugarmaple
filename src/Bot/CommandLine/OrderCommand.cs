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
    public static ConsoleBotHandler? _handler;

    public static ConsoleBotHandler Handler
    {
        get
        {
            if (_handler == null)
            {
                var wikiUri = FileUtil.GetValue("WikiUri");
                var apiToken = FileUtil.GetValue("ApiToken");
                var userName = FileUtil.GetValue("UserName");
                var wikiNamespaces = FileUtil.GetValues("WikiNamespaces");
                _handler = ConsoleBotCreator.Create("https://namu.wiki", wikiUri, apiToken, userName, wikiNamespaces);
            }
            return _handler;
        }
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