using FtrackDotNet.Api;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Type = FtrackDotNet.Models.Type;

namespace FtrackDotNet.TypeGenerator;

public class CustomFtrackContext(
    IFtrackDataSetFactory ftrackDataSetFactory, 
    IFtrackClient ftrackClient, 
    IChangeTracker changeTracker) : FtrackContext(ftrackClient, changeTracker)
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
}