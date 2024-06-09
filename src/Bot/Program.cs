using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using Sugarmaple.Bot.CommandLine;
using System.CommandLine;
using System.Runtime.InteropServices;

Console.WriteLine("[C]ontinue/[R]estart/[E]xit");
const string taskFile = "startTask.json";
string inputStr;

foreach (var dte in DTEHandler.GetInstances())
{
    if (dte != null)
    {
        dte.MainWindow.Activate();
        string older = "", newer = "";
        dte.ExecuteCommand("Tools.DiffFiles",
            $"\"{older}\" \"{newer}\"");

        Console.WriteLine("Visual Studio 인스턴스를 성공적으로 제어했습니다.");
    }
    else
    {
        Console.WriteLine("Visual Studio 인스턴스를 찾을 수 없습니다.");
    }
}

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
Console.WriteLine("모든 작업이 종료되었습니다.");

public class DTEHandler
{
    public static IEnumerable<DTE> GetInstances()
    {
        IRunningObjectTable rot;
        IEnumMoniker enumMoniker;
        int retVal = GetRunningObjectTable(0, out rot);

        if (retVal == 0)
        {
            rot.EnumRunning(out enumMoniker);

            uint fetched = uint.MinValue;
            IMoniker[] moniker = new IMoniker[1];
            while (enumMoniker.Next(1, moniker, out fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);
                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);
                Console.WriteLine("Display Name: {0}", displayName);
                bool isVisualStudio = displayName.StartsWith("!VisualStudio");
                if (isVisualStudio)
                {
                    object obj;
                    rot.GetObject(moniker[0], out obj);
                    var dte = (DTE)obj;
                    yield return dte;
                }
            }
        }
    }

    [DllImport("ole32.dll")]
    private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
}