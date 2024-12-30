using Microsoft.Extensions.DependencyInjection;
using PockitBook.ViewModels;
using PockitBook.Services;
using ReactiveUI;

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
    public static void AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<MainWindowViewModel>();
        serviceCollection.AddSingleton<RoutingState>();
        serviceCollection.AddSingleton<DataBaseConnector>(ServiceProvider => new DataBaseConnector("pockitbook.db"));
    }
}