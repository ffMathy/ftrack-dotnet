using FtrackDotNet.Clients;
using FtrackDotNet.Linq;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Models;

internal class FtrackDataSetFactory(
    IFtrackClient ftrackClient) : IFtrackDataSetFactory
{
    public FtrackDataSet<T> Create<T>() where T : IFtrackEntity
    {
        var changeTracker = new ChangeTracker();
        return new FtrackDataSet<T>(
            changeTracker,
            new FtrackQueryProvider(
                ftrackClient,
                changeTracker));
    }
}