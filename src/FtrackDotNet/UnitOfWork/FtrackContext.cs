using System.Text.Json;
using FtrackDotNet.Api;
using FtrackDotNet.Models;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet.UnitOfWork;

public class FtrackContext(
    IFtrackDataSetFactory ftrackDataSetFactory,
    IFtrackClient ftrackClient,
    IChangeTracker changeTracker)
{
    public FtrackDataSet<Project> Projects => ftrackDataSetFactory.Create<Project>();
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
                    EntityType = x.Entity.Type,
                    EntityData = x.Entity.Reference.Target!
                },
                TrackedEntityOperationType.Update => (FtrackOperation)new FtrackUpdateOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityKey = x.Entity.Key,
                    EntityData = x.Entity.Reference.Target!
                },
                TrackedEntityOperationType.Delete => (FtrackOperation)new FtrackDeleteOperation()
                {
                    EntityType = x.Entity.Type,
                    EntityKey = x.Entity.Key,
                },
                _ => throw new InvalidOperationException("Unknown operation: " + x.Operation)
            });
        var responses = await ftrackClient.CallAsync<JsonElement>(operations, cancellationToken);

        changeTracker.OnSaved();
    }
}
