using Microsoft.Extensions.DependencyInjection;
using FtrackDotNet;
using FtrackDotNet.Api;
using FtrackDotNet.EventHub;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedType.Global
#pragma warning disable CA1050
public static class FtrackServiceCollectionExtensions
#pragma warning restore CA1050
{
    public static IServiceCollection AddFtrack<TFtrackContext>(
        this IServiceCollection services,
        Action<FtrackOptions>? configureOptions = null) where TFtrackContext : FtrackContext
    {
        services.AddScoped<TFtrackContext>();
        services.AddScoped<FtrackContext>(serviceProvider => serviceProvider.GetRequiredService<TFtrackContext>());
        
        var ftrackContextType = typeof(TFtrackContext);
        var ftrackDataSetProperties = ftrackContextType
            .GetProperties()
            .Where(t => 
                t.PropertyType.IsGenericType && 
                t.PropertyType.GetGenericTypeDefinition() == typeof(FtrackDataSet<>));
        foreach(var ftrackDataSetProperty in ftrackDataSetProperties)
        {
            var ftrackType = ftrackDataSetProperty.PropertyType.GetGenericArguments().Single();
            FtrackContext.RegisterFtrackType(ftrackType);
        }
        
        services.AddHttpClient<IFtrackClient, FtrackClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<FtrackOptions>>().CurrentValue;
            client.BaseAddress = new Uri(options.ServerUrl, UriKind.Absolute);
            client.Timeout = options.RequestTimeout ?? Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Add("Ftrack-User", options.ApiUser);
            client.DefaultRequestHeaders.Add("Ftrack-Api-Key", options.ApiKey);
        });
        
        services.AddScoped<ISocketIOFactory, SocketIOFactory>();
        services.AddScoped<IFtrackEventHubClient, FtrackEventHubClient>();
        
        services.AddScoped<IChangeTracker, ChangeTracker>();
        services.AddTransient<IFtrackDataSetFactory, FtrackDataSetFactory>();
        
        var builder = services
            .AddOptions<FtrackOptions>()
            .ValidateDataAnnotations()
            .BindConfiguration("Ftrack");
        if (configureOptions != null)
        {
            builder = builder.Configure(configureOptions);
        }
        
        return services;
    }
}
