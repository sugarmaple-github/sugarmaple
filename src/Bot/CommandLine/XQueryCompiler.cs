/*namespace Sugarmaple.Bot.CommandLine;

using java.util;
using net.sf.saxon.dotnet;
using net.sf.saxon.om;
using net.sf.saxon.value;
using Saxon.Api;

public class XQueryCompiler
{
    public static readonly XQueryCompiler Default = new();

    private Processor _processor;
    private Saxon.Api.XQueryCompiler _compiler;

    public XQueryCompiler()
    {
        _processor = new Processor();
        _processor.RegisterExtensionFunction(new Function());
        _compiler = _processor.NewXQueryCompiler();

    }

    public void Compile(string xquery)
    {
        _processor.NewDocumentBuilder().Build()
    }
}

public class IterTest : SequenceIterator
{
    public void close()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void forEachOrFail(ItemConsumer consumer)
    {
        throw new NotImplementedException();
    }

    public EnumSet getProperties()
    {
        throw new NotImplementedException();
    }

    public GroundedValue materialize()
    {
        throw new NotImplementedException();
    }

    public Item next()
    {
        throw new NotImplementedException();
    }
}

public class Function : ExtensionFunction
{
    public XdmValue Call(XdmValue[] arguments)
    {
        IEnumerable<int> o = default;

        IEnumerable<XdmAtomicValue> v = o.Select(o => new XdmAtomicValue(o));
        new SequenceExtent(v);

        XdmValue.Wrap(new LazySequence());
        throw new NotImplementedException();
    }

    public XdmSequenceType[] GetArgumentTypes()
    {
        throw new NotImplementedException();
    }

    public QName GetName()
    {
        return new QName("backlink");
    }

    public XdmSequenceType GetResultType()
    {
        throw new NotImplementedException();
    }
}*/