using Microsoft.Extensions.DependencyInjection;
using FtrackDotNet;
using FtrackDotNet.Clients;
using FtrackDotNet.EventHub;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedType.Global
#pragma warning disable CA1050
public static class FtrackServiceCollectionExtensions
#pragma warning restore CA1050
{
    public static IServiceCollection AddFtrack(
        this IServiceCollection services,
        Action<FtrackOptions>? configureOptions = null)
    {
        services.AddTransient<FtrackContext>();
        services.AddTransient<IFtrackClient, FtrackClient>();
        
        services.AddScoped<ISocketIOFactory, SocketIOFactory>();
        services.AddScoped<IFtrackEventHubClient, FtrackEventHubClient>();
        
        services.AddTransient<IChangeDetector, ChangeDetector>();
        services.AddTransient<IFtrackTransactionFactory, FtrackTransactionFactory>();
        services.AddSingleton<IFtrackTransactionState, FtrackTransactionState>();
        
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