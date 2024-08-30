namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;

public class MainCommand : RootCommand
{
    internal MainCommand()
    {
        //Add(Profile());
        Add(Session());
    }
    private static Command Profile()
    {
        var cmd = new Command("profile");
        return cmd;
    }

    private static Command Session()
    {
        var sessionNameArg = new Argument<string>();
        var createCmd = new Command("create")
        {
            sessionNameArg
        };

        var commandArg = new Argument<string>();
        var addCommand = new Command("add-command")
        {
            sessionNameArg, commandArg
        };
        addCommand.SetHandler(o => SessionHandler.AddCommand(o, ReadCommandSet()), sessionNameArg);

        createCmd.SetHandler(SessionHandler.Create, sessionNameArg);

        var executeCommand = new Command("execute");

        var taskNameArgument = new Argument<string>();
        executeCommand.Add(taskNameArgument);

        var checkingOption = new Option<bool>("--check", () => true);
        executeCommand.Add(checkingOption);

        executeCommand.SetHandler(async (task, checking) => await Progress(task, DefaultBot.Handler, checking), taskNameArgument, checkingOption);

        var resetCmd = new Command("reset")
        {
           taskNameArgument
        };

        resetCmd.SetHandler(Reset, taskNameArgument);
        var cmd = new Command("session")
        {
            createCmd, addCommand, executeCommand, resetCmd
        };


        return cmd;
    }

    static List<string> ReadCommandSet()
    {
        var output = new List<string>();
        var line = Console.ReadLine();
        while (ValidateAndNeedMoreLine(line)) //입력이 잘못되거나 더 필요하면 더 호출
        {
            output.Add(line);
            line = Console.ReadLine();
        }
        return output;
    }

    private static bool ValidateAndNeedMoreLine(string line)
    {
        return false;
        //var OrderAtomCommand();
    }

    static void Reset(string task)
    {
        var path = Path.Combine("tasks", task);
        var text = FileUtil.Read(path);
        var json = JObject.Parse(text)!;
        var progress = json["progress"];
        progress["label"] = 0;
        progress["context"]["from"] = "";
        Save(path, json);
    }

    private static void Save(string path, JObject json)
    {
        using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using var streamWriter = new StreamWriter(fileStream);
        using var jsonWriter = new JsonTextWriter(streamWriter) { Indentation = 4, IndentChar = ' ' };
        json.WriteTo(jsonWriter);
    }

    private static Task Progress(string session, ConsoleBotHandler handler, bool checking)
    {
        Console.Clear();
        ConsoleMessage.Default.ShowMessage("OrderStart", session);

        handler.CheckEditMode = checking;
        return SessionHandler.Execute(session, handler.Bot);
    }
}