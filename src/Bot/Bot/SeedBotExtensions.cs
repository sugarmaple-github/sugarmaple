﻿namespace Sugarmaple.Bot;

using net.sf.saxon.functions;
using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Namumark;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

public static class SeedBotExtensions
{
    public static IEnumerable<EditView> GetBacklinksFEdit(this SeedBot self, string document, NamespaceMask nsMask, string from)
    {
        var docs = self.GetBacklinks(document, from, (~nsMask).ToNames(self.WikiNamespaces)).Select(o => o.Document);
        if (document.StartsWith("분류:"))
        {
            var categorizedDocs = self.GetCategoryDocument(document);
            docs = docs.Concat(categorizedDocs);
        }

        return docs.Select(o => self.GetEditAsync(o).Result!).Where(o => o != null);
    }

    public static IEnumerable<Document> BacklinkBodies(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, string log)
    {
        var backlinks = _bot.GetBacklinksFEdit(document, @namespace, fromValue);
        foreach (var view in backlinks)
        {
            var doc = DocumentFactory.Default.Parse(view.Text);
            yield return doc;
            view.PostEditAsync(NamuFormatter.Default.ToMarkup(doc), log);
            doc.Dispose();
        }
    }

    public static IEnumerable<IReferer> BacklinkReferers(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, string log)
        => _bot.BacklinkReferers<IReferer>(document, @namespace, fromValue, log);

    public static IEnumerable<T> BacklinkReferers<T>(this SeedBot _bot, string document, NamespaceMask @namespace, string fromValue, string log) where T : IReferer
        => _bot.BacklinkBodies(document, @namespace, fromValue, log).SelectMany(o => o.QuerySelectorAll<T>("*")).Where(o => o.Reference == document);

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
    public static void ReplaceBacklink(this SeedBot _bot,
        string source, string destination,
        string? destinationDisplay = null, string from = "",
        string? sourceAnchor = null, string? destAnchor = null,
        string? log = null, Func<bool>? predicate = null)
    {
        log ??= _bot.LogMakerDict[nameof(ReplaceBacklink)](source, destination);
        if (IsFrame(source) && !IsFrame(destination))
        {
            _bot.ReplaceBacklinkFrameToNotFrame(source, destination, destinationDisplay, from, sourceAnchor, destAnchor, log);
            return;
        }

        if (destinationDisplay == null)
            destinationDisplay = source;
        else if (destinationDisplay == "")
            destinationDisplay = destination;

        var targets = _bot.BacklinkReferers(source, ~NamespaceMask.Wiki, from, log);
        if (sourceAnchor != null)
            targets = targets.Where(o => o is InternalLink i && i.Anchor == sourceAnchor);

        foreach (var o in targets)
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

    private static void ReplaceBacklinkFrameToNotFrame(this SeedBot self, string source, string destination,
        string? destinationDisplay = null, string from = "",
        string? sourceAnchor = null, string? destAnchor = null,
        string? log = null)

    {
        var targets = self.BacklinkReferers(source, ~NamespaceMask.Wiki, from, log);
        if (sourceAnchor != null)
            targets = targets.Where(o => o is InternalLink i && i.Anchor == sourceAnchor);

        foreach (var o in targets)
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

    public static void MakeEditOnly(this SeedBot self, string source, string from)
    {
        var editors = self.BacklinkReferers(source,
            @namespace: ~NamespaceMask.Wiki,
            fromValue: from,
            log: $"[자동 편집] {source} 틀 ##@ 문법 적용");
        foreach (var o in editors)
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

    public static void ReplaceSearch(this SeedBot self, string source, string destination, int page = 1)
    {
        var changePage = false;
        while (true)
        {
            var list = self.Crawler.Search("raw", source, "문서", page);
            if (!list.Any())
                break;
            foreach (var o in list)
            {
                var view = self.GetEditAsync(o).Result;
                if (view == null)
                {
                    changePage = true;
                    continue;
                }
                Debug.Assert(view.Exist);
                var newContent = view.Text.Replace(source, destination);
                view.PostEditAsync(newContent, $"[자동 편집] '{source}' -> '{destination}' 변경");
            }
        }
    }

    //개발 중인 함수
    public static void ReplaceSearch(this SeedBot self)
    {
        const string ColorFrom = "1f2023";

        var page = 1;
        var changePage = false;
        while (true)
        {
            var list = self.Crawler.Search("raw", ColorFrom, "문서", page);
            if (!list.Any())
                break;
            foreach (var o in list)
            {
                var view = self.GetEditAsync(o).Result;
                if (view == null)
                {
                    changePage = true;
                    continue;
                }
                Debug.Assert(view.Exist);

                var newText = view.Text.Replace(ColorFrom, "1c1d1f");
                view.PostEditAsync(newText, $"[자동 편집] 1f2023 -> 1c1d1f 색상 변경");
            }
            if (changePage)
            {
                changePage = false;
                page++;
            }
        }
    }
}

public readonly struct NamespaceMask
{
    public readonly int Value;

    public NamespaceMask(int value)
    {
        Value = value;
    }

    public static readonly NamespaceMask Document = new(1 << 0);
    public static readonly NamespaceMask Frame = new(1 << 1);
    public static readonly NamespaceMask Category = new(1 << 2);
    public static readonly NamespaceMask File = new(1 << 3);
    public static readonly NamespaceMask User = new(1 << 4);
    public static readonly NamespaceMask Special = new(1 << 5);
    public static readonly NamespaceMask Wiki = new(1 << 6);

    public static IReadOnlyCollection<string> DefaultNamespaces =
        new[] { "문서", "틀", "분류", "파일", "사용자", "특수기능" };

    public List<string> ToNames(string[] wikiNamespaces)
    {
        var ret = new List<string>();
        var idx = 0;
        var bit = 1;
        var compared = Value;
        while (compared != 0)
        {
            if ((compared & bit) != 0)
            {
                ret.Add(wikiNamespaces[idx]);
                compared -= bit;
            }
            bit <<= 1;
            idx++;
        }
        return ret;
    }

    public static NamespaceMask operator ~(NamespaceMask value) => new(~value.Value);
}