using Microsoft.Extensions.DependencyInjection;
using PockitBook.Extensions;
using Serilog;

namespace PockitBook.IntegrationTests;

/// <summary>
/// Startup class for integration testing.
/// </summary>
public class TestStartUp
{
    /// <summary>
    /// Builds the ServiceProvider for integration tests.
    /// </summary>
    /// <returns></returns>
    public static ServiceProvider BuildTestServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                path: "Logs/log.txt",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddServices(dbName: dbName, isTesting: true);
        return services.BuildServiceProvider();
    }
}