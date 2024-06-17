namespace Sugarmaple.Bot.CommandLine;

using System.Collections;

public static class OrderCreator
{
    public static OrderDelegate ReplaceBacklink(string source, string destination,
        string? destinationDisplay = null,
        string from = "", string? sourceAnchor = null, string? destAnchor = null, string? log = null, bool context = false) => async (b, s) =>
        {
            IEnumerator? searchRoutine = null;
            //IEnumerator<IWebElement>? enumerator_old = null;
            if (context)
                b.OnGetEditSuccessfully += (document, text) =>
                {
                    b.Bot.Viewer.ShowView(document);
                    searchRoutine = b.Bot.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(source)}')]");
                    //enumerator_old = b.Viewer.FindElements(By.XPath("//a[contains(@href,'Repulse%20Stream')]")).GetEnumerator();
                };

            //페이지를 읽어올 때마다, 다른 페이지 열어서

            //Func<bool>인 이벤트가 필요.
            await b.Bot.ReplaceBacklink(source, destination,
                destinationDisplay: destinationDisplay,
                from: s.From ?? from,
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

    public static OrderDelegate MakeEditOnly(string source, string from) => async (b, c) =>
    {
        await b.Bot.MakeEditOnly(source, from: c.From ?? from);
    };

    public static OrderDelegate SearchReplace(string source, string destination, string target, string log) => async (b, c) =>
    {
        await b.Bot.ReplaceSearch(source, destination, target, c.Page, log);
    };
}
