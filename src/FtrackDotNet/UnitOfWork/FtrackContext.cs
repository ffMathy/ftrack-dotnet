using FtrackDotNet.Models;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet.UnitOfWork;

public class FtrackContext(
    IFtrackDataSetFactory ftrackDataSetFactory,
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
        var changes = changeTracker.GetChanges();
        throw new NotImplementedException();

        changeTracker.RefreshSnapshots();
    }
}
