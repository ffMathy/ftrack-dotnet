using FtrackDotNet.Linq;
using FtrackDotNet.Models;

namespace FtrackDotNet;

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
