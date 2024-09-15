namespace Sugarmaple.Bot.CommandLine;

using net.sf.saxon.ma.map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;

[JsonObject]
public struct SessionSaved
{
    public SessionSaved()
    {
    }

    public List<List<string>> Commands { get; set; } = new();

    [JsonProperty]
    [Obsolete]
    public string Script { get; set; }
    [JsonProperty]
    public OrderProgress Progress { get; set; } = new();
    [JsonProperty]
    public OrderResult Result { get; set; } = new();
}

public class OrderContinueInfo
{
    public string From { get; set; }
    public int Page { get; set; } = 1;
    public Dictionary<string, string> Config { get; internal set; } = new();
}
public class OrderProgress
{
    public int Label { get; set; }
    public OrderContinueInfo Context { get; set; } = new();
}
public class OrderDenied
{
    public HashSet<string> Acl { get; } = new();
    public HashSet<string> Bug { get; } = new();
}
public class OrderResult
{
    public List<EditPostResult> Accepted { get; } = new();
    public OrderDenied Denied { get; } = new();
}

public record Session(SessionAction[] Actions, SessionTask[] Schedule, SessionProgress Progress);
public record SessionTask(int ActionId);
public record SessionProgress(int Label, Dictionary<string, object> Config);

public record SessionAction(string Type, object Args, int Id);
public record ConfigArgs(string Key, string Value);
public record EditArgs(FunctionCall[] Scope, Conditional[] Processing);
public record FunctionCall(string Name, object[] Args);
public record Conditional(FunctionCall Condition, FunctionCall Then);

public class SessionArgConverter : JsonConverter<SessionAction>
{
    public override SessionAction? ReadJson(JsonReader reader, Type objectType, SessionAction? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        int id = jsonObject["id"]!.Value<int>();
        var type = jsonObject["type"]?.ToString();
        var argsJObject = jsonObject["args"];
        object args;

        if (type == "edit")
        {
            args = argsJObject!.ToObject<EditArgs>(serializer)!;
        }
        else if (type == "config")
        {
            args = argsJObject!.ToObject<ConfigArgs>(serializer)!;
        }
        else
        {
            throw new InvalidOperationException($"Unknown type: {type}");
        }
        return new(type, args, id);
    }


    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, SessionAction? value, JsonSerializer serializer) => throw new NotImplementedException();
}