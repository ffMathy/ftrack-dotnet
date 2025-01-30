using System.Linq.Expressions;
using FtrackDotNet.Linq;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Models;

public class FtrackDataSet<T>(
    IChangeTracker changeTracker,
    IFtrackQueryProvider ftrackQueryProvider)
    : FtrackQueryable<T>(ftrackQueryProvider)
    where T : IFtrackEntity
{
    public void Add(T entity)
    {
        changeTracker.TrackEntity(entity, TrackedEntityOperationType.Create);
    }

    public void Remove(T entity)
    {
        changeTracker.TrackEntity(entity, TrackedEntityOperationType.Delete);
    }
    
    public void Attach(T entity)
    {
        changeTracker.TrackEntity(entity, TrackedEntityOperationType.Update);
    }
}