namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CommandLine;

public class MainCommand : RootCommand
{
    private readonly SessionHandler _sessionHandler = new();

    public event Action<string>? SessionCreated {
        add => _sessionHandler.SessionCreated += value;
        remove => _sessionHandler.SessionCreated -= value;
    }
    public event Action<string>? SessionExecuting {
        add => _sessionHandler.SessionExecuting += value;
        remove => _sessionHandler.SessionExecuting -= value;
    }

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

    private Command Session()
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
        addCommand.SetHandler(o => _sessionHandler.AddCommand(o, ReadCommandSet()), sessionNameArg);

        createCmd.SetHandler(_sessionHandler.Create, sessionNameArg);

        var executeCommand = new Command("execute")
        {
            sessionNameArg
        };

        var checkingOption = new Option<bool>("--check", () => true);
        executeCommand.Add(checkingOption);

        executeCommand.SetHandler(_sessionHandler.Execute, sessionNameArg, checkingOption);
        // executeCommand.SetHandler(async (session, checking) => await Progress(session, DefaultBot.Handler, checking), 
        //     sessionNameArg, checkingOption);

        var resetCmd = new Command("reset")
        {
           sessionNameArg
        };

        resetCmd.SetHandler(Reset, sessionNameArg);
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

    private Task Progress(string session, ConsoleBotHandler handler, bool checking)
    {
        handler.CheckEditMode = checking;
        return _sessionHandler.Execute(session, handler.Bot);
    }
}