// See https://aka.ms/new-console-template for more information
namespace Sugarmaple.Bot.CommandLine;
using Microsoft.Extensions.Configuration;
using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Crawler;

internal static class FileUtil
{
    static readonly string _path;
    static readonly IConfigurationRoot _credentials;
    static readonly IConfigurationRoot _config;

    static FileUtil()
    {
        _config = GetConfigRoot("config", "config.json");

        _path = _config["dataLocation"]!;
        _credentials = GetConfigRoot("credentials.json");
    }

    public static string GetValue(string key) =>
        _credentials[key] ?? throw new KeyNotFoundException();

    public static string[] GetValues(string key) =>
        _credentials.GetSection(key).Get<string[]>() ?? throw new KeyNotFoundException();

    public static IConfigurationRoot GetConfigRoot(string name)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(_path)
            .AddJsonFile(name, true, true);
        return builder.Build();
    }

    public static IConfigurationRoot GetConfigRoot(string path, string name)
    {
        var basicPath = Environment.CurrentDirectory;
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(basicPath, path))
            .AddJsonFile(name, true, true);
        return builder.Build();
    }

    public static SeedApiClient GetConfigClient()
    {
        var wikiUri = GetValue("WikiUri");
        var apiToken = GetValue("ApiToken");
        return new(wikiUri, apiToken);
    }

    public static SeedCrawler GetCrawler()
    {
        return new(GetValue("WikiUri"));
    }

    public static void SetEditResult(string content)
    {
        File.WriteAllText(_path + '/' + GetValue("EditResult"), content);
    }

    public static void Write(string name, string content)
    {
        var fileLoc = Path.Combine(_path, name);
        File.WriteAllText(fileLoc, content);
    }

    public static FileStream Create(string name)
    {
        var fileLoc = Path.Combine(_path, name);
        return File.Create(fileLoc);
    }

    public static FileStream OpenWrite(string name)
    {
        var fileLoc = Path.Combine(_path, name);
        return File.OpenWrite(fileLoc);
    }
    public static string Read(string name)
    {
        var fileLoc = Path.Combine(_path, name);
        var ret = File.ReadAllText(fileLoc);
        return ret;
    }

    public static string[] GetDocs()
    {
        return File.ReadAllLines(_path + '/' + GetValue("Docs"));
    }

    public static string GetMarkup()
    {
        return File.ReadAllText(_path + '/' + GetValue("Markup"));
    }

    public static string GetUserName() => GetValue("User");

    internal static string[] GetStrings(string v)
    {
        return File.ReadAllLines(_path + '/' + v);
    }
}