namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sugarmaple.TheSeed.Api;

[JsonObject]
public struct OrderSaved
{
    [JsonProperty]
    public string Script { get; set; }
    [JsonProperty]
    public OrderProgress Progress { get; set; }
    [JsonProperty]
    public OrderResult Result { get; set; }
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
