using Sugarmaple.Bot.CommandLine;
using System.CommandLine;

Console.WriteLine("Sugarmaple 콘솔에 오신 걸 환영합니다!");
const string taskFile = "startTask.json";

var rootCmd = new MainCommand();
if (args.Length == 0)
{
    Console.WriteLine("[C]ontinue/[R]estart/[E]xit");
    while (true)
    {
        var input = Console.ReadLine()!;
        rootCmd.Invoke(input);
        //var inputChar = input![0];
        //if (inputChar is 'C' or 'c')
        //{
        //    input = $"order execute {taskFile} --check";
        //}
        //else if (inputChar is 'R' or 'r')
        //{
        //    input = $"order reset {taskFile}";
        //}
        //else if (inputChar is 'E' or 'e')
        //{
        //    break;
        //}
        //else if (inputChar is '?')
        //{
        //    rootCmd.Invoke("?");
        //}
        //await rootCmd.InvokeAsync(input);
    }
}
else
{
    rootCmd.Invoke(args);
}