using FtrackDotNet.Clients;
using FtrackDotNet.Linq;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet;

public class FtrackContext(
    IFtrackClient ftrackClient,
    IFtrackTransactionFactory ftrackTransactionFactory,
    IFtrackTransactionState ftrackTransactionState)
{
    private FtrackQueryProvider Provider => new(ftrackClient, ftrackTransactionState);
    
    public IFtrackTransaction BeginTransaction() => ftrackTransactionFactory.Create();

    public IQueryable<TypedContext> TypedContexts => new FtrackQueryable<TypedContext>(Provider);
    public IQueryable<Context> Contexts => new FtrackQueryable<Context>(Provider);
    public IQueryable<ObjectType> ObjectTypes => new FtrackQueryable<ObjectType>(Provider);
    public IQueryable<Priority> Priorities => new FtrackQueryable<Priority>(Provider);
    public IQueryable<ProjectSchema> ProjectSchemas => new FtrackQueryable<ProjectSchema>(Provider);
    public IQueryable<Status> Statuses => new FtrackQueryable<Status>(Provider);
    public IQueryable<TaskTemplate> TaskTemplates => new FtrackQueryable<TaskTemplate>(Provider);
    public IQueryable<TaskTypeSchema> TaskTypeSchemas => new FtrackQueryable<TaskTypeSchema>(Provider);
    public IQueryable<CustomAttributeConfiguration> CustomAttributeConfigurations => new FtrackQueryable<CustomAttributeConfiguration>(Provider);
    public IQueryable<Type> Types => new FtrackQueryable<Type>(Provider);
}
