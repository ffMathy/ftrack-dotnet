﻿using System.Text;
using System.Text.Json;
using FtrackDotNet;
using FtrackDotNet.Api;
using FtrackDotNet.Extensions;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Task = FtrackDotNet.Models.Task;

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
        x.ObjectTypeSchemas,
        x.ObjectTypes,
        x.TaskTemplates,
        x.TaskWorkflowSchemaOverrides
    })
    .ToArrayAsync();

var outputBuilder = new StringBuilder();
outputBuilder.AppendLine("// <auto-generated>");
outputBuilder.AppendLine("// This file was generated by a tool; any manual changes may be lost.");
outputBuilder.AppendLine("// </auto-generated>");
outputBuilder.AppendLine();
outputBuilder.AppendLine("using System.Text.Json;");
outputBuilder.AppendLine("using System.Text.Json.Serialization;");
outputBuilder.AppendLine();
outputBuilder.AppendLine("#pragma warning disable CS8669");
outputBuilder.AppendLine();
outputBuilder.AppendLine("namespace FtrackDotNet.Models;");
outputBuilder.AppendLine();

var typeNamesAlreadyInInbuiltModels = typeof(FtrackContext).Assembly
    .GetTypes()
    .Where(x => x.Namespace == "FtrackDotNet.Models")
    .Select(x => x.Name)
    .ToArray();

var typedContextSchema = schemas.Single(x => x.Id == nameof(TypedContext));
foreach (var schema in schemas)
{
    var className = schema.Id;
    if(typeNamesAlreadyInInbuiltModels.Contains(className))
    {
        continue;
    }
    
    var isTypedContextSchema =
        schema.AliasFor.ValueKind == JsonValueKind.Object && 
        schema.AliasFor.GetProperty("id").GetString() == nameof(Task);
    var baseSchema = isTypedContextSchema ? 
        typedContextSchema :
        schemas.SingleOrDefault(x => x.Id == schema.Mixin?.Ref);

    outputBuilder.Append($"public partial record {className} : ");
    if (baseSchema != null)
    {
        outputBuilder.Append($"{baseSchema.Id}");
    }
    else
    {
        outputBuilder.AppendLine("IFtrackEntity");
    }
    
    outputBuilder.AppendLine(" {");

    var propertyAccessModifier = baseSchema != null ? "override" : "virtual";
    outputBuilder.AppendLine($"\t[JsonPropertyName(\"__entity_type__\")]");
    outputBuilder.AppendLine($"\tpublic {propertyAccessModifier} string __entity_type__ => \"{className}\";");
    
    outputBuilder.AppendLine($"\tpublic {propertyAccessModifier} FtrackPrimaryKey[] GetPrimaryKeys() => new [] {{ {schema
        .PrimaryKey
        .Select(p => $"new FtrackPrimaryKey {{ Name = \"{p.FromSnakeCaseToPascalCase()}\", Value = {p.FromSnakeCaseToPascalCase()} }}")
        .Aggregate((x, y) => $"{x}, {y}")} }};");

    var properties = schema.Properties
        .Where(x =>
            !x.Key.StartsWith("_") &&
            baseSchema?.Properties.ContainsKey(x.Key) != true &&
            x.Value.Type != null)
        .ToArray();
    foreach (var property in properties)
    {
        var csharpType = property.Value.Type switch
        {
            "integer" => "int",
            "variable" => "JsonElement",
            "string" => "string",
            "boolean" => "bool",
            "object" => "object",
            "number" => "double",
            "array" or "mapped_array" => property.Value.Items switch
            {
                var items => $"{items.Ref}[]"
            },
            var type => throw new NotImplementedException(type)
        };
        
        var csharpValueTypes = new[] { "int", "bool", "double" };
        var isSimpleCsharpType = csharpValueTypes.Contains(csharpType);
        
        var isRequired = schema.Required.Contains(property.Key);
        var isPrimaryKey = schema.PrimaryKey.Contains(property.Key);
        
        outputBuilder.AppendLine($"\tpublic {csharpType}{(isRequired ? "" : "?")} {property.Key.FromSnakeCaseToPascalCase()} {{ get; {(isPrimaryKey ? "init" : "set")}; }}{(isRequired && !isSimpleCsharpType ? " = null!;" : "")}");
    }

    outputBuilder.AppendLine("}");
}

await File.WriteAllTextAsync("Generated.cs", outputBuilder.ToString());

Console.WriteLine("Done!");