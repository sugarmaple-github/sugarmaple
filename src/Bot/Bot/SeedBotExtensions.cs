namespace Sugarmaple.Bot;

using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Crawler;
using Sugarmaple.TheSeed.Namumark;
using System;
using System.Diagnostics;
using System.Linq;

public static class SeedBotExtensions
{
    public static IAsyncEnumerable<string> GetBacklinksForEditAsync(this SeedBot self, string document, NamespaceMask nsMask, string from)
    {
        var docs = self.GetBacklinksAsync(document, from, (~nsMask).ToNames(self.WikiNamespaces)).Select(o => o.Document);
        if (document.StartsWith("분류:"))
        {
            var categorizedDocs = self.GetCategoryDocument(document);
            var categorydocs = categorizedDocs.ToAsyncEnumerable();
            docs = docs.Concat(categorydocs);
        }
        return docs;
    }

    public static async IAsyncEnumerable<Document> GetViewsAsync(this IAsyncEnumerable<string> docs, SeedBot self)
    {
        await foreach ((var resp, var title) in docs.SelectAwait(async title => (await self.GetEditAsync(title), title)).Where(o => o.Item1.Item?.Text != null).Select(o => (o.Item1.Item, o.title)))
        {
            var doc = DocumentFactory.Default.Parse(resp!.Text);
            doc.Title = title;
            doc.Token = resp!.Token;
            doc.Bot = self;
            yield return doc;
            doc.Dispose();
        }
    }

    public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
    {
        var waitTask = Task.Run(async () =>
        {
            while (!condition()) await Task.Delay(frequency);
        });

        if (waitTask != await Task.WhenAny(waitTask,
                Task.Delay(timeout)))
            throw new TimeoutException();
    }

    public static IAsyncEnumerable<Document> BacklinkBodiesAsync(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, string log)
    {
        return BacklinkBodiesAsync(_bot, document, @namespace, fromValue, () => log);
    }

    public static IAsyncEnumerable<Document> BacklinkBodiesAsync(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, Func<string> logGenerator)
    {
        throw new Exception();
        //return _bot.GetBacklinksForEditAsync(document, @namespace, fromValue).GetViewsAsync(logGenerator, _bot);
    }

    public static IAsyncEnumerable<IReferer> BacklinkReferersAsync(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, string log)
        => _bot.BacklinkReferersAsync<IReferer>(document, @namespace, fromValue, log);

    public static IAsyncEnumerable<T> BacklinkReferersAsync<T>(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, string log) where T : IReferer
        => _bot.BacklinkBodiesAsync(document, @namespace, fromValue, log).SelectMany(o => o.QuerySelectorAll<T>("*").ToAsyncEnumerable()).Where(o =>
        {
            //NamuNormalizer.Default.Normalize(o);
            return o.Reference == document;
        });

    public static IAsyncEnumerable<T> BacklinkReferersAsync<T>(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, Func<string> logGenerator) where T : IReferer
        => _bot.BacklinkBodiesAsync(document, @namespace, fromValue, logGenerator).SelectMany(o => o.QuerySelectorAll<T>("*").ToAsyncEnumerable()).Where(o =>
        {
            //NamuNormalizer.Default.Normalize(o);
            return o.Reference.Trim() == document;
        });

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_bot"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destinationDisplay">
    /// 대체될 링크의 표기입니다. null이 입력되면, source를, 공백 문자열이 입력되면 destination을 따릅니다.
    /// </param>
    /// <param name="from"></param>
    /// <param name="log"></param>
    public static async Task ReplaceBacklinkAsync(this SeedBot _bot,
        string source, string destination,
        string? destinationDisplay = null, string from = "",
        string? sourceAnchor = null, string? destAnchor = null,
        Func<bool>? predicate = null, Func<string, string, string> logMaker = null)
    {
        var fullLog = logMaker?.Invoke(source, destination);
        if (IsFrame(source) && !IsFrame(destination))
        {
            await _bot.ReplaceBacklinkFrameToNotFrame(source, destination, destinationDisplay, from, sourceAnchor, destAnchor, fullLog);
            return;
        }

        if (destinationDisplay == null)
            destinationDisplay = source;
        else if (destinationDisplay == "")
            destinationDisplay = destination;

        var targets = _bot.BacklinkReferersAsync(source, ~NamespaceMask.Wiki, from, fullLog);
        if (sourceAnchor != null)
            targets = targets.Where(o => o is InternalLink i && i.Anchor == sourceAnchor);

        await foreach (var o in targets)
        {
            if (predicate?.Invoke() != false)
            {
                if (o is InternalLink link)
                {
                    if (link.Children.Count == 0)
                        link.AppendChild(new Text(destinationDisplay));
                    if (destAnchor != null)
                        link.Anchor = destAnchor;
                }
                o.Reference = destination;
            }
        }
    }

    private static async Task ReplaceBacklinkFrameToNotFrame(this SeedBot self, string source, string destination,
        string? destinationDisplay = null, string from = "",
        string? sourceAnchor = null, string? destAnchor = null,
        string? log = null)

    {
        var targets = self.BacklinkReferersAsync(source, ~NamespaceMask.Wiki, from, log);
        if (sourceAnchor != null)
            targets = targets.Where(o => o is InternalLink i && i.Anchor == sourceAnchor);

        await foreach (var o in targets)
        {
            if (o is Include)
            {
                var link = new InternalLink { Reference = destination };
                if (destinationDisplay != null)
                    link.AppendChild(new Text(destinationDisplay));
                o.ReplaceWith(link);
            }
            else
                o.Reference = destination;
        }
    }

    public static async Task MakeEditOnlyAsync(this SeedBot self, string source, string from)
    {
        var editors = self.BacklinkReferersAsync(source,
            @namespace: ~NamespaceMask.Wiki,
            fromValue: from,
            log: $"[자동 편집] {source} 틀 ##@ 문법 적용");
        await foreach (var o in editors)
        {
            if (o is Include include)
            {
                if (include.PreviousSibling is not Text prevText || !prevText.OuterMarkup.EndsWith('\n'))
                    include.Append("\n");
                include.Append("##@");
                if (include.NextSibling is not Text nextText || !nextText.OuterMarkup.StartsWith('\n'))
                    include.Append("\n");
            }
        }
    }
    private static bool IsFrame(string docTitle) => docTitle.StartsWith("틀:");

    /// <summary>
    /// 검색한 문서명을 중복 없이 반환합니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="target">"title_content", "title", "content", "raw" 중 하나를 입력합니다.</param>
    /// <param name="q">검색어</param>
    /// <param name="namespace">이름 공간을 입력 합니다.</param>
    /// <returns></returns>
    public static async IAsyncEnumerable<string> SearchFullAsync(this SeedCrawler self, string target, string q, string @namespace)
    {
        const int maxPage = 500;
        const int resultByPage = 20;
        var curPage = 2;
        var emerged = new HashSet<string>();
        while (true)
        {
            var (_, titles) = self.Search(target, q, @namespace, curPage);
            if (!titles.Any())
                yield break;
            var duplicateHappend = false;
            foreach (var o in titles)
            {
                if (emerged.Add(o))
                {
                    yield return o;
                }
                else duplicateHappend = true;
            }
            if (duplicateHappend)
            {
                var nonDuplicateHappend = false;
                (_, titles) = self.Search(target, q, @namespace, curPage - 1);
                foreach (var oin in titles)
                {
                    if (emerged.Add(oin))
                    {
                        yield return oin;
                        nonDuplicateHappend = true;
                    }
                }
                if (!nonDuplicateHappend) //중복 값으로 폐기 페이지가 가득차면
                    curPage++;
            }
        }
    }

    //public static async Task ReplaceSearchAsync(this SeedBot self, string source, string destination, string target, int page, Func<string> logGenerator)
    //{
    //    await foreach (var o in self.Crawler.SearchFullAsync(target, source, "문서").ToAsyncEnumerable().GetViewsAsync(logGenerator, self))
    //    {
    //        if (o == null) continue;
    //        Debug.Assert(o.Exist); //존재하지 않는 문서를 편집할 수는 없습니다.
    //        var newContent = o.Text.Replace(source, destination);
    //        await o.PostEditAsync(newContent, $"[자동] '{source}' -> '{destination}' 변경 ({log})");
    //    }
    //}
}