using Sugarmaple.Bot.CommandLine;
using System.CommandLine;

var rootCmd = CreateMainProcessor();
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

MainCommand CreateMainProcessor() {
    var ret = new MainCommand();
    ret.SessionCreated += name => Console.WriteLine($"'{name}'을 생성했습니다.");
    ret.SessionExecuting += name => {
        Console.Clear();
        ConsoleMessage.Default.ShowMessage("OrderStart", name);
    };
    return ret;
}