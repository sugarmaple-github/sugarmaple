namespace Sugarmaple.Bot.CommandLine;
using System.CommandLine;
using System.Text;

public class MainCommand : RootCommand
{
    internal MainCommand()
    {
        var wikiUri = FileUtil.GetValue("WikiUri");
        var apiToken = FileUtil.GetValue("ApiToken");
        var userName = FileUtil.GetValue("UserName");
        var wikiNamespaces = FileUtil.GetValues("WikiNamespaces");
        var bot = ConsoleBotCreator.Create("https://namu.wiki", wikiUri, apiToken, userName, wikiNamespaces);
        DefaultBot.Handler = bot;
        Add(new OrderCommand());
    }

    private static void Test(SeedBot bot)
    {
        //bot.ReplaceSearch();
    }
}

public static class CommandSpliter
{
    public static List<string> Split(string commandRaw)
    {
        return ParseCommands(commandRaw);
    }

    public static List<string> ParseCommands(string input)
    {
        List<string> commands = new List<string>();
        StringBuilder currentCommand = new StringBuilder();
        bool insideQuotes = false;

        foreach (char c in input)
        {
            if (c == ';' && !insideQuotes)
            {
                // 세미콜론이 따옴표 안에 없으면 명령어를 추가
                commands.Add(currentCommand.ToString().Trim());
                currentCommand.Clear();
            }
            else
            {
                // 세미콜론이 따옴표 안에 있거나 다른 문자인 경우에는 현재 부분을 그대로 추가
                currentCommand.Append(c);
            }

            // 따옴표 안에 있으면 상태 업데이트
            if (c == '\"' || c == '\'')
            {
                insideQuotes = !insideQuotes;
            }
        }

        // 마지막 명령어 추가
        commands.Add(currentCommand.ToString().Trim());

        return commands;
    }
}