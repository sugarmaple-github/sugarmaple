namespace Sugarmaple.TheSeed.Crawler;
using HtmlAgilityPack;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Sugarmaple.TheSeed.Api;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Xml.Linq;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V125.DevToolsSessionDomains;

public class SeedCrawler
{
    public string BaseAddress { get; set; }
    private readonly ChromeDriver _driver;

    static readonly ChromeDriverService _service;

    public Action<int>? OnSearch { get; }

    static SeedCrawler()
    {
        _service = ChromeDriverService.CreateDefaultService();
        _service.HideCommandPromptWindow = true;
    }

    public SeedCrawler(string baseAddress, bool hide = true)
    {
        BaseAddress = baseAddress.TrimEnd('/') + '/';
        var chromeOptions = new ChromeOptions();
        if (hide)
        {
            chromeOptions.AddArguments("headless");
        }
        _driver = new(_service, chromeOptions);
    }

    public RecentChanges GetRecentChanges(LogType logType = LogType.All)
    {
        var logTypeString = logType.ToString().ToLower();
        var docNode = GetDocumentNode($"RecentChanges?logtype={logTypeString}");
        var nodes = docNode.SelectNodes(".//tbody/node()[self::comment() or self::tr]");
        var array = new DocumentChange[nodes.Count / 2];
        for (int i = 0; i < nodes.Count; i += 2)
        {
            var curNode = nodes[i];
            var title = curNode.SelectSingleNode(".//td[1]/a[1]").InnerText;
            var sizeChangeString = curNode.SelectSingleNode(".//td[1]/span/span/text()").InnerText;
            var sizeChange = int.Parse(sizeChangeString);
            var user = curNode.SelectSingleNode(".//td[2]/div/div/a/text()").InnerText;
            var timestamp = curNode.SelectSingleNode(".//td[3]/time/text()").InnerText;
            var time = DateTime.ParseExact(timestamp, "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);

            var comment = nodes[i + 1].SelectSingleNode(".//td/span/text()")?.InnerText ?? "";
            array[i / 2] = new(title, sizeChange, user, time, comment);
        }
        return new RecentChanges(array);
    }

    public SpecialPageList GetNeededPages(string @namespace = "문서", int from = 0) =>
        GetSpecialPageListFrom("NeededPages", @namespace, from.ToString());
    public SpecialPageList GetNeededPagesUntil(string @namespace, int until) =>
        GetSpecialPageListUntil("NeededPages", @namespace, until.ToString());

    public SpecialPageList GetOrphanedPages(string @namespace = "문서", string from = "") =>
        GetSpecialPageListFrom("OrphanedPages", @namespace, from);
    public SpecialPageList GetOrphanedPagesUntil(string @namespace, string until) =>
        GetSpecialPageListUntil("OrphanedPages", @namespace, until);

    public SpecialPageList GetUncategorizedPages(string @namespace = "문서", string from = "") =>
        GetSpecialPageListFrom("UncategorizedPages", @namespace, from);
    public SpecialPageList GetUncategorizedPagesUntil(string @namespace = "문서", string until = "") =>
        GetSpecialPageListUntil("UncategorizedPages", @namespace, until);


    public PageListBySize GetShortestPages()
    {
        var docNode = GetDocumentNode("ShortestPages");
        var docList = docNode.SelectNodes("//article//li/a/text()");
        var sizes = docNode.SelectNodes("//article//li/text()");
        var array = new PageAndSize[docList.Count];
        for (int i = 0; i < docList.Count; i++)
        {
            var sizeRaw = sizes[i].InnerText;
            var indexStart = sizeRaw.IndexOf('(');
            var indexEnd = sizeRaw.IndexOf('글');
            var size = int.Parse(sizeRaw.AsSpan(indexStart, indexEnd - indexStart - 1));
            array[i] = new(docList[i].InnerText, size);
        }
        return new(array);
    }

    public PageListBySize GetLongestPages()
    {
        var docNode = GetDocumentNode("LongestPages");
        var docList = docNode.SelectNodes("//article//li/a/text()");
        var sizes = docNode.SelectNodes("//article//li/text()");
        var array = new PageAndSize[docList.Count];
        for (int i = 0; i < docList.Count; i++)
        {
            var sizeRaw = sizes[i].InnerText;
            var indexStart = sizeRaw.IndexOf('(');
            var indexEnd = sizeRaw.IndexOf('글');
            var size = int.Parse(sizeRaw.AsSpan(indexStart, indexEnd - indexStart - 1));
            array[i] = new(docList[i].InnerText, size);
        }
        return new(array);
    }

    public SpecialPageList GetRandomPage(string @namespace = "문서") =>
        GetSpecialPageList("RandomPage", @namespace);

    public OldPages GetOldPages()
    {
        var docNode = GetDocumentNode("OldPages");
        var docList = docNode.SelectNodes(".//article//li")
            .Select(o => new OldPage(o.SelectSingleNode(".//a[1]/text()").InnerText,
            DateTime.ParseExact(o.SelectSingleNode(".//time/text()").InnerText,
            "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture))).ToArray();
        return new OldPages(docList);
    }

    public DiscussPage GetDiscussPage(string doc)
    {
        GoToUrl($"discuss/{doc}");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        var ul = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("//article//h3[text() = '토론']/following-sibling::ul")));

        //var article = GetArticleNode($"discuss/{doc}");
        //var ul = article.SelectSingleNode(".//h3[text() = '토론']/following-sibling::ul") ?? throw new Exception();
        var list = ul.FindElements(By.XPath(".//li"));
        DiscussionPreview[] array;
        if (list != null)
        {
            array = list.Select(o => new DiscussionPreview()).ToArray();
        }
        else
        {
            array = Array.Empty<DiscussionPreview>();
        }
        return new DiscussPage(array);
    }
    public void ShowDiff(string doc, int rev) => ShowDiff(doc, rev, rev - 1);

    public void ShowDiff(string doc, int rev, int oldrev)
    {
        _driver.Navigate().GoToUrl($"{BaseAddress}diff/{Uri.EscapeDataString(doc)}?rev={rev}&oldrev={oldrev}");
    }

    public void Dispose()
    {
        _driver.Dispose();
    }

    public IEnumerable<string> GetCategoryDocument(string doc)
    {
        //var article = GetArticleNode($"w/{doc}");
        //var docs = article.SelectNodes(".//div[contains(@id,'category')]//li/a/@title");
        //if (docs == null)
        //{
        //    Trace.Write("No Categorized");
        //    return Enumerable.Empty<string>();
        //}
        return GetCategoryDocument(doc, "파일", "", "");
    }

    public IEnumerable<string> GetCategoryDocument(string doc, string @namespace, string shownTitle, string realTitle)
    {
        //var article = GetArticleNode($"w/{doc}");
        //var docs = article.SelectNodes(".//div[contains(@id,'category')]//li/a/@title");
        //if (docs == null)
        //{
        //    Trace.Write("No Categorized");
        //    return Enumerable.Empty<string>();
        //}

        //?namespace=파일&cfrom=%5B"074%20꼬마돌.png","074%20꼬마돌.png"%5D
        var navigator = GetNavigator($"w/{Uri.EscapeDataString(doc)}?namespace={Uri.EscapeDataString(@namespace)}&cfrom=%5B[\"{Uri.EscapeDataString(shownTitle)}\",\"{Uri.EscapeDataString(realTitle)}\"]%5D");
        var result = navigator.Select(".//div[contains(@id,'category')]//li/a/@title");
        return result.Cast<HtmlNodeNavigator>().Select(o => o.Value);
    }

    public HtmlNodeNavigator GetNavigator(string path)
    {
        var article = GetArticleNode(path);
        var navigator = (HtmlNodeNavigator)article.OwnerDocument.CreateNavigator();
        return navigator;
    }

    public IEnumerable<string> GetContribution(string author, string from = "")
    {
        var article = GetArticleNode($"contribution/author/{author}/document?from={from}");
        var docs = article.SelectNodes(".//tr/td[1]/a[1]/text()");
        // //table/preceding-sibling::div/div/a[2]/@href
        return docs.Select(o => o.InnerText);
    }

    public JObject GetInitialState(string path)
    {
        GoToUrl(path);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var tag = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("/html/body/script")));
        var content = tag.Text;
        var start = content.IndexOf('{') + 1;
        var json = JObject.Parse(content[start..^1]);
        return json;
    }

    private IWebElement GoToUrlAndWaitUntil(string path, string xpath)
    {
        GoToUrl(path);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var tag = wait.Until(ExpectedConditions.ElementExists(
            By.XPath(xpath)));
        return tag;
    }

    private void GoToUrl(string path)
    {
        _driver.Navigate().GoToUrl(Combine(BaseAddress, path));
    }

    public (int Count, IEnumerable<string> Titles) Search(string target, string q, string @namespace, int page = 1)
    {
        var count = 0;
        OnSearch?.Invoke(page);
        var section = GoToUrlAndWaitUntil($"Search?target={target}&q={Uri.EscapeDataString(q)}&namespace={@namespace}&page={page}",
            "//article//div[4]/div/section");
        var countText = section.FindElement(By.XPath("./preceding-sibling::div[1]")).Text;
        var first = countText.IndexOf("전체 ") + 3;
        var last = countText.IndexOf(" 건");
        count = int.Parse(countText.AsSpan(first, last - first));

        var docs = section.FindElements(By.XPath("./div/h4/a"));
        Debug.Assert(docs.Count > 0);
        if (docs != null)
            return (count, docs.Select(o => o.Text.Trim(' ', '\n', '\r').Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")).ToArray());
        return (count, Enumerable.Empty<string>());
    }

    public IEnumerable<string> Search_old(string target, string q, string @namespace, int page = 1)
    {
        for (int i = 0; i < 2; i++)
        {
            OnSearch?.Invoke(page);
            var article = GetArticleNode($"Search?target={target}&q={Uri.EscapeDataString(q)}&namespace={@namespace}&page={page}");
            var docs = article.SelectNodes(".//div[4]/div/section/div/h4/a/text()");
            if (docs != null)
                return docs.Select(o => o.InnerHtml.Trim(' ', '\n', '\r').Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"));
        }
        return Enumerable.Empty<string>();
    }

    private SpecialPageList GetSpecialPageListFrom(string path, string? @namespace, string? from)
    {
        ArgumentNullException.ThrowIfNull(from);
        return GetSpecialPageList(path, @namespace, $"&from={from}");
    }
    private SpecialPageList GetSpecialPageListUntil(string path, string? @namespace, string? until)
    {
        ArgumentNullException.ThrowIfNull(until);
        return GetSpecialPageList(path, @namespace, $"&until={until}");
    }

    private SpecialPageList GetSpecialPageList(string path, string? @namespace, string lastParam = "")
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        return GetSpecialPageList($"{path}?namespace={@namespace}{lastParam}");
    }

    private SpecialPageList GetSpecialPageList(string path)
    {
        var docNode = GetDocumentNode(path);
        var namespaces = docNode.SelectNodes(".//option/text()").Select(o => o.InnerText).ToArray();
        var docList = docNode.SelectNodes(".//article//li/a[1]/text()").Select(o => o.InnerText).ToArray();
        return new SpecialPageList(new(namespaces), docList);
    }

    private HtmlNode GetDocumentNode(string path)
    {
        _driver.Navigate().GoToUrl(Combine(BaseAddress, path));
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var appDiv = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("/html/body/div[@id=\"app\"]/div[@class!=\"app-loading\"]")));
        var appRaw = appDiv.GetAttribute("outerHTML");
        var doc = new HtmlDocument();
        doc.LoadHtml(appRaw);
        return doc.DocumentNode;
    }

    [Obsolete("더 이상 유효하지 않음(article을 대기해도 더 기다려야 함.)")]
    private HtmlNode GetArticleNode(string path)
    {
        _driver.Navigate().GoToUrl(Combine(BaseAddress, path));
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        var appDiv = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("//article")));
        var appRaw = appDiv.GetAttribute("outerHTML");
        var doc = new HtmlDocument();
        doc.LoadHtml(appRaw);
        return doc.DocumentNode;
    }

    private static string Combine(string uri1, string uri2)
    {
        uri1 = uri1.TrimEnd('/');
        uri2 = uri2.TrimStart('/');
        return string.Format("{0}/{1}", uri1, uri2);
    }

    public void ShowBacklink(string document)
    {
        _driver.Navigate().GoToUrl($"{BaseAddress}backlink/{Uri.EscapeDataString(document)}");
    }
}

public record DiscussPage(IReadOnlyCollection<DiscussionPreview> OpenDiscussionList);

public class DiscussionPreview
{

}

public class SeedViewer : ChromeDriver
{
    public string BaseAddress { get; set; }
    private ChromeDriver _driver => this;

    static readonly ChromeDriverService _service;
    static SeedViewer()
    {
        _service = ChromeDriverService.CreateDefaultService();
        _service.HideCommandPromptWindow = true;
    }

    public SeedViewer(string baseAddress) : base(_service, new())
    {
        BaseAddress = baseAddress.TrimEnd('/') + '/';
        //options.AddArguments("--auto-open-devtools-for-tabs");
    }

    public void ShowDiff(string doc, int rev) => ShowDiff(doc, rev, rev - 1);

    public void ShowDiff(string doc, int rev, int oldrev)
    {
        GoToUrl($"diff/{Uri.EscapeDataString(doc)}?rev={rev}&oldrev={oldrev}");
    }

    public void ShowView(string document)
    {
        GoToUrl($"w/{Uri.EscapeDataString(document)}");
    }

    private void GoToUrl(string lastUrl)
    {
        Navigate().GoToUrl($"{BaseAddress}{lastUrl}");
    }

    public JObject GetInitialState(string path)
    {
        Navigate().GoToUrl(Combine(BaseAddress, path));
        var wait = new WebDriverWait(this, TimeSpan.FromSeconds(10));
        var tag = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("/html/body/script")));
        var content = tag.Text;
        var start = content.IndexOf('{') + 1;
        var json = JObject.Parse(content[start..^1]);
        return json;
    }

    public void ShowBacklink(string document)
    {
        GoToUrl($"backlink/{Uri.EscapeDataString(document)}");
    }

    private void Highlight(IWebElement element)
    {
        string highlightJavascript = @"arguments[0].style.cssText = ""border-width: 2px; border-style: solid; border-color: red"";";
        ExecuteScript(highlightJavascript, new object[] { element });
        ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'})", element);
    }

    private SpecialPageList GetSpecialPageListFrom(string path, string? @namespace, string? from)
    {
        ArgumentNullException.ThrowIfNull(from);
        return GetSpecialPageList(path, @namespace, $"&from={from}");
    }
    private SpecialPageList GetSpecialPageListUntil(string path, string? @namespace, string? until)
    {
        ArgumentNullException.ThrowIfNull(until);
        return GetSpecialPageList(path, @namespace, $"&until={until}");
    }

    private SpecialPageList GetSpecialPageList(string path, string? @namespace, string lastParam = "")
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        return GetSpecialPageList($"{path}?namespace={@namespace}{lastParam}");
    }

    private SpecialPageList GetSpecialPageList(string path)
    {
        var docNode = GetDocumentNode(path);
        var namespaces = docNode.SelectNodes(".//option/text()").Select(o => o.InnerText).ToArray();
        var docList = docNode.SelectNodes(".//article//li/a[1]/text()").Select(o => o.InnerText).ToArray();
        return new SpecialPageList(new(namespaces), docList);
    }

    private HtmlNode GetDocumentNode(string path)
    {
        _driver.Navigate().GoToUrl(Combine(BaseAddress, path));
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var appDiv = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("/html/body/div[@id=\"app\"]/div[@class!=\"app-loading\"]")));
        var appRaw = appDiv.GetAttribute("outerHTML");
        var doc = new HtmlDocument();
        doc.LoadHtml(appRaw);
        return doc.DocumentNode;
    }

    private HtmlNode GetArticleNode(string path)
    {
        _driver.Navigate().GoToUrl(Combine(BaseAddress, path));
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var appDiv = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("//article")));
        var appRaw = appDiv.GetAttribute("outerHTML");
        var doc = new HtmlDocument();
        doc.LoadHtml(appRaw);
        return doc.DocumentNode;
    }

    private static string Combine(string uri1, string uri2)
    {
        uri1 = uri1.TrimEnd('/');
        uri2 = uri2.TrimStart('/');
        return string.Format("{0}/{1}", uri1, uri2);
    }

    public IEnumerator SearchRoutine(string xPath)
    {
        var elements = _driver.FindElements(By.XPath(xPath));
        Debug.Assert(elements.Count > 0);
        foreach (var elem in elements)
        {
            Highlight(elem);
            yield return null;
        }
    }
}