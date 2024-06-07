using System.CommandLine;
using System.CommandLine.Parsing;
namespace Sugarmaple.Bot.CommandLine;
public static class CommandStarter
{
    public static async void InvokeSeveral(this Command cmd, string[] args)
    {
        var buffer = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg[^1] == ';' || i == args.Length - 1)
            {
                var parsedArg = arg.Split(';');
                foreach (var item in parsedArg)
                {
                    if (!string.IsNullOrEmpty(item))
                        buffer.Add(item);
                }
                await cmd.InvokeAsync(buffer.ToArray());
                buffer.Clear();
            }
            else buffer.Add(arg);
        }
    }

    public static void InvokeSeveral(this Command cmd, string raw)
    {
        cmd.InvokeSeveral(raw.Split(' '));
    }
}