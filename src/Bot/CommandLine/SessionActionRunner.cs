namespace Sugarmaple.Bot.CommandLine;

using net.sf.saxon.expr.instruct;
using Newtonsoft.Json.Linq;
using Sugarmaple.TheSeed.Namumark;
using System;
using System.Linq;

public class SessionActionRunner
{
    public static async Task Execute(SessionAction sessionAction, SeedBot bot, Dictionary<string, object> config, Action saveHandler)
    {
        if (sessionAction.Type == "config")
        {
            ExecuteConfig((ConfigArgs)sessionAction.Args, config);
        }
        else if (sessionAction.Type == "edit")
        {
            await ExecuteEdit((EditArgs)sessionAction.Args, bot, config, saveHandler);
        }
        else
        {
            throw new Exception();
        }
    }

    private static void ExecuteConfig(ConfigArgs args, Dictionary<string, object> config)
    {
        config[args.Key] = args.Value;
    }

    public static async Task ExecuteEdit(EditArgs args, SeedBot bot, Dictionary<string, object> config, Action saver)
    {
        var env = new Dictionary<string, string>();
        foreach (var scopeCall in args.Scope)
        {
            await foreach (var doc in DocScope(scopeCall, bot, config))
            {
                var editHappened = false;
                foreach (var o in DocToElement(((JObject)scopeCall.Args[1]).ToObject<FunctionCall>(), doc, config))
                {
                    ExecuteEditProcessing((Clause)o, args.Processing, bot, config, env, saver);
                    editHappened = true;
                }
                ((JArray)config["edited"]).Add(doc.Title);
                saver();
                if (editHappened)
                    await doc.Post("[자동] '야마사키 히로코' 링크화");
            }
        }

        //await foreach (var o in args.Scope.ToAsyncEnumerable().SelectMany(o => CallFunctionScope(o, bot, config, env)))
        //{
        //    ExecuteEditProcessing(o, args.Processing, bot, config, env, saver);
        //}
    }

    private static void ExecuteEditProcessing(Clause elem, Conditional[] processing, SeedBot bot, Dictionary<string, object> config, Dictionary<string, string> env, Action saver)
    {
        //몇 번 분기에서 동작하는 지 Log에 기재해야.
        foreach (var o in processing)
        {
            if (CallFunctionCondition(o.Condition, bot, elem))
            {
                switch (o.Then.Name)
                {
                    case "replace":
                        var destination = (string)o.Then.Args[0];
                        env["destination"] = destination;
                        var anchor = o.Then.Args.Length >= 2 ? (string)o.Then.Args[1] : null;
                        config["from"] = elem.OwnerDocument!.Title;
                        if (elem is IAnchorReferer referer)
                        {
                            referer.Reference = destination;
                            if (anchor != null)
                                referer.Anchor = anchor;
                        }
                        else
                        {
                            elem.ReplaceWith(new InternalLink() { Reference = destination });
                        }
                        saver();
                        return;
                    default:
                        throw new Exception();
                }
            }
        }
    }

    public static bool CallFunctionCondition(FunctionCall call, SeedBot bot, Element element)
    {
        var args = call.Args;
        switch (call.Name)
        {
            case "input":
                if (element is IAnchorReferer referer)
                {
                    bot.Viewer.ShowView(referer.OwnerDocument!.Title, true);
                    bot.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(referer.Reference)}')]");
                }
                return Console.ReadLine() == (string)args[0];
            case "true": return true;
            default: throw new Exception();
        }
    }

    public static IEnumerable<Element> DocToElement(FunctionCall call, Document doc, Dictionary<string, object> config)
    {
        var source = (string)call.Args[0];
        return call.Name switch
        {
            "backlink" => doc.QuerySelectorAll<IReferer>("*").Where(o => o.Reference.Trim() == source).Cast<Element>(),
            "search" => SearchDocToElem(source, doc, config),
            "selectText" => SelectText(call.Args, doc, config),
            _ => throw new Exception(),
        };

        static IEnumerable<Element> SelectText(object[] args, Document doc, Dictionary<string, object> config)
        {
            return args.Cast<string>().SelectMany(o => SearchDocToElem(o, doc, config));
        }
    }

    public static IEnumerable<Element> SearchDocToElem(string source, Document doc, Dictionary<string, object> config)
    {
        foreach (var text in doc.QuerySelectorAll<Text>("*").Where(o => o.WholeText.Contains(source)))
        {
            var parent = text.Parent!;
            var splited = text.WholeText.Split(source);
            var inserted = splited.SelectMany((item, index) => new[] { item, source }).SkipLast(1).Select(o => new Text(o)).ToArray();
            foreach (var splitedElem in inserted)
            {
                parent.InsertBefore(splitedElem, text);
            }
            text.Remove();
            foreach (var o in inserted.Select((item, index) => new { item, index }).Where(o => o.index % 2 == 1 && !NearestParent<IReferer>(o.item)).Select(o => o.item))
                yield return o;
        }

        static bool NearestParent<T>(IElement? element)
        {
            if (element is T) return true;
            else if (element is null) return false;
            else return NearestParent<T>(element.Parent);
        }
    }

    public static IAsyncEnumerable<Document> DocScope(FunctionCall call, SeedBot bot, Dictionary<string, object> config)
    {
        var source = (string)call.Args[0];
        return call.Name switch
        {
            "backlink" => bot.GetBacklinksForEditAsync(source, ~NamespaceMask.Wiki, (string)config.GetValueOrDefault("from") ?? "").GetViewsAsync(bot),
            "search" => bot.Crawler.SearchFullAsync(target: "raw", $"\"{source}\"", "").GetViewsAsync(bot),
            "forLink" => ForLink(bot, source, new HashSet<string>(((JArray)config["edited"]).Select(o => o.ToObject<string>()))),
            _ => throw new NotImplementedException(),
        };

        static IAsyncEnumerable<Document> ForLink(SeedBot bot, string source, HashSet<string> edited)
        {
            var resp = bot.GetEditAsync(source).Result.Item!;
            return DocumentFactory.Default.Parse(resp.Text, resp.Token)
                .QuerySelectorAll<IReferer>("*")
                .Where(o => !edited.Contains(o.Reference))
                .Select(o => o.Reference)
                .ToAsyncEnumerable().GetViewsAsync(bot);
        }


    }

    public static IAsyncEnumerable<IAnchorReferer> CallFunctionScope(FunctionCall call, SeedBot bot, Dictionary<string, string> config, Dictionary<string, string> env)
    {
        var source = (string)call.Args[0];
        if (call.Name == "backlink")
        {
            //b.OnGetEditSuccessfully += (document, text) =>
            //{
            //    b.Bot.Viewer.ShowView(document, true);
            //    searchRoutine = b.Bot.Viewer.SearchRoutine($"//a[contains(@href,'{Uri.EscapeDataString(source)}')]");
            //    //enumerator_old = b.Viewer.FindElements(By.XPath("//a[contains(@href,'Repulse%20Stream')]")).GetEnumerator();
            //}
            return bot.BacklinkReferersAsync<IAnchorReferer>(source, ~NamespaceMask.Wiki, config.GetValueOrDefault("from") ?? "",
                () =>
                $"[자동] 역링크 정리 \"{source}\" -> \"{env["destination"]}\" (사유: {config["reason"]})");
        }
        else if (call.Name == "search")
        {

        }
        throw new Exception();
    }
}