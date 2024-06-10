namespace Sugarmaple.Bot.CommandLine;
using Sugarmaple.TheSeed.Api;
using Sugarmaple.TheSeed.Crawler;
using Sugarmaple.TheSeed.Namumark;
using System.Text;

internal class ConsoleBotState
{
    private bool _checkEditMod = true;
    private int _lastEditCountBeforeCheck;
    private int _editTerm;
    private bool _wasChanged;
    private bool _wasFormatted;
    private string? _beforeEdit;

    public event Action<string, string>? DetectingChange;

    public void BeforeEveryEdit(string title, string body)
    {
        _beforeEdit = body;
    }

    public void DocumentPosting(string title, Document body)
    {
        _wasChanged = body.IsChanged;
        _wasFormatted = true;
    }


    public string ApiPosting(string title, string body)
    {
        var ret = body;
        if (_checkEditMod)
        {
            DetectingChange?.Invoke(_beforeEdit, body);
            Console.WriteLine($"{title} 결과:");
            string message;
            if (!_wasFormatted)
                _wasChanged = _beforeEdit != body;


            if (_wasChanged)
            {
                message = "편집을 승인하려면 엔터 키를 눌러주세요. 편집 모드를 중단하려면 s를 누르세요.";
            }
            else
            {
                message = "본 문서의 내용이 똑같습니다. 계속하려면 엔터 키를 눌러주세요. 편집 모드를 중단하려면 s를 누르세요.";
            }
            Console.WriteLine(message);

            var line = Console.ReadLine() ?? throw new Exception("Something Wrong");
            if (line == "s") _checkEditMod = false;
            else if (line.StartsWith("skip"))
            {
                var splited = line!.Split(' ');
                var term = int.Parse(splited[1]);
                _lastEditCountBeforeCheck = _editTerm = term;
                _checkEditMod = false;
            }
            ret = FileUtil.Read("after_edit.txt");
        }
        else if (_editTerm != 0 && --_lastEditCountBeforeCheck <= 0)
        {
            _lastEditCountBeforeCheck = _editTerm;
            Console.Write($"{_lastEditCountBeforeCheck}회 당 검토 중(check를 입력하여 중지합니다.)");
            var line = Console.ReadLine();
            if (line == "check")
                _checkEditMod = true;
        }
        _wasChanged = false;
        _wasFormatted = false;
        return ret;
    }

    public void OnEditWhenNoDiff(string doc, Document revised)
    {
        //Config.SetEditResult(revised.OuterMarkup);
    }

    public void OnBacklink(BacklinkResult result)
    {
        var sb = new StringBuilder();
        sb.Append($"'{result.Document}' 열람 결과:\n").AppendJoin('\n', result.Namespaces.Select(o => $" - {o.Namespace} ({o.Count})"));
        Console.WriteLine(sb);
    }

    public void OnLackOfPermission(EditGetError obj)
    {
        //ConsoleMessage.Default.ShowMessage("LackOfPermission", obj.Data);
        Console.WriteLine($"ACL 권한이 부족하여 수정하지 못했습니다. : '{obj.Document}' 문서\n{obj.Data}");
    }

    public void OnEveryPost(EditPostResult arg)
    {
        Console.WriteLine($"{arg.Document}  r{arg.Rev} 편집 완료.");
        //if (_checkEditMod)
        //    _crawler.ShowDiff(doc, rev);
    }
}

internal class ConsoleBotCreator
{
    public static SeedBot Create(string wikiUri, string wikiApiUri, string apiToken, string userName, string[] wikiNamespaces)
    {
        var state = new ConsoleBotState();
        state.DetectingChange += (older, newer) =>
        {
            FileUtil.Write("before_edit.txt", older);
            FileUtil.Write("after_edit.txt", newer);
        };

        var bot = new SeedBot(wikiUri, wikiApiUri, apiToken, userName, wikiNamespaces);
        bot.OnGetEditSuccessfully += state.BeforeEveryEdit;
        bot.DocumentPosting += state.DocumentPosting;
        bot.ApiPosting += state.ApiPosting;
        bot.OnPostSameDoc += state.OnEditWhenNoDiff;
        bot.OnPostSuccessfully.Event += state.OnEveryPost;
        bot.OnLackOfPermission += state.OnLackOfPermission;
        bot.OnBacklink += state.OnBacklink;
        bot.LogMakerDict["ReplaceBacklink"] = args => $"[자동] 역링크 정리 \"{args[0]}\" -> \"{args[1]}\" (사유: {args[2]})";
        return bot;
    }
}