namespace Sugarmaple.Bot.CommandLine;

using javax.xml.transform;
using net.sf.saxon.functions;
using Sugarmaple.TheSeed.Namumark;
using System;
using System.IO;


public class SessionHandler
{
    private const string SessionFolder = "sessions";

    public static void Create(string name)
    {
        if (!name.EndsWith(".json"))
            name += ".json";
        var newData = new SessionSaved();
        if (!Directory.Exists(SessionFolder))
            Directory.CreateDirectory(SessionFolder);
        var path = Path.Combine(SessionFolder, name);
        FileUtil.WriteJson(path, newData);
    }

    public static void AddCommand(string name, List<string> commandSet)
    {
        var path = Path.Combine(SessionFolder, name);
        var session = FileUtil.GetDeserializedJson<SessionSaved>(path);
        session.Commands.Add(commandSet);
        FileUtil.WriteJson(path, session);
    }

    public static Task Execute(string name, SeedBot bot)
    {
        var path = Path.Combine(SessionFolder, name);
        var session = FileUtil.GetDeserializedJson<Session>(path);
        return ExecuteInternal(session, bot, () => FileUtil.WriteJson(path, session));
    }

    private static async Task ExecuteInternal(Session session, SeedBot bot, Action saveHandler)
    {
        var actions = session.Actions;
        foreach (var o in session.Schedule.Skip(session.Progress.Label))
        {
            await ExecuteAction(actions[o.ActionId], bot, session.Progress.Config, saveHandler);
            saveHandler();
        }
    }

    private static Task ExecuteAction(SessionAction action, SeedBot bot, Dictionary<string, string> config, Action saveHandler)
    {
        return SessionActionRunner.Execute(action, bot, config, saveHandler);
    }

    //public Task Start(string orderName, SeedBot bot)
    //{
    //    var progresssPath = Path.Combine("tasks", orderName);
    //    var order = FileUtil.GetDeserializedJson<SessionSaved>(progresssPath);

    //    var reportPath = Path.Combine("reports", orderName);
    //    return Start(ref order, bot, progresssPath, reportPath);
    //}

    //public Task Start(ref SessionSaved orderSaved, SeedBot bot, string progress, string result)
    //{
    //    var label = orderSaved.Progress.Label;
    //    var commands = FileUtil.Read(Path.Combine("orders", orderSaved.Script)).Split('\n');
    //    var order = CommandCompiler.Default.Build(commands);
    //    return Invoke(order, label, bot, orderSaved, progress, result);
    //}

    //public async Task Invoke(Order order, int start, SeedBot bot_, SessionSaved saved, string progressStream, string reportStream)
    //{
    //    using var bot = new BotEventHandler(bot_);
    //    var insts = order.Instructions;
    //    var progress = saved.Progress;
    //    bot.OnLackOfPermission +=
    //        o =>
    //        {
    //            saved.Result.Denied.Acl.Add(o.Document);
    //        };
    //    bot.OnGetEditSuccessfully +=
    //        (document, _) =>
    //        {
    //            progress.Context.From = document;
    //            FileUtil.WriteJson(progressStream, saved);
    //        };
    //    bot.OnPostSuccessfully += saved.Result.Accepted.Add;
    //    bot.OnPostEditError += o =>
    //    {
    //        if (o.InvalidRequestBody)
    //        {
    //            saved.Result.Denied.Bug.Add(o.Document);
    //            FileUtil.WriteJson(progressStream, saved);
    //        }
    //    };

    //    var report = new List<OrderResult>();
    //    var context = new OrderContext() { Bot = bot, Starter = progress.Context, Saver = () => FileUtil.WriteJson(progressStream, saved) };
    //    for (int i = start; i < insts.Length; i++)
    //    {
    //        (var processor, var curLabel) = insts[i];
    //        progress.Label = curLabel;
    //        await processor.Invoke(context);

    //        report.Add(saved.Result);
    //        FileUtil.WriteJson(reportStream, report);

    //        progress.Context = new();
    //        saved.Result = new();
    //    }
    //}
}

public class SessionActionRunner
{
    public static async Task Execute(SessionAction sessionAction, SeedBot bot, Dictionary<string, string> config, Action saveHandler)
    {
        if (sessionAction.Type == "config")
        {
            ExecuteConfig((ConfigArgs)sessionAction.Args, config);
        }
        else if (sessionAction.Type == "edit")
        {
            await ExecuteEdit((EditArgs)sessionAction.Args, bot, config, saveHandler);
        }
        else
        {
            throw new Exception();
        }
    }

    private static void ExecuteConfig(ConfigArgs args, Dictionary<string, string> config)
    {
        config[args.Key] = args.Value;
    }

    public static async Task ExecuteEdit(EditArgs args, SeedBot bot, Dictionary<string, string> config, Action saver)
    {
        //bot.OnPostSuccessfully
        await foreach (var o in args.Scope.ToAsyncEnumerable().SelectMany(o => CallFunctionScope(o, bot, config)))
        {
            ExecuteEditProcessing(o, args.Processing, bot, config, saver);
        }
    }

    private static void ExecuteEditProcessing(IAnchorReferer doc, Conditional[] processing, SeedBot bot, Dictionary<string, string> config, Action saver)
    {
        //몇 번 분기에서 동작하는 지 Log에 기재해야.
        foreach (var o in processing)
        {
            if (CallFunctionCondition(o.Condition, bot, doc))
            {
                switch (o.Then.Name)
                {
                    case "replace":
                        var destination = (string)o.Then.Args[0];
                        var anchor = o.Then.Args.Length >= 2 ? (string)o.Then.Args[1] : null;
                        config["from"] = doc.OwnerDocument!.Title;
                        doc.Reference = destination;
                        doc.Anchor = anchor;
                        saver();
                        return;
                    default:
                        throw new Exception();
                }
            }
        }
    }

    public static bool CallFunctionCondition(FunctionCall call, SeedBot bot, IAnchorReferer doc)
    {
        var args = call.Args;
        switch (call.Name)
        {
            case "input":
                bot.Viewer.ShowView(doc.OwnerDocument!.Title, true);
                bot.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(doc.Reference)}')]");
                return Console.ReadLine() == (string)args[0];
            case "true": return true;
            default: throw new Exception();
        }
    }

    public static IAsyncEnumerable<IAnchorReferer> CallFunctionScope(FunctionCall call, SeedBot bot, Dictionary<string, string> config)
    {
        var source = (string)call.Args[0];
        if (call.Name == "backlink")
        {
            //b.OnGetEditSuccessfully += (document, text) =>
            //{
            //    b.Bot.Viewer.ShowView(document, true);
            //    searchRoutine = b.Bot.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(source)}')]");
            //    //enumerator_old = b.Viewer.FindElements(By.XPath("//a[contains(@href,'Repulse%20Stream')]")).GetEnumerator();
            //}
            return bot.BacklinkReferersAsync<IAnchorReferer>(source, ~NamespaceMask.Wiki, config.GetValueOrDefault("from") ?? "",
                destination => $"[자동] 역링크 정리 \"{source}\" -> \"{destination}\" (사유: {config["reason"]})");
        }
        throw new Exception();
    }
}