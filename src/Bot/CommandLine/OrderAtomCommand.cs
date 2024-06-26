﻿namespace Sugarmaple.Bot.CommandLine;
using System.CommandLine;

internal class BacklinkCommand : Command
{
    public BacklinkCommand(List<OrderDelegate> orders) : base("backlink")
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
            orders.Add(OrderCreator.MakeEditOnly(source, flag)),
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
            orders.Add(OrderCreator.MakeEditOnly(source, from)),
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
            orders.Add(OrderCreator.ReplaceBacklink(source, destinaion, destinaionDisplay, from, sourceAnchor, destAnchor, log, context));
        }, sourceArg, destinationOption, destinationDisplayOption, fromOption, sourceAnchorOption, destAnchorOption, logOption, contextOption);
        return cmd;
    }
}

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

        cmd.Add(BacklinkCommand.logOption);

        cmd.SetHandler((source, destinaion, target, log) =>
        {
            orders.Add(OrderCreator.SearchReplace(source, destinaion, target, log));
        }, sourceArg, destinationArg, targetOpt, BacklinkCommand.logOption);
        return cmd;
    }
}

internal class OrderAtomCommand : RootCommand
{
    private readonly List<OrderDelegate> _orders = new();

    public OrderAtomCommand()
    {
        Add(new BacklinkCommand(_orders));
        Add(new SearchCommand(_orders));
    }

    public IEnumerable<OrderDelegate> GetOrder()
    {
        var ret = _orders.ToList();
        _orders.Clear();
        return ret;
    }
}
