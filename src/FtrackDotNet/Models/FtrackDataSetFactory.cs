using FtrackDotNet.Api;
using FtrackDotNet.Linq;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Models;

internal class FtrackDataSetFactory(
    IFtrackClient ftrackClient,
    IChangeTracker changeTracker) : IFtrackDataSetFactory
{
    public FtrackDataSet<T> Create<T>() where T : IFtrackEntity
    {
        return new FtrackDataSet<T>(
            changeTracker,
            new FtrackQueryProvider(
                ftrackClient,
                changeTracker));
    }
}