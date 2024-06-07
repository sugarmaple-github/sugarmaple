//namespace Tests;

//using Sugarmaple.Bot;

//[TestClass]
//public class UnitTest1
//{
//    private SeedBot _bot;

//    public void RemoveRedirect()
//    {
//        var bot = Config.GetSeedBot();
//        var docs = Config.GetDocs();
//        foreach (var doc in docs)
//        {
//            bot.ScheduleRemoveBacklink(doc);
//        }
//    }

//    [DataTestMethod]
//    [DataRow("파일:트위터 아이콘.svg", "파일:X Corp 아이콘(블랙).svg")]
//    public void ReplaceRedirect(string from, string to)
//    {
//        _bot.ScheduleReplaceBacklink(from, to);
//    }

//    [TestInitialize]
//    public void Initialize()
//    {
//        _bot = Config.GetSeedBot();
//    }
//}
//static void DoArcaWork(ConsoleBotController console, string from)
//{
//    string log = @"[자동 편집] 역링크 정리 (""파일:아카라이브 아이콘.svg"" -> ""틀:아카라이브 로고"")";
//    console.ForeachNodeFromBacklinks<FileLink>("파일:아카라이브 아이콘.svg", "문서", from, log, ReplaceNodeIfAble);

//    static bool Contains(string raw, string host)
//    {
//        if (!Uri.TryCreate(raw, UriKind.Absolute, out var url))
//            return false;

//        var tmpHost = url.Host;
//        if (url.Host.StartsWith(host))
//            return true;

//        var dotIndex = tmpHost.IndexOf('.');
//        return tmpHost.AsSpan(dotIndex).StartsWith(host);
//    }

//    static void ReplaceNodeIfAble(FileLink o)
//    {
//        if (o.Reference == "파일:아카라이브 아이콘.svg" &&
//            o.Parent is ExternalLink parent &&
//            Contains(parent.Reference, "arca.live"))
//        {
//            var replacing = GetInclude(parent.Reference, o);
//            if (o.NextSibling != null)
//            {
//                parent.Parent!.InsertBefore(o.NextSibling, parent.NextSibling);
//            }
//            parent.ReplaceWith(replacing);

//            const string BlackLink = "파일:아카라이브 아이콘(블랙).svg";
//            if (replacing.NextSibling is FileLink nextFile && nextFile.Reference == BlackLink)
//                nextFile.Remove();
//            if (replacing.PreviousSibling is FileLink prevFile && prevFile.Reference == BlackLink)
//                prevFile.Remove();
//        }

//        static Include GetInclude(string reference, FileLink childFile)
//        {
//            var uri = new Uri(reference);
//            var left = uri.PathAndQuery[1..];

//            var includeArgs = new Dictionary<string, string>() { { "링크", left } };
//            if (childFile.Width != null)
//            {
//                includeArgs.Add("크기", childFile.Width);
//            }
//            else if (childFile.Height != null)
//            {
//                includeArgs.Add("크기", childFile.Height);
//            }
//            var include = new Include("틀:아카라이브 로고", includeArgs);

//            return include;
//        }
//    }
//}

//static void DoTwitterTask(ConsoleBotController console)
//{
//    string log = @"[자동 편집] 역링크 정리 (""파일:트위터 아이콘.svg"" -> ""틀:트위터 로고"", 외부링크 마크 생략)";
//    console.ForeachNodeFromBacklinks<IParentLink>("파일:트위터 아이콘.svg", "문서", log, ReplaceNodeIfAble);

//    static void ReplaceLinkIfAble(ExternalLink o)
//    {
//        var fileChild = o.Children.OfType<FileLink>();
//        if (IsTwitter(o, fileChild, out FileLink? file))
//        {
//            Change(o, file);
//        }
//        else if (o.Children.Count == 2 && o.Children[0] is Text text && text.OuterMarkup == " " && o.Children[1] is FileLink file3 && MarkupConfig.Icons.Contains(file3.Reference))
//        {
//            text.Remove();
//        }

//        static void Change(ExternalLink o, FileLink childFile)
//        {
//            var uri = new Uri(o.Reference);
//            var left = uri.PathAndQuery[1..];

//            var includeArgs = new Dictionary<string, string>() { { "링크", left } };
//            if (childFile.Width != null)
//            {
//                includeArgs.Add("크기", childFile.Width);
//            }
//            else if (childFile.Height != null)
//            {
//                includeArgs.Add("크기", childFile.Height);
//            }
//            var include = new Include("틀:트위터 로고", includeArgs);
//            o.ReplaceWith(include);

//            if (childFile.NextSibling != null)
//            {
//                include.Parent!.InsertBefore(childFile.NextSibling, include.NextSibling);
//            }
//        }

//        static bool IsTwitter(ExternalLink o, IEnumerable<FileLink> fileChild, out FileLink? output)
//        {
//            if (!Regex.IsMatch(o.Reference, @"\Ghttps?://((mobile|www)\.)?twitter\.com/"))
//            {
//                output = null;
//                return false;
//            }

//            if (fileChild.Count() == 1 && fileChild.FirstOrDefault() is var file && file != null && Regex.IsMatch(file.Reference, @"파일: *트위터 아이콘\.svg"))
//            {
//                output = file;
//                return true;
//            }
//            if (fileChild.FirstOrDefault() is var childFile && childFile != null && Regex.IsMatch(childFile.Reference, @"파일: *트위터 아이콘\.svg") &&
//            o.Children.IndexOf(childFile) is var index && index is 0 or 1)
//            {
//                output = childFile;
//                return true;
//            }

//            output = null;
//            return false;
//        }
//    }

//    static void ReplaceNodeIfAble(IParentLink o)
//    {
//        if (o is ExternalLink ex)
//            ReplaceLinkIfAble(ex);
//        else if (o is InternalLink && o.Children.OfType<FileLink>().FirstOrDefault() is var file && file != null && file.Reference == "파일:트위터 아이콘.svg" &&
//            o.Children.IndexOf(file) is var index && index is 0 or 1)
//        {
//            var includeArgs = new Dictionary<string, string>() { { "링크", @"Url 중 'twitter.com/' 다음 부분 입력" } };
//            if (file.Width != null)
//            {
//                includeArgs.Add("크기", file.Width);
//            }
//            else if (file.Height != null)
//            {
//                includeArgs.Add("크기", file.Height);
//            }
//            var include = new Include("틀:트위터 로고", includeArgs);
//            o.ReplaceWith(include);

//            if (file.NextSibling != null)
//            {
//                include.Parent!.InsertBefore(file.NextSibling, include.NextSibling);
//            }
//        }
//    }
//}

//IEnumerable<ValueTuple<string, string>> GetDocPairs()
//{
//    var raw = FileUtil.GetStrings("/input/docs.txt");
//    return raw.Select(o =>
//    {
//        var splited = o.Split(" → ");
//        return (splited[0], splited[1]);
//    });
//}

//class MarkupConfig
//{
//    public static string[] Icons = FileUtil.GetStrings("icons.txt");
//};