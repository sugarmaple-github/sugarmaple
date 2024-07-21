using Sugarmaple.Bot.CommandLine;
using System.CommandLine;

Console.WriteLine("Sugarmaple 콘솔에 오신 걸 환영합니다!");
const string taskFile = "startTask.json";

var rootCmd = new MainCommand();
Console.WriteLine("[C]ontinue/[R]estart/[E]xit");
while (true)
{
    var input = Console.ReadLine();
    var inputChar = input![0];
    if (inputChar is 'C' or 'c')
    {
        input = $"order execute {taskFile} --check";
    }
    else if (inputChar is 'R' or 'r')
    {
        input = $"order reset {taskFile}";
    }
    else if (inputChar is 'E' or 'e')
    {
        break;
    }
    else if (inputChar is '?')
    {
        rootCmd.Invoke("?");
    }
    await rootCmd.InvokeAsync(input);
}

Console.WriteLine("모든 작업이 종료되었습니다.");