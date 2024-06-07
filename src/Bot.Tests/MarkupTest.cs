namespace Tests;
using Sugarmaple.TheSeed.Namumark;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[TestClass]
public class MarkupTest
{
    [TestMethod]
    public void LinkTest()
    {
        var markup = Config.GetMarkup();
        var doc = DocumentFactory.Default.Parse(markup);
        Foreach<IParentLink>(doc,
            o =>
            {
                if (o.Children.Count > 0 && o.Children[0] is FileLink file && file.Reference == "파일:트위터 아이콘.svg")
                {
                    var args = new Dictionary<string, string>() { { "링크", @"Url 중 'twitter.com/' 다음 부분 입력" } };
                    if (file.Width != null)
                    {
                        args.Add("크기", file.Width);
                    }
                    else if (file.Height != null)
                    {
                        args.Add("크기", file.Height);
                    }
                    var include = new Include("틀:트위터 로고", args);
                    o.ReplaceWith(include);
                }
            });
        Config.SetResultMarkup(doc.OuterMarkup);
    }

    private void Foreach<TIn>(Document doc, Action<TIn> action)
    {
        foreach (var child in doc.QuerySelectorAll<TIn>("*")) //need to improve QuerySelector Can get descendents
        {
            action(child);
        }
    }

    private void ChangeAll<TIn>(Document doc, Predicate<TIn> predicate, Func<TIn, Clause> changer) where TIn : Clause
    {
        foreach (var child in doc.QuerySelectorAll("*")) //need to improve QuerySelector Can get descendents
        {
            if (child is TIn o && predicate(o))
            {
                var changed = changer(o);
                child.ReplaceWith(changed);
            }
        }
    }

    private void ChangeAll<TIn>(Document doc, Func<TIn, Clause> changer) where TIn : Clause
    {
        foreach (var child in doc.QuerySelectorAll("*"))
        {
            if (child is TIn o)
            {
                var changed = changer(o);
                child.ReplaceWith(changed);
            }
        }
    }
}
