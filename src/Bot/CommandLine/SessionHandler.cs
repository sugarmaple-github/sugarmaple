namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;


public class SessionHandler
{
    public static void Create(string name)
    {
        if (!name.EndsWith(".json"))
            name += ".json";
        var newData = new SessionSaved();
        if (!Directory.Exists("sessions"))
            Directory.CreateDirectory("sessions");
        var path = Path.Combine("sessions", name);
        FileUtil.WriteJson(path, newData);
    }

    public static void AddCommand(string name, List<string> commandSet)
    {
        var path = Path.Combine("sessions", name);
        var session = FileUtil.GetDeserializedJson<SessionSaved>(path);
        session.Commands.Add(commandSet);
        FileUtil.WriteJson(path, session);
    }

    public Task Start(string orderName, SeedBot bot)
    {
        var progresssPath = Path.Combine("tasks", orderName);
        var order = FileUtil.GetDeserializedJson<SessionSaved>(progresssPath);

        var reportPath = Path.Combine("reports", orderName);
        return Start(ref order, bot, progresssPath, reportPath);
    }

    public Task Start(ref SessionSaved orderSaved, SeedBot bot, string progress, string result)
    {
        var label = orderSaved.Progress.Label;
        var commands = FileUtil.Read(Path.Combine("orders", orderSaved.Script)).Split('\n');
        var order = CommandCompiler.Default.Build(commands);
        return Invoke(order, label, bot, orderSaved, progress, result);
    }

    public async Task Invoke(Order order, int start, SeedBot bot_, SessionSaved saved, string progressStream, string reportStream)
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
                FileUtil.WriteJson(progressStream, saved);
            };
        bot.OnPostSuccessfully += saved.Result.Accepted.Add;
        bot.OnPostEditError += o =>
        {
            if (o.InvalidRequestBody)
            {
                saved.Result.Denied.Bug.Add(o.Document);
                FileUtil.WriteJson(progressStream, saved);
            }
        };

        var report = new List<OrderResult>();
        var context = new OrderContext() { Bot = bot, Starter = progress.Context, Saver = () => FileUtil.WriteJson(progressStream, saved) };
        for (int i = start; i < insts.Length; i++)
        {
            (var processor, var curLabel) = insts[i];
            progress.Label = curLabel;
            await processor.Invoke(context);

            report.Add(saved.Result);
            FileUtil.WriteJson(reportStream, report);

            progress.Context = new();
            saved.Result = new();
        }
    }
}