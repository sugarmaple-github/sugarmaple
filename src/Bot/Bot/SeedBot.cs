namespace Sugarmaple.Bot;

using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Crawler;
using Sugarmaple.TheSeed.Namumark;
using System;

public delegate string LogMaker(params string[] args);

/// <summary>
/// 
/// </summary>
/// <seealso cref="SeedBot"/>
public class SeedBot : SeedApiClient
{
    public SeedCrawler Crawler => _crawler;
    private readonly SeedCrawler _crawler;

    private readonly int _discussNum;
    private int _editCount;
    private const int CheckTerm = 5;

    public string UserName { get; }
    public string UserDoc { get; }

    public Action? OnEmergencyHappened = () => throw new Exception("Emergency Happen!");

    public SeedViewer? _viewer;
    public SeedViewer Viewer { get => _viewer ??= new(_trueWikiUri); }
    private string _trueWikiUri;
    public string[] WikiNamespaces { get; internal set; }
    public Action<string, Document> DocumentPosting { get; internal set; }

    public SeedBot(string wikiUri, string wikiApiUri, string apiToken, string userName, string[] wikiNamespaces) : base(wikiApiUri, apiToken)
    {
        _crawler = new(wikiUri);
        _trueWikiUri = wikiUri;
        PostedSuccessfully += o => NotifyEdit();

        UserDoc = "사용자:" + userName;
        UserName = userName;
        _discussNum = GetDiscussCount();
        WikiNamespaces = NamespaceMask.DefaultNamespaces.Concat(wikiNamespaces).ToArray();
    }

    public void Dispose()
    {
        _crawler.Dispose();
    }

    public IEnumerable<string> GetContribution(string from) => _crawler.GetContribution(UserName, from);

    public IEnumerable<string> GetCategoryDocument(string document) => _crawler.GetCategoryDocument(document);

    private int GetDiscussCount() => _crawler.GetDiscussPage(UserDoc).OpenDiscussionList.Count;

    private void NotifyEdit()
    {
        if (_editCount++ % CheckTerm == 0)
            if (_discussNum != GetDiscussCount())
            {
                OnEmergencyHappened?.Invoke();
            }
    }

    public Task<Option<EditResponse>> PostEditAsync(string title, string token, Document doc, string log)
    {
        DocumentPosting?.Invoke(title, doc);
        var text = NamuFormatter.Default.ToMarkup(doc);
        return PostEditAsync(title, text, log, token);
    }
}
