using Microsoft.Extensions.DependencyInjection;
using PockitBook.ViewModels;
using PockitBook.Services;
using PockitBook.Repositories;
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
            serviceProvider => new SqliteDatabase(
                dbName: dbName,
                logger: serviceProvider.GetRequiredService<ILogger<SqliteDatabase>>(),
                isTesting: isTesting
                ));

        serviceCollection.AddSingleton<AccountRepository>();
        serviceCollection.AddSingleton<ScheduledItemRepository>();
        serviceCollection.AddSingleton<TransactionRepository>();
        serviceCollection.AddSingleton<BalanceCheckpointRepository>();
        serviceCollection.AddSingleton<ImportBatchRepository>();
        serviceCollection.AddSingleton<CsvImportService>();
        serviceCollection.AddSingleton<BillCsvImportService>();

        serviceCollection.AddSingleton(
            serviceProvider => new MainWindowViewModel(
                router: serviceProvider.GetRequiredService<RoutingState>(),
                database: serviceProvider.GetRequiredService<SqliteDatabase>(),
                accountRepository: serviceProvider.GetRequiredService<AccountRepository>(),
                scheduledItemRepository: serviceProvider.GetRequiredService<ScheduledItemRepository>(),
                transactionRepository: serviceProvider.GetRequiredService<TransactionRepository>(),
                balanceCheckpointRepository: serviceProvider.GetRequiredService<BalanceCheckpointRepository>(),
                csvImportService: serviceProvider.GetRequiredService<CsvImportService>(),
                billCsvImportService: serviceProvider.GetRequiredService<BillCsvImportService>(),
                isTesting: isTesting
            ));

        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
    }
}