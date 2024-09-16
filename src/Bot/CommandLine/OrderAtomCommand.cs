namespace Sugarmaple.Bot.CommandLine;

using Sugarmaple.TheSeed.Namumark;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
[Obsolete]
internal class BacklinkCommand_old : Command
{
    public BacklinkCommand_old(List<OrderDelegate> orders) : base("backlink")
    {
        Add(EditOnly(orders));
        Add(GetEditBacklink(orders));
        Add(Recategory(orders));
        AddAlias("bl");
    }

    public static Option<string> logOption = new("--log", () => "", "편집 요약을 지정합니다.");

    private Command Recategory(List<OrderDelegate> orders)
    {
        var cmd = new Command("recategory");

        var sourceArg = new Argument<string>("source");
        cmd.Add(sourceArg);

        var flagOpt = new Option<string>("--flag", () => "");

        cmd.SetHandler((source, flag) =>
            orders.Add(ProcessorCreator.MakeEditOnly(source, flag)),
        sourceArg, flagOpt);
        return cmd;
    }

    private Command EditOnly(List<OrderDelegate> orders)
    {
        var cmd = new Command("makeeditonly");
        cmd.AddAlias("ed");

        var sourceArg = new Argument<string>("source");
        cmd.Add(sourceArg);

        var fromOption = new Option<string>("--from", () => "");
        cmd.AddOption(fromOption);

        cmd.SetHandler((source, from) =>
            orders.Add(ProcessorCreator.MakeEditOnly(source, from)),
        sourceArg, fromOption);
        return cmd;
    }


    private Command GetEditBacklink(List<OrderDelegate> orders)
    {
        var cmd = new Command("replace");
        cmd.AddAlias("re");

        var sourceArg = new Argument<string>("source");
        cmd.Add(sourceArg);

        var destinationOption = new Argument<string>("destination");
        cmd.Add(destinationOption);

        var destinationDisplayOption = new Option<string?>("--destination-display", () => null);
        destinationDisplayOption.AddAlias("-d|");
        cmd.AddOption(destinationDisplayOption);

        var sourceAnchorOption = new Option<string?>("--source-anchor", () => null);
        sourceAnchorOption.AddAlias("-s#");
        cmd.AddOption(sourceAnchorOption);

        var destAnchorOption = new Option<string?>("--destination-anchor", () => null);
        destAnchorOption.AddAlias("-d#");
        cmd.AddOption(destAnchorOption);

        var fromOption = new Option<string>("--from", () => "");
        cmd.AddOption(fromOption);

        var contextOption = new Option<bool>("--context", () => false);
        cmd.AddOption(contextOption);

        cmd.AddOption(logOption);

        cmd.SetHandler((source, destinaion, destinaionDisplay, from, sourceAnchor, destAnchor, log, context) =>
        {
            orders.Add(ProcessorCreator.ReplaceBacklink(source, destinaion, destinaionDisplay, from, sourceAnchor, destAnchor, log, context));
        }, sourceArg, destinationOption, destinationDisplayOption, fromOption, sourceAnchorOption, destAnchorOption, logOption, contextOption);
        return cmd;
    }
}
[Obsolete]
internal class SearchCommand : Command
{
    public SearchCommand(List<OrderDelegate> orders) : base("search")
    {
        Add(Replace(orders));
    }

    private Command Replace(List<OrderDelegate> orders)
    {
        var cmd = new Command("replace");
        cmd.AddAlias("re");

        var sourceArg = new Argument<string>("source");
        cmd.Add(sourceArg);

        var destinationArg = new Argument<string>("destination");
        cmd.Add(destinationArg);

        var targetOpt = new Option<string>("destination", () => "content");
        cmd.Add(destinationArg);

        cmd.Add(BacklinkCommand_old.logOption);

        cmd.SetHandler((source, destinaion, target, log) =>
        {
            orders.Add(ProcessorCreator.SearchReplace(source, destinaion, target, log));
        }, sourceArg, destinationArg, targetOpt, BacklinkCommand_old.logOption);
        return cmd;
    }
}
[Obsolete]
internal class OrderAtomCommand : RootCommand
{
    //[Obsolete]
    //private readonly List<OrderDelegate> _orders = new();

    public OrderAtomCommand(OrderCompileInfo context)
    {
        Add(BacklinkCmd(context));
        Add(ReplaceCmd(context));
        Add(ConfigCmd(context));

        //Add(new BacklinkCommand_old(_orders));
        //Add(new SearchCommand(_orders));
    }

    private Command ConfigCmd(OrderCompileInfo context)
    {
        var cmd = new Command("config");
        var reason = new Command("reason");
        var reasonArg = new Argument<string>("reasonArg");
        cmd.Add(reason);
        reason.Add(reasonArg);
        reason.SetHandler(o =>
        {
            context.SavedLabel = context.Label;
            context.Processors.Add((ProcessorCreator.Config("reason", o), context.SavedLabel));
        }, reasonArg);
        return cmd;
    }

    private Command BacklinkCmd(OrderCompileInfo context)
    {
        var cmd = new Command("backlink");

        var sourceArg = new Argument<string>("source");
        cmd.Add(sourceArg);

        var fromOption = new Option<string>("--from", () => "");
        cmd.AddOption(fromOption);

        cmd.SetHandler((s, f) =>
        {
            context.Level = 1;
            context.SavedLabel = context.Label;
            context.Target = (ProcessorCreator.Backlink(s, f));
        }, sourceArg, fromOption);

        return cmd;
    }

    private Command ReplaceCmd(OrderCompileInfo context)
    {
        var cmd = new Command("replace");

        var destinationArg = new Argument<string>("destination");
        cmd.Add(destinationArg);

        cmd.SetHandler(d =>
        {
            Trace.Assert(context.Level >= 1);
            context.Level--;
            context.Processors.Add((ProcessorCreator.Replace(context.Target, d), context.SavedLabel));
        }, destinationArg);

        return cmd;
    }

    //public IEnumerable<OrderDelegate> GetOrder()
    //{
    //    var ret = _orders.ToList();
    //    _orders.Clear();
    //    return ret;
    //}
}
[Obsolete]
public class OrderCompileInfo
{
    public List<(OrderDelegate Processor, int Label)> Processors = new();
    public int Level;
    public int Label;
    public int SavedLabel;

    public Func<OrderContext, IAsyncEnumerable<InternalLink>> Target { get; internal set; }
}