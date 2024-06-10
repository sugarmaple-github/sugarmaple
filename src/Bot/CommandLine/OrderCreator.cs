namespace Sugarmaple.Bot.CommandLine;

using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;

public static class OrderCreator
{
    public static OrderDelegate ReplaceBacklink(string source, string destination,
        string? destinationDisplay = null,
        string from = "", string? sourceAnchor = null, string? destAnchor = null, string? log = null, bool context = false) => (b, s) =>
        {
            IEnumerator? searchRoutine = null;
            //IEnumerator<IWebElement>? enumerator_old = null;
            if (context)
                s.OnGetEditSuccessfully += (document, text) =>
                {
                    s.SaveProgress("from", document);
                    b.Viewer.ShowView(document);
                    searchRoutine = b.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(source)}')]");
                    //enumerator_old = b.Viewer.FindElements(By.XPath("//a[contains(@href,'Repulse%20Stream')]")).GetEnumerator();
                };

            //페이지를 읽어올 때마다, 다른 페이지 열어서

            //Func<bool>인 이벤트가 필요.
            b.ReplaceBacklink(source, destination,
                destinationDisplay: destinationDisplay,
                from: s.TryGetProgress("from", from),
                sourceAnchor: sourceAnchor,
                destAnchor: destAnchor,
                log: log, predicate: context ? OnCheck : () => true);
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

    public static OrderDelegate MakeEditOnly(string source, string from) => (b, s) =>
    {
        s.AddEvent(b.OnPostSuccessfully, o => s.SaveProgress("from", o.Document));
        b.MakeEditOnly(source, from: s.TryGetProgress("from", from));
    };

    internal static OrderDelegate SearchReplace(string source, string destination, string log) => (b, s) =>
    {
        s.AddEvent(b.Crawler.OnSearch, o => s.SaveProgress("page", o));
        b.ReplaceSearch(source, destination, s.TryGetProgressInt("page", 0), log);
    };
}
