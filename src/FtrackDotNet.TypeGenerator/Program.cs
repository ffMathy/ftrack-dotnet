using FtrackDotNet;
using FtrackDotNet.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder();
hostBuilder.ConfigureAppConfiguration(x => x
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables());
hostBuilder.ConfigureServices(services =>
    services.AddFtrack());

using var host = hostBuilder.Build();
await using var scope = host.Services.CreateAsyncScope();

var ftrackContext = scope.ServiceProvider.GetRequiredService<FtrackContext>();
var ftrackClient = scope.ServiceProvider.GetRequiredService<IFtrackClient>();

var schemas = await ftrackClient.QuerySchemasAsync();

var types = await ftrackContext.Types
    .OrderBy(x => x.Sort)
    .Select(x => new
    {
        x.IsBillable,
        x.Name,
        x.Sort,
        x.TaskTypeSchemas
    })
    .ToArrayAsync();

var priorities = await ftrackContext.Priorities
    .OrderBy(x => x.Sort)
    .Select(x => new
    {
        x.Id,
        x.Color,
        x.Name,
        x.Sort,
        x.Value
    })
    .ToArrayAsync();

var statuses = await ftrackContext.Statuses
    .OrderBy(x => x.Sort)
    .Select(x => new
    {
        x.Id,
        x.Color,
        x.IsActive,
        x.Name,
        x.Sort,
        x.State
    })
    .ToArrayAsync();

var objectTypes = await ftrackContext.ObjectTypes
    .OrderBy(x => x.Sort)
    .Select(x => new
    {
        x.Id,
        x.IsLeaf,
        x.IsPrioritizable,
        x.IsSchedulable,
        x.IsStatusable,
        x.IsTaskable,
        x.IsTimeReportable,
        x.IsTypeable,
        x.Name,
        x.ProjectSchemas
    })
    .ToArrayAsync();

var projectSchemas = await ftrackContext.ProjectSchemas
    .Select(x => new
    {
        x.Name,
        x.AssetVersionWorkflowSchema,
        x.ObjectTypeSchemas,
        x.ObjectTypes,
        x.TaskTemplates,
        x.TaskTypeSchema,
        x.TaskWorkflowSchema,
        x.TaskWorkflowSchemaOverrides
    })
    .ToArrayAsync();