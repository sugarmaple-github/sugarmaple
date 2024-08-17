namespace Sugarmaple.Bot.CommandLine;

using Sugarmaple.TheSeed.Namumark;
using System.Collections;

public static class OrderCreator
{
    public static OrderDelegate Backlink(string source, string from) => async o =>
    {
        var b = o.Bot;
        var s = o.Starter;
        o.Queue = b.Bot.BacklinkReferersAsync<InternalLink>(source, ~NamespaceMask.Wiki, o.Starter.From ?? from, o.CreateLog);
        o.SaveLabel();
        //(source, destination) => $"[자동] 역링크 정리 \"{source}\" -> \"{destination}\" (사유: {log})")
    };

    internal static OrderDelegate Replace(string destination) => async o =>
    {
        o.LogMaker = (source) => $"[자동] 역링크 정리 \"{source}\" -> \"{destination}\" (사유: 의뢰 '')";
        await foreach (var item in o.Queue)
        {
            item.ReplaceWith(new InternalLink() { Reference = destination });
        }
        o.Queue = null;
    };

    public static OrderDelegate ReplaceBacklink(string source, string destination,
    string? destinationDisplay = null,
    string from = "", string? sourceAnchor = null, string? destAnchor = null, string? log = null, bool context = false) => async o =>
    {
        var b = o.Bot;
        var s = o.Starter;
        IEnumerator? searchRoutine = null;
        //IEnumerator<IWebElement>? enumerator_old = null;
        if (context)
            b.OnGetEditSuccessfully += (document, text) =>
            {
                b.Bot.Viewer.ShowView(document, true);
                searchRoutine = b.Bot.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(source)}')]");
                //enumerator_old = b.Viewer.FindElements(By.XPath("//a[contains(@href,'Repulse%20Stream')]")).GetEnumerator();
            };

        //페이지를 읽어올 때마다, 다른 페이지 열어서

        //Func<bool>인 이벤트가 필요.
        await b.Bot.ReplaceBacklinkAsync(source, destination,
            destinationDisplay: destinationDisplay,
            from: s.From ?? from,
            sourceAnchor: sourceAnchor,
            destAnchor: destAnchor,
            predicate: context ? OnCheck : () => true,
            logMaker: (source, destination) => $"[자동] 역링크 정리 \"{source}\" -> \"{destination}\" (사유: {log})");
        bool OnCheck()
        {
            if (searchRoutine == null)
                throw new Exception("Something goes wrong");
            searchRoutine.MoveNext();

            while (true)
            {
                var input = Console.ReadLine()!.ToLower();
                if (input == "y")
                    return true;
                if (input == "n")
                    return false;
            }
        }
    };

    public static OrderDelegate MakeEditOnly(string source, string from) => async o =>
    {
        var b = o.Bot;
        var c = o.Starter;
        await b.Bot.MakeEditOnlyAsync(source, from: c.From ?? from);
    };

    public static OrderDelegate SearchReplace(string source, string destination, string target, string log) => async o =>
    {
        var b = o.Bot;
        var c = o.Starter;
        await b.Bot.ReplaceSearchAsync(source, destination, target, c.Page, log);
    };
}

public static class CollectionExtensions
{
    public static async Task EnqueueRangeAsync<T>(this Queue<T> q, IAsyncEnumerable<T> items)
    {
        await foreach (var o in items)
        {
            q.Enqueue(o);
        }
    }
}
