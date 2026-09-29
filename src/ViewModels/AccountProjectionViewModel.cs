using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.VisualElements;
using PockitBook.Models;
using PockitBook.Services;
using PockitBook.Repositories;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// The view model for the Account Projection view - shows the actual balance history
/// (from recorded Transactions) alongside a projected balance (from active ScheduledItems),
/// split at today.
/// </summary>
public partial class AccountProjectionViewModel : ViewModelBase, IRoutableViewModel
{
    private const int ProjectionWindowMonths = 2;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AccountProjectionViewModel(
        IScreen screen,
        AccountRepository accountRepository,
        ScheduledItemRepository scheduledItemRepository,
        TransactionRepository transactionRepository,
        BalanceCheckpointRepository balanceCheckpointRepository,
        CsvImportService csvImportService)
    {
        HostScreen = screen;
        _accountRepository = accountRepository;
        _scheduledItemRepository = scheduledItemRepository;
        _transactionRepository = transactionRepository;
        _balanceCheckpointRepository = balanceCheckpointRepository;
        _csvImportService = csvImportService;
        _projectionCalculator = new ProjectionCalculator();

        OverrideBalanceCommand = ReactiveCommand.CreateFromTask(OverrideBalanceAsync);
    }

    /// <summary>
    /// Reference to IScreen that owns the routable view model.
    /// </summary>
    public IScreen HostScreen { get; }

    /// <summary>
    /// Unique identifier for the routable view model.
    /// </summary>
    public string UrlPathSegment { get; set; } = $"Account Projection page: {Guid.NewGuid().ToString().Substring(0, 5)}";

    /// <summary>
    /// The computed, read-only current balance for the primary account (most recent
    /// BalanceCheckpoint plus transactions since).
    /// </summary>
    public decimal ComputedBalance
    {
        get => _computedBalance;
        set => this.RaiseAndSetIfChanged(ref _computedBalance, value);
    }

    /// <summary>
    /// Binding property for a manual balance override entry.
    /// </summary>
    public string OverrideBalanceInput
    {
        get => _overrideBalanceInput;
        set => this.RaiseAndSetIfChanged(ref _overrideBalanceInput, value);
    }

    /// <summary>
    /// Validation/error feedback for the override input.
    /// </summary>
    public string ValidationError
    {
        get => _validationError;
        set => this.RaiseAndSetIfChanged(ref _validationError, value);
    }

    /// <summary>
    /// Command to write a manual BalanceCheckpoint from OverrideBalanceInput.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OverrideBalanceCommand { get; }

    /// <summary>
    /// The accounts available to import a CSV into.
    /// </summary>
    public ObservableCollection<Account> ImportAccounts { get; set; } = new();

    /// <summary>
    /// The account currently selected as the CSV import target.
    /// </summary>
    public Account? SelectedImportAccount
    {
        get => _selectedImportAccount;
        set => this.RaiseAndSetIfChanged(ref _selectedImportAccount, value);
    }

    /// <summary>
    /// Feedback shown to the user after a CSV import.
    /// </summary>
    public string ImportStatusMessage
    {
        get => _importStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _importStatusMessage, value);
    }

    /// <summary>
    /// The x-axis formatting for the cartesian chart.
    /// </summary>
    public ICartesianAxis[] XAxes
    {
        get => _xAxes;
        set => this.RaiseAndSetIfChanged(ref _xAxes, value);
    }

    /// <summary>
    /// The array of data for graph plots - one actual series and one projected series.
    /// </summary>
    public ISeries[] Series
    {
        get => _series;
        set => this.RaiseAndSetIfChanged(ref _series, value);
    }

    /// <summary>
    /// The characteristics of the graph.
    /// </summary>
    public LabelVisual Title { get; set; } = new LabelVisual
    {
        Text = "Account Projection Chart",
        TextSize = 25,
        Padding = new LiveChartsCore.Drawing.Padding(10)
    };

    private readonly AccountRepository _accountRepository;
    private readonly ScheduledItemRepository _scheduledItemRepository;
    private readonly TransactionRepository _transactionRepository;
    private readonly BalanceCheckpointRepository _balanceCheckpointRepository;
    private readonly CsvImportService _csvImportService;
    private readonly ProjectionCalculator _projectionCalculator;

    private int? _displayAccountId;
    private decimal _computedBalance;
    private string _overrideBalanceInput = string.Empty;
    private string _validationError = string.Empty;
    private Account? _selectedImportAccount;
    private string _importStatusMessage = string.Empty;
    private ICartesianAxis[] _xAxes = [new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMMM dd"))];
    private ISeries[] _series = [];

    /// <inheritdoc />
    protected override void OnPageLoadedEventHandler()
    {
        _ = InitializeAsync();
    }

    /// <summary>
    /// Loads the accounts (for the import picker) and the primary account's projection.
    /// The primary/displayed account is fixed to "Checking" for v1 - see the refactor design
    /// doc's "multi-account UI" cut.
    /// </summary>
    public async Task InitializeAsync()
    {
        IEnumerable<Account> accounts = await _accountRepository.GetAllAsync();
        ImportAccounts = new ObservableCollection<Account>(accounts);
        SelectedImportAccount = ImportAccounts.FirstOrDefault();

        Account? primaryAccount = ImportAccounts.FirstOrDefault(a => a.Name == AccountRepository.SeedAccountNames[0]);
        _displayAccountId = primaryAccount?.Id;

        await UpdateProjectionAsync();
    }

    /// <summary>
    /// Recomputes the actual + projected series and the displayed balance.
    /// </summary>
    private async Task UpdateProjectionAsync()
    {
        if (_displayAccountId is null)
            return;

        int accountId = _displayAccountId.Value;
        DateTime today = DateTime.Today;

        // GetBalanceHistoryAsync always returns at least the anchor point, so its last value is
        // always the current computed balance - GetComputedBalanceAsync exists as a separately
        // testable/reusable piece of this formula (see BalanceCheckpointRepositoryTests), not as
        // the primary path here.
        List<LabeledDateTimePoint> actualPoints = await _balanceCheckpointRepository.GetBalanceHistoryAsync(accountId, today);
        decimal currentBalance = (decimal)actualPoints[^1].Value!;
        ComputedBalance = currentBalance;

        IEnumerable<ScheduledItem> scheduledItems = await _scheduledItemRepository.GetByAccountAsync(accountId);
        DateTime windowEnd = today.AddMonths(ProjectionWindowMonths);
        List<LabeledDateTimePoint> projectedPoints = _projectionCalculator.BuildProjection(currentBalance, today, windowEnd, scheduledItems);

        var actualSeries = new LineSeries<LabeledDateTimePoint>
        {
            Name = "Actual",
            Values = actualPoints,
            Fill = null,
            GeometrySize = 8,
            XToolTipLabelFormatter = point => point.Model!.DateTime.ToString("MMMM dd, yyyy"),
            YToolTipLabelFormatter = point => FormatPointTooltip(point.Model!)
        };

        var projectedSeries = new LineSeries<LabeledDateTimePoint>
        {
            Name = "Projected",
            Values = projectedPoints,
            Fill = null,
            GeometrySize = 8,
            XToolTipLabelFormatter = point => point.Model!.DateTime.ToString("MMMM dd, yyyy"),
            YToolTipLabelFormatter = point => FormatPointTooltip(point.Model!)
        };

        Series = [actualSeries, projectedSeries];

        // XAxes is left as the DateTimeAxis set in its field initializer - LiveCharts scales and
        // labels it automatically from the series' real DateTimePoint values. It used to be
        // overwritten here with a categorical Axis{Labels=...}, which rendered the curve fine but
        // left the x-axis with no visible date labels at all (Labels expects index-based lookups
        // that don't correspond to a date-scaled axis's actual tick positions).
    }

    /// <summary>
    /// Formats a chart point's tooltip text: the label plus, when the point represents an
    /// incremental movement (a bill/income occurrence or a transaction) rather than a hard reset
    /// (a starting-balance anchor or a balance checkpoint), the signed amount that caused it.
    /// </summary>
    private static string FormatPointTooltip(LabeledDateTimePoint point)
    {
        decimal balance = (decimal)point.Value!;

        if (point.Delta is null)
            return $"{point.Label}: {balance:C}";

        string sign = point.Delta.Value >= 0 ? "+" : "-";
        return $"{point.Label}: {sign}{Math.Abs(point.Delta.Value):C} (balance {balance:C})";
    }

    /// <summary>
    /// Writes a manual BalanceCheckpoint from OverrideBalanceInput and refreshes the projection.
    /// </summary>
    private async Task OverrideBalanceAsync()
    {
        ValidationError = string.Empty;

        if (_displayAccountId is null)
        {
            ValidationError = "The account isn't ready yet - please try again in a moment.";
            return;
        }

        bool isValid = decimal.TryParse(OverrideBalanceInput, out decimal newBalance);
        if (!isValid)
        {
            ValidationError = "Enter a valid numeric balance.";
            return;
        }

        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = _displayAccountId.Value,
            Date = DateTime.Today,
            Balance = newBalance,
            Source = TransactionSource.Manual,
            Note = "Manual override"
        });

        OverrideBalanceInput = string.Empty;
        await UpdateProjectionAsync();
    }

    /// <summary>
    /// Imports a CSV file into the currently selected import account, then refreshes the
    /// projection if that account is the one being displayed.
    /// </summary>
    public async Task ImportCsvAsync(string filePath)
    {
        if (SelectedImportAccount?.Id is null)
        {
            ImportStatusMessage = "Select an account first.";
            return;
        }

        CsvImportResult result = await _csvImportService.ImportAsync(SelectedImportAccount.Id.Value, filePath);
        ImportStatusMessage =
            $"Imported {result.ImportedRows} of {result.PostedRows} posted rows " +
            $"({result.DuplicateRows} duplicates skipped, {result.SkippedPendingRows} pending rows not yet posted).";

        if (_displayAccountId is not null && SelectedImportAccount.Id == _displayAccountId.Value)
            await UpdateProjectionAsync();
    }
}
