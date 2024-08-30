namespace Sugarmaple.Bot.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

internal static class FileUtil
{
    public static string Read(string name)
    {
        var ret = File.ReadAllText(name);
        return ret;
    }

    static readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = new[] { new SessionArgConverter() },
        Formatting = Formatting.Indented,
    });

    public static T GetDeserializedJson<T>(string path) => GetDeserializedJson<T>(path, _serializer);

    public static T GetDeserializedJson<T>(string path, JsonSerializer serializer)
    {
        using var file = new StreamReader(File.OpenRead(path));
        using var j = new JsonTextReader(file);
        return serializer.Deserialize<T>(j)!;
    }

    public static T GetDeserializedJson<T>(FileStream stream)
    {
        using var file = new StreamReader(stream);
        using var j = new JsonTextReader(file);
        return _serializer.Deserialize<T>(j)!;
    }

    public static void WriteJson<T>(string path, T value)
    {
        using var stream = new FileStream(path, FileMode.Truncate, FileAccess.Write);
        using var streamWriter = new StreamWriter(stream);
        using var jsonWriter = new JsonTextWriter(streamWriter);
        jsonWriter.Indentation = 4;
        _serializer.Serialize(jsonWriter, value);
    }
}

