using System;

namespace Sugarmaple.Bot.CommandLine;

using System;
using System.CommandLine;

public class EditBacklinkCommand : Command
{
    internal EditBacklinkCommand() : base("backlink", "First Level Subcommand")
    {
        var sourceOption = new Option<string>("--source") { IsRequired = true };
        AddOption(sourceOption);

        var destinationOption = new Option<string?>("--destination", () => null);
        AddOption(destinationOption);

        var destinationDisplayOption = new Option<string>("--destination-display", () => "");
        AddOption(destinationDisplayOption);

        var sourceAnchorOption = new Option<string>("--source-anchor", () => "");
        AddOption(sourceAnchorOption);

        var fromOption = new Option<string>("--from", () => "");
        AddOption(fromOption);

        var customOption = new Option<bool>("--custom");
        AddOption(customOption);

        this.SetHandler((source, destinaion, destinaionDisplay, from, sourceAnchor, custom) =>
        {
            //console.ReplaceBacklick(source, destinaion, destinaionDisplay, from, sourceAnchor, "", custom);
        }, sourceOption, destinationOption, destinationDisplayOption, fromOption, sourceAnchorOption, customOption);
    }

    //var arrayArgument = new Argument<string[]>("array");
    //editBacklinkCommand.Add(arrayArgument);

    //editBacklinkCommand.SetHandler((arrayArgumentValue) =>
    //{
    //    var tuples = arrayArgumentValue.Select((String, Index) => new { String, Index })
    //    .GroupBy(o => o.Index / 2)
    //    .Select(group => (group.ElementAt(0).String, group.ElementAt(1).String));
    //    console.ReplaceBacklink(tuples);
    //}, arrayArgument);
}
