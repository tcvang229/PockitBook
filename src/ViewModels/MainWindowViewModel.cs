using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using PockitBook.Services;
using PockitBook.Repositories;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// The Main Window view model.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IScreen
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindowViewModel(
        RoutingState router,
        SqliteDatabase database,
        AccountRepository accountRepository,
        ScheduledItemRepository scheduledItemRepository,
        TransactionRepository transactionRepository,
        BalanceCheckpointRepository balanceCheckpointRepository,
        CsvImportService csvImportService,
        BillCsvImportService billCsvImportService,
        bool isTesting = false)
    {
        GoToBillDetailsView = ReactiveCommand.CreateFromObservable(
            () => NavigateForward(Constants.AppViews.BillDetailsView));

        GoToAccountProjectionView = ReactiveCommand.CreateFromObservable(
            () => NavigateForward(Constants.AppViews.AccountProjectionView));

        Router = router;

        _database = database;
        _accountRepository = accountRepository;
        _scheduledItemRepository = scheduledItemRepository;
        _transactionRepository = transactionRepository;
        _balanceCheckpointRepository = balanceCheckpointRepository;
        _csvImportService = csvImportService;
        _billCsvImportService = billCsvImportService;

        if (!isTesting)
            _ = InitializeAsync();
    }

    /// <summary>
    /// The Router associated with this Screen.
    /// Required by the IScreen interface.
    /// </summary>
    public RoutingState Router { get; }

    /// <summary>
    /// Command to navigate to the previous view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoToPreviousView => Router.NavigateBack;

    /// <summary>
    /// Command to navigate to the Bill Details view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoToBillDetailsView { get; }

    /// <summary>
    /// Command to navigate to the Account Projection view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoToAccountProjectionView { get; }

    private readonly SqliteDatabase _database;
    private readonly AccountRepository _accountRepository;
    private readonly ScheduledItemRepository _scheduledItemRepository;
    private readonly TransactionRepository _transactionRepository;
    private readonly BalanceCheckpointRepository _balanceCheckpointRepository;
    private readonly CsvImportService _csvImportService;
    private readonly BillCsvImportService _billCsvImportService;

    private async Task InitializeAsync()
    {
        await _database.InitializeDataBaseAsync();
        await _accountRepository.EnsureSeedAccountsAsync();
    }

    protected override void OnPageLoadedEventHandler()
    {
        // Show the splash screen.
        Task.Run(async () =>
        {
            var waitTime = 2 * 1000;
            await Task.Delay(waitTime);

            Dispatcher.UIThread.Post(() =>
                NavigateForward(Constants.AppViews.HomeView));
        });
    }

    /// <summary>
    /// Navigates forward to the targeted view.
    /// </summary>
    /// <param name="viewToNavigate"></param>
    /// <returns></returns>
    private IObservable<IRoutableViewModel> NavigateForward(Constants.AppViews viewToNavigate)
    {
        return viewToNavigate switch
        {
            // Todo: instead of new-ing up objects, follow factory pattern. this will allow us to
            // make async calls
            Constants.AppViews.HomeView => Router.Navigate.Execute(new HomeViewModel(this)),
            Constants.AppViews.BillDetailsView => Router.Navigate.Execute(
                new BillDetailsViewModel(this, _accountRepository, _scheduledItemRepository, _billCsvImportService)),
            Constants.AppViews.AccountProjectionView => Router.Navigate.Execute(
                new AccountProjectionViewModel(
                    this,
                    _accountRepository,
                    _scheduledItemRepository,
                    _transactionRepository,
                    _balanceCheckpointRepository,
                    _csvImportService)),
            _ => throw new Exception("Cannot navigate to page, the page does not exist.")
        };
    }
}
