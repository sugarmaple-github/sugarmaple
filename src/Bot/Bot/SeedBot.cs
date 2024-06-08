#define SingleThread
#define TwitterMVP
#define ApiClientRevise
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

    public event Action<string, Document>? OnPostSameDoc;
    public event Action<EditGetError>? OnLackOfPermission;

    public Action? OnEmergencyHappened = () => throw new Exception("Emergency Happen!");

    public Dictionary<string, LogMaker> LogMakerDict { get; } = new();
    public SeedViewer? _viewer;
    public SeedViewer Viewer { get => _viewer ??= new(WikiUri); }
    public string[] WikiNamespaces { get; internal set; }

    public SeedBot(string wikiUri, string wikiApiUri, string apiToken, string userName, string[] wikiNamespaces) : base(wikiApiUri, apiToken)
    {
        _crawler = new(wikiUri);
        OnGetEditError += o =>
        {
            if (o.IsLackOfPermission)
            {
                OnLackOfPermission?.Invoke(o);
            }
            else
            {

            }
        };
        OnPostSuccessfully.Event += o => NotifyEdit();
        OnPostEditError += o =>
         {
             if (o.HasSameDocumentContent)
             {
                 //OnPostSameDoc?.Invoke(o.Document, docBody);
             }
             else if (o.InvalidRequestBody)
             {
                 Console.WriteLine("Invalid Request Body: 너무 깁니다. 수동으로 편집해주세요.");
             }
         };
        OnBacklinkError += o =>
        {
            Console.WriteLine("역링크 도중에 에러가 발생했습니다.");
            Console.WriteLine(o);
        };

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
}
