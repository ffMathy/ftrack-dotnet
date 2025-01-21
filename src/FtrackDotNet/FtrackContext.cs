using FtrackDotNet.Linq;

namespace FtrackDotNet;

public class FtrackContext
{
    private readonly FtrackContextOptions _options;
    private readonly FtrackQueryProvider _provider;

    public FtrackContext(FtrackContextOptions options)
    { 
        _options = options ?? throw new ArgumentNullException(nameof(options));
        // Create your IQueryProvider with the same options
        _provider = new FtrackQueryProvider(_options);

        // Potentially initialize a lower-level FtrackClient 
        // or something else that uses _options.ServerUrl, _options.ApiKey, etc.
    }

    // public IQueryable<Task> Tasks => new FtrackQueryable<Task>(_provider, null);

    // ... any additional sets
}
