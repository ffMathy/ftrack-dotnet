using FtrackDotNet.Linq;

namespace FtrackDotNet;

public class FtrackContext
{
    private readonly FtrackQueryProvider _provider;

    public FtrackContext(IFtrackClient ftrackClient)
    { 
        _provider = new FtrackQueryProvider(ftrackClient);
    }

    // public IQueryable<TypedContext> Tasks => new FtrackQueryable<TypedContext>(_provider, null);

    // ... any additional sets
}
