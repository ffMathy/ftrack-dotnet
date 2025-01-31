using FtrackDotNet.Api;
using FtrackDotNet.Models;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet.UnitOfWork;

public class FtrackContext(
    IFtrackDataSetFactory ftrackDataSetFactory,
    IFtrackClient ftrackClient,
    IChangeTracker changeTracker)
{
    public FtrackDataSet<TypedContext> TypedContexts => ftrackDataSetFactory.Create<TypedContext>();
    public FtrackDataSet<Context> Contexts => ftrackDataSetFactory.Create<Context>();
    public FtrackDataSet<ObjectType> ObjectTypes => ftrackDataSetFactory.Create<ObjectType>();
    public FtrackDataSet<Priority> Priorities => ftrackDataSetFactory.Create<Priority>();
    public FtrackDataSet<ProjectSchema> ProjectSchemas => ftrackDataSetFactory.Create<ProjectSchema>();
    public FtrackDataSet<Status> Statuses => ftrackDataSetFactory.Create<Status>();
    public FtrackDataSet<TaskTemplate> TaskTemplates => ftrackDataSetFactory.Create<TaskTemplate>();
    public FtrackDataSet<TaskTypeSchema> TaskTypeSchemas => ftrackDataSetFactory.Create<TaskTypeSchema>();
    public FtrackDataSet<CustomAttributeConfiguration> CustomAttributeConfigurations => ftrackDataSetFactory.Create<CustomAttributeConfiguration>();
    public FtrackDataSet<Type> Types => ftrackDataSetFactory.Create<Type>();
    
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var operations = changeTracker
            .GetChanges()
            .Select(x => x.Operation switch
            {
                TrackedEntityOperationType.Create => (FtrackOperation)new FtrackCreateOperation()
                {
                    EntityType = x.Entity.GetType().Name,
                    EntityData = x.Entity
                },
                TrackedEntityOperationType.Update => (FtrackOperation)new FtrackUpdateOperation()
                {
                    EntityType = x.Entity.GetType().Name,
                    EntityKey = new object(),
                    EntityData = x.Entity
                },
                TrackedEntityOperationType.Delete => (FtrackOperation)new FtrackDeleteOperation()
                {
                    EntityType = x.Entity.GetType().Name,
                    EntityKey = new object(),
                },
                _ => throw new InvalidOperationException("Unknown operation: " + x.Operation)
            });
        await ftrackClient.CallAsync<object>(operations, cancellationToken);

        changeTracker.RefreshSnapshots();
    }
}
