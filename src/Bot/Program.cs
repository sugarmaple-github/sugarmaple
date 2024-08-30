using Sugarmaple.Bot.CommandLine;
using System.CommandLine;

var rootCmd = new MainCommand();
if (args.Length == 0)
{
    Console.WriteLine("Sugarmaple 콘솔에 오신 걸 환영합니다!");
    while (true)
    {
        var input = Console.ReadLine()!;
        rootCmd.Invoke(input);
    }
}
else
{
    rootCmd.Invoke(args);
}

