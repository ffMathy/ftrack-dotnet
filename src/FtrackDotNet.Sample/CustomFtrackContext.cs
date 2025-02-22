using FtrackDotNet.Api;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Sample;

public class CustomFtrackContext(
    IFtrackDataSetFactory ftrackDataSetFactory, 
    IFtrackClient ftrackClient, 
    IChangeTracker changeTracker) : FtrackContext(ftrackClient, changeTracker)
{
    public FtrackDataSet<Project> Projects => ftrackDataSetFactory.Create<Project>();
}