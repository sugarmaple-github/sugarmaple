// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Crawler;

internal static class Config
{
    static readonly string _path;
    static readonly IConfigurationRoot _root;

    static Config()
    {
        _path = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var builder = new ConfigurationBuilder()
            .SetBasePath(_path)
            .AddJsonFile("appsettings.json", true, true);
        _root = builder.Build();
    }

    public static string GetValue(string key) =>
        _root[key] ?? throw new KeyNotFoundException();

    public static SeedBot GetSeedBot()
    {
        var wikiUri = GetValue("WikiUri");
        var apiToken = GetValue("ApiToken");
        var userName = GetValue("UserName");
        return new(wikiUri, apiToken, userName);
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

    public static string[] GetDocs()
    {
        return File.ReadAllLines(_path + '/' + GetValue("Docs"));
    }

    /*public static SeedJsonClient GetJsonClient()
    {
        var wikiUri = GetValue("WikiUri");
        var ret = new SeedJsonClient(wikiUri);
        var apiToken = GetValue("ApiToken");
        ret.UpdateAuthHeader($"Bearer {apiToken}");
        return ret;
    }*/

    public static string GetUserName() => GetValue("User");

    public static string GetMarkup()
    {
        return File.ReadAllText(_path + '/' + GetValue("Markup"));
    }

    public static void SetResultMarkup(string result)
    {
        File.WriteAllText(_path + '/' + GetValue("MarkupResult"), result);
    }
}