using FtrackDotNet.Api;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet.Tests;

public class CustomFtrackContext(
    IFtrackDataSetFactory ftrackDataSetFactory, 
    IFtrackClient ftrackClient, 
    IChangeTracker changeTracker) : FtrackContext(ftrackClient, changeTracker)
{
    public FtrackDataSet<Project> Projects => ftrackDataSetFactory.Create<Project>();
    public FtrackDataSet<TypedContext> TypedContexts => ftrackDataSetFactory.Create<TypedContext>();
}