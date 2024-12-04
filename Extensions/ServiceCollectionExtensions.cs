using Microsoft.Extensions.DependencyInjection;
using PockitBook.ViewModels;
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
    }
}