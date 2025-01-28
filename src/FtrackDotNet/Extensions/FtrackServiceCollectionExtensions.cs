using Microsoft.Extensions.DependencyInjection;
using FtrackDotNet;
using FtrackDotNet.Clients;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedType.Global
#pragma warning disable CA1050
public static class FtrackServiceCollectionExtensions
#pragma warning restore CA1050
{
    public static IServiceCollection AddFtrack(
        this IServiceCollection services,
        Action<FtrackContextOptions>? configureOptions = null)
    {
        services.AddScoped<FtrackContext>();
        services.AddScoped<IFtrackClient, FtrackClient>();
        
        var builder = services
            .AddOptions<FtrackContextOptions>()
            .ValidateDataAnnotations()
            .BindConfiguration("Ftrack");
        if (configureOptions != null)
        {
            builder = builder.Configure(configureOptions);
        }
        
        return services;
    }
}