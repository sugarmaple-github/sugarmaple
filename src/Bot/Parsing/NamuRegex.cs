namespace Sugarmaple.TheSeed.Namumark.Parsing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal static partial class NamuRegex
{
    public static Regex
        Link = LinkInternal();

    [GeneratedRegex(@"\G(?<ref>[^\n]*?)(?<end>]]|\|)")]
    private static partial Regex LinkInternal();
}