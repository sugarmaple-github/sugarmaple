namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

public class MainCommand : RootCommand
{
    internal MainCommand()
    {
        Add(Session());
    }

    private static Command Session()
    {
        var cmd = new Command("session");

        var executeCommand = new Command("execute");
        cmd.Add(executeCommand);

        var taskNameArgument = new Argument<string>();
        executeCommand.Add(taskNameArgument);

        var checkingOption = new Option<bool>("--check", () => true);
        executeCommand.Add(checkingOption);

        executeCommand.SetHandler(async (task, checking) => await Progress(task, DefaultBot.Handler, checking), taskNameArgument, checkingOption);

        var resetCmd = new Command("reset");
        cmd.Add(resetCmd);
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


        return cmd;
    }

    private static void Save(string path, JObject json)
    {
        using var fileStream = FileUtil.Create(path);
        using var streamWriter = new StreamWriter(fileStream);
        using var jsonWriter = new JsonTextWriter(streamWriter) { Indentation = 4, IndentChar = ' ' };
        json.WriteTo(jsonWriter);
    }

    private static Task Progress(string session, ConsoleBotHandler handler, bool checking)
    {
        Console.Clear();
        ConsoleMessage.Default.ShowMessage("OrderStart", session);

        handler.CheckEditMode = checking;

        var starter = new OrderStarter();
        return starter.Start(session, handler.Bot);
    }
}