namespace Sugarmaple.TheSeed.Crawler.Tests;
using System.Diagnostics;


[TestClass]
public class SeedCrawlerTest
{
    [TestMethod]
    public void RecentChangesTest()
    {
        var crawler = GetCrawler();
        var pages = crawler.GetRecentChanges();
        var message = Stringify(pages);
        Trace.Write(message);
    }

    [TestMethod]
    public void NeededPagesTest()
    {
        var crawler = GetCrawler();
        var pages = crawler.GetNeededPages();
        var message = Stringify(pages);
        Trace.Write(message);
    }

    [TestMethod]
    public void OrphanedPagesTest()
    {
        var crawler = GetCrawler();
        var pages = crawler.GetOrphanedPages();
        var message = Stringify(pages);
        Trace.Write(message);
    }

    [TestMethod]
    public void UncategorizedPagesTest()
    {
        var crawler = GetCrawler();
        var pages = crawler.GetUncategorizedPages();
        var message = Stringify(pages);
        Trace.Write(message);
    }

    [TestMethod]
    public void RandomPageTest()
    {
        var crawler = GetCrawler();
        var pages = crawler.GetRandomPage();
        var message = Stringify(pages);
        Trace.Write(message);
    }

    public void OldPagessTest()
    {
        var crawler = GetCrawler();
        var pages = crawler.GetOldPages();
        var message = Stringify(pages);
        Trace.Write(message);
    }


    private static SeedCrawler GetCrawler()
    {
        return new SeedCrawler("https://namu.wiki/");
    }

    private static string Stringify<T>(IReadOnlyList<T> list)
    {
        return string.Join("\n", list);
    }
}