namespace Sugarmaple.Bot.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NamuWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}
