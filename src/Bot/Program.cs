using Sugarmaple.Bot.CommandLine;
using System.CommandLine;

Console.WriteLine("[C]ontinue/[R]estart/[E]xit");
const string taskFile = "startTask.json";
string inputStr;

var rootCmd = new MainCommand();
while (true)
{
    var input = Console.ReadLine()![0];
    if (input is 'C' or 'c')
    {
        inputStr = $"order execute {taskFile};";
        break;
    }
    if (input is 'R' or 'r')
    {
        inputStr = $"order reset {taskFile};";
        rootCmd.InvokeSeveral(inputStr);
    }
    if (input is 'E' or 'e')
    {
        return;
    }
    if (input is '?')
    {
        rootCmd.Invoke("?");
    }
}

rootCmd.InvokeSeveral(inputStr);
//Console.WriteLine("모든 작업이 종료되었습니다.");