namespace Sugarmaple.Bot.CommandLine;
using System;
using System.IO;
using Sugarmaple.TheSeed.Api;

public class SessionHandler
{
    private const string SessionFolder = "sessions";

    public event Action<string>? SessionCreated;
    public event Action<string>? SessionExecuting;
    public event Action<string, Option<BacklinkResponse>>? ApiBacklinkGot;
    public void Create(string name)
    {
        if (!name.EndsWith(".json"))
            name += ".json";
        var newData = new Session();
        if (!Directory.Exists(SessionFolder))
            Directory.CreateDirectory(SessionFolder);
        var path = Path.Combine(SessionFolder, name);
        FileUtil.WriteJson(path, newData);
        SessionCreated?.Invoke(name);
    }

    public void AddCommand(string name, List<string> commandSet)
    {
        var path = Path.Combine(SessionFolder, name);
        var session = FileUtil.GetDeserializedJson<SessionSaved>(path);
        session.Commands.Add(commandSet);
        FileUtil.WriteJson(path, session);
    }

    public Task Execute(string name, SeedBot bot)
    {
        SessionExecuting?.Invoke(name);
        var path = Path.Combine(SessionFolder, name);
        var session = FileUtil.GetDeserializedJson<Session>(path);
        return ExecuteInternal(session, bot, () => FileUtil.WriteJson(path, session));
    }

    public Task Execute(string name, bool checking)
    {
        SessionExecuting?.Invoke(name);
        var path = Path.Combine(SessionFolder, name);
        var session = FileUtil.GetDeserializedJson<Session>(path);
        return ExecuteInternal(session, LoadProfile(), () => FileUtil.WriteJson(path, session));
    }

    public SeedBot LoadProfile() {
        var file = new FileStream("config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        var profile = FileUtil.GetDeserializedJson<Profile>(file);

        (var wikiUri, var wikiApiUri, var apiToken, var userName, var wikiNamespaces) = profile;
        var handler = ConsoleBotCreator.Create(wikiUri, wikiApiUri, apiToken, userName, wikiNamespaces.ToArray());
        var ret = handler.Bot;
        ret.GotBacklink += ApiBacklinkGot;
        return handler.Bot;
    }

    private async Task ExecuteInternal(Session session, SeedBot bot, Action saveHandler)
    {
        var actions = session.Actions;
        foreach (var o in session.Schedule.Skip(session.Progress.Label))
        {
            await ExecuteAction(actions[o.ActionId], bot, session.Progress.Config, saveHandler);
            saveHandler();
        }
    }

    private Task ExecuteAction(SessionAction action, SeedBot bot, Dictionary<string, object> config, Action saveHandler)
    {
        return SessionActionRunner.Execute(action, bot, config, saveHandler);
    }
}