using Microsoft.Extensions.DependencyInjection;
using PockitBook.ViewModels;
using PockitBook.Services;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using Serilog;

namespace PockitBook.Extensions;

/// <summary>
/// Extension methods for IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services to the application.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="dbName"></param>
    /// <param name="isTesting"></param>
    public static void AddServices(this IServiceCollection serviceCollection, string dbName = "pockitbook.db", bool isTesting = false)
    {
        serviceCollection.AddSingleton<RoutingState>();

        serviceCollection.AddSingleton(
            serviceProvider => new DataBaseConnector(
                dbName: dbName,
                logger: serviceProvider.GetRequiredService<ILogger<DataBaseConnector>>(),
                isTesting: isTesting
                ));

        serviceCollection.AddSingleton(
            serviceProvider => new MainWindowViewModel(
                router: serviceProvider.GetRequiredService<RoutingState>(),
                dbConnector: serviceProvider.GetRequiredService<DataBaseConnector>(),
                isTesting: isTesting
            ));

        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
    }
}