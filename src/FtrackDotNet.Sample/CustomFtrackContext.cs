using FtrackDotNet.Api;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Sample;

public class CustomFtrackContext(
    IFtrackClient ftrackClient, 
    IChangeTracker changeTracker) : FtrackContext(ftrackClient, changeTracker)
{
    
}