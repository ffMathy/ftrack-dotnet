using FtrackDotNet.Linq;
using FtrackDotNet.Models;
using Task = FtrackDotNet.Models.Task;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet;

public class FtrackContext
{
    private readonly FtrackQueryProvider _provider;

    public FtrackContext(IFtrackClient ftrackClient)
    {
        _provider = new FtrackQueryProvider(ftrackClient);
    }

    public IQueryable<TypedContext> TypedContexts => new FtrackQueryable<TypedContext>(_provider);
    public IQueryable<Context> Contexts => new FtrackQueryable<Context>(_provider);
    public IQueryable<ObjectType> ObjectTypes => new FtrackQueryable<ObjectType>(_provider);
    public IQueryable<ObjectTypeSchema> ObjectTypeSchemas => new FtrackQueryable<ObjectTypeSchema>(_provider);
    public IQueryable<Priority> Priorities => new FtrackQueryable<Priority>(_provider);
    public IQueryable<ProjectSchema> ProjectSchemas => new FtrackQueryable<ProjectSchema>(_provider);
    public IQueryable<Status> Statuses => new FtrackQueryable<Status>(_provider);
    public IQueryable<Task> Tasks => new FtrackQueryable<Task>(_provider);
    public IQueryable<TaskTemplate> TaskTemplates => new FtrackQueryable<TaskTemplate>(_provider);
    public IQueryable<TaskTypeSchema> TaskTypeSchemas => new FtrackQueryable<TaskTypeSchema>(_provider);
    public IQueryable<Type> Types => new FtrackQueryable<Type>(_provider);
}
