using FtrackDotNet.Linq;

namespace FtrackDotNet;

public class TypedContext
{
    public string Name { get; set; }
}

public class Context {}

public class FtrackContext
{
    private readonly FtrackQueryProvider _provider;

    public FtrackContext(IFtrackClient ftrackClient)
    {
        _provider = new FtrackQueryProvider(ftrackClient);
    }

    public IQueryable<TypedContext> TypedContexts => new FtrackQueryable<TypedContext>(_provider);
    public IQueryable<Context> Contexts => new FtrackQueryable<Context>(_provider);
}
