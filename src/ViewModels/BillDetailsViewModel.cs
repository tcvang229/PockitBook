using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using PockitBook.Models;
using PockitBook.Services;
using PockitBook.Repositories;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// The view model for the Bill Tracker view.
/// </summary>
public partial class BillDetailsViewModel : ViewModelBase, IRoutableViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BillDetailsViewModel(
        IScreen screen,
        AccountRepository accountRepository,
        ScheduledItemRepository scheduledItemRepository,
        BillCsvImportService billCsvImportService)
    {
        HostScreen = screen;
        _accountRepository = accountRepository;
        _scheduledItemRepository = scheduledItemRepository;
        _billCsvImportService = billCsvImportService;
        AddBillCommand = ReactiveCommand.CreateFromTask(AddBillAsync);
        DeleteAllBillsCommand = ReactiveCommand.CreateFromTask(DeleteAllBillsAsync);
        DeleteBillCommand = ReactiveCommand.CreateFromTask<ScheduledItem>(DeleteBillAsync);

        // Todo: follow factory pattern, that way we could call this method asynchronously
        InitializeAsync();
    }

    /// <summary>
    /// Reference to IScreen that owns the routable view model.
    /// </summary>
    public IScreen HostScreen { get; }

    /// <summary>
    /// Unique identifier for the routable view model.
    /// </summary>
    public string UrlPathSegment { get; set; } = $"Bill Details page: {Guid.NewGuid().ToString().Substring(0, 5)}";

    /// <summary>
    /// List of scheduled bills/income for the primary account.
    /// </summary>
    public ObservableCollection<ScheduledItem> ScheduledItems { get; set; } = new();

    /// <summary>
    /// The selectable Bill/Income types, for binding to a type-picker control. Static since this
    /// is fixed data, not per-instance state - lets both the add-row ComboBox and the grid's
    /// per-cell editing ComboBox reference it via an x:Static binding without needing to reach
    /// back up to this ViewModel from inside a DataGrid row template.
    /// </summary>
    public static IEnumerable<ScheduledItemType> ScheduledItemTypeOptions { get; } = Enum.GetValues<ScheduledItemType>();

    /// <summary>
    /// The selectable recurrence options, for binding to the add-row's Recurrence picker - same
    /// static x:Static pattern as ScheduledItemTypeOptions.
    /// </summary>
    public static IEnumerable<RecurrenceType> RecurrenceTypeOptions { get; } = Enum.GetValues<RecurrenceType>();

    /// <summary>
    /// Command to add the new item to the database.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddBillCommand { get; }

    /// <summary>
    /// Command to delete all items from the database.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> DeleteAllBillsCommand { get; }

    /// <summary>
    /// Command to delete a single scheduled item, given as the CommandParameter.
    /// </summary>
    public ReactiveCommand<ScheduledItem, System.Reactive.Unit> DeleteBillCommand { get; }

    /// <summary>
    /// Binding property for NameOfNewBill element.
    /// </summary>
    public string NameOfNewBill
    {
        get => _nameOfnewBill;
        set => this.RaiseAndSetIfChanged(ref _nameOfnewBill, value);
    }

    /// <summary>
    /// Binding property for the DueDay/anchor-date element - a day-of-month (e.g. "21") when
    /// SelectedRecurrence is Monthly, or a full date (e.g. "09/18/2026") for every other
    /// recurrence, which needs a specific calendar date rather than a day-of-month to anchor its
    /// interval math to.
    /// </summary>
    public string AnchorInput
    {
        get => _anchorInput;
        set => this.RaiseAndSetIfChanged(ref _anchorInput, value);
    }

    /// <summary>
    /// Binding property for the new item's recurrence.
    /// </summary>
    public RecurrenceType SelectedRecurrence
    {
        get => _selectedRecurrence;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRecurrence, value);
            this.RaisePropertyChanged(nameof(AnchorInputWatermark));
        }
    }

    /// <summary>
    /// The AnchorInput textbox's watermark, switching between a day-of-month example and a full
    /// date example depending on SelectedRecurrence - manually raised from SelectedRecurrence's
    /// setter since this is a derived, not backed, property.
    /// </summary>
    public string AnchorInputWatermark =>
        SelectedRecurrence == RecurrenceType.Monthly ? "E.g., 21 (day of month)" : "E.g., 09/18/2026";

    /// <summary>
    /// Binding property for the AmountDue element.
    /// </summary>
    public string AmountDue
    {
        get => _amountDue;
        set => this.RaiseAndSetIfChanged(ref _amountDue, value);
    }

    /// <summary>
    /// Binding property for whether the new item is a Bill or Income.
    /// </summary>
    public ScheduledItemType SelectedType
    {
        get => _selectedType;
        set => this.RaiseAndSetIfChanged(ref _selectedType, value);
    }

    /// <summary>
    /// Validation/error feedback for the user, empty when there's nothing to show.
    /// </summary>
    public string ValidationError
    {
        get => _validationError;
        set => this.RaiseAndSetIfChanged(ref _validationError, value);
    }

    /// <summary>
    /// Feedback shown to the user after a bill CSV import.
    /// </summary>
    public string ImportStatusMessage
    {
        get => _importStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _importStatusMessage, value);
    }

    private readonly AccountRepository _accountRepository;
    private readonly ScheduledItemRepository _scheduledItemRepository;
    private readonly BillCsvImportService _billCsvImportService;
    private int? _accountId;
    private string _nameOfnewBill = string.Empty;
    private string _anchorInput = string.Empty;
    private string _amountDue = string.Empty;
    private ScheduledItemType _selectedType = ScheduledItemType.Bill;
    private RecurrenceType _selectedRecurrence = RecurrenceType.Monthly;
    private string _validationError = string.Empty;
    private string _importStatusMessage = string.Empty;

    /// <summary>
    /// Resolves the primary account and loads its scheduled items.
    /// </summary>
    public async Task InitializeAsync()
    {
        Account? account = await _accountRepository.GetByNameAsync(AccountRepository.SeedAccountNames[0]);
        _accountId = account?.Id;

        await SetScheduledItemsAsync();
    }

    /// <summary>
    /// Adds a Bill or Income item to the UI and database.
    /// </summary>
    public async Task AddBillAsync()
    {
        ValidationError = string.Empty;

        if (_accountId is null)
        {
            ValidationError = "The account isn't ready yet - please try again in a moment.";
            return;
        }

        ScheduledItem? scheduledItem = BuildScheduledItem(_accountId.Value, NameOfNewBill, AnchorInput, AmountDue, SelectedType, SelectedRecurrence);
        if (scheduledItem is null)
        {
            ValidationError = SelectedRecurrence == RecurrenceType.Monthly
                ? "Enter a name, a day of month between 1 and 31, and a numeric amount."
                : "Enter a name, a valid date (e.g. 09/18/2026), and a numeric amount.";
            return;
        }

        int rowsAffected = await _scheduledItemRepository.AddAsync(scheduledItem);
        if (rowsAffected <= 0)
        {
            ValidationError = "Something went wrong saving this item - please try again.";
            return;
        }

        ScheduledItems.Add(scheduledItem);

        NameOfNewBill = string.Empty;
        AnchorInput = string.Empty;
        AmountDue = string.Empty;
    }

    /// <summary>
    /// Deletes all scheduled items for the primary account.
    /// </summary>
    /// <returns></returns>
    public async Task DeleteAllBillsAsync()
    {
        if (_accountId is null)
            return;

        await _scheduledItemRepository.DeleteAllAsync(_accountId.Value);
        ScheduledItems.Clear();
    }

    /// <summary>
    /// Deletes a single scheduled item.
    /// </summary>
    public async Task DeleteBillAsync(ScheduledItem item)
    {
        if (item.Id is null)
            return;

        await _scheduledItemRepository.DeleteAsync(item.Id.Value);
        ScheduledItems.Remove(item);
    }

    /// <summary>
    /// Persists an in-place edit made directly in the Bill Details grid (name, type, due day, or
    /// amount). Called from the View when a DataGrid cell edit commits.
    /// </summary>
    public async Task UpdateBillAsync(ScheduledItem item)
    {
        if (item.Id is null)
            return;

        int rowsAffected = await _scheduledItemRepository.UpdateAsync(item);
        if (rowsAffected <= 0)
            ValidationError = "Something went wrong saving that change - please try again.";
    }

    /// <summary>
    /// Tries to build a ScheduledItem model. Monthly interprets anchorInput as a day-of-month
    /// (1-31, clamped into the current month); every other recurrence interprets it as a full
    /// date, since a day-of-month alone can't anchor Weekly/Biweekly/OneTime's interval math -
    /// e.g. a biweekly paycheck needs a specific date to count 14-day multiples from.
    /// `recurrence` defaults to Monthly so existing callers (and tests) that don't pass it keep
    /// their old day-of-month behavior unchanged.
    /// </summary>
    public ScheduledItem? BuildScheduledItem(
        int accountId,
        string nameOfNewBill,
        string anchorInput,
        string stringifiedAmountDue,
        ScheduledItemType type,
        RecurrenceType recurrence = RecurrenceType.Monthly)
    {
        if (string.IsNullOrWhiteSpace(nameOfNewBill))
            return null;

        DateTime today = DateTime.Today;
        DateTime anchorDate;

        if (recurrence == RecurrenceType.Monthly)
        {
            bool isDueDayOfMonthValid = int.TryParse(anchorInput, out int dueDay);
            if (!isDueDayOfMonthValid || dueDay > 31 || dueDay < 1)
                return null;

            int clampedDay = Math.Min(dueDay, DateTime.DaysInMonth(today.Year, today.Month));
            anchorDate = new DateTime(today.Year, today.Month, clampedDay);
        }
        else
        {
            bool isAnchorDateValid = DateTime.TryParse(anchorInput, CultureInfo.InvariantCulture, DateTimeStyles.None, out anchorDate);
            if (!isAnchorDateValid)
                return null;
        }

        bool isAmountDueValid = decimal.TryParse(stringifiedAmountDue, out decimal amountDue);
        if (!isAmountDueValid)
            return null;

        // Weekly/Biweekly cadences (paychecks) commonly shift to the preceding business day when
        // the computed date lands on a weekend - Monthly/OneTime items (rent, a one-off payment)
        // don't share that convention. Not yet user-configurable - see
        // ScheduledItem.DateAdjustment's doc comment.
        DateAdjustmentRule dateAdjustment = recurrence is RecurrenceType.Weekly or RecurrenceType.Biweekly
            ? DateAdjustmentRule.NearestPriorBusinessDay
            : DateAdjustmentRule.None;

        return new ScheduledItem
        {
            AccountId = accountId,
            Name = nameOfNewBill,
            Type = type,
            ExpectedAmount = amountDue,
            Recurrence = recurrence,
            AnchorDate = anchorDate,
            StartDate = today,
            EndDate = null,
            IsActive = true,
            DateAdjustment = dateAdjustment
        };
    }

    /// <summary>
    /// Imports bills/income from a CSV file, adding them to whatever scheduled items already
    /// exist for the primary account rather than replacing them - see BillCsvImportService for
    /// the expected column format.
    /// </summary>
    public async Task ImportCsvAsync(string filePath)
    {
        if (_accountId is null)
        {
            ImportStatusMessage = "The account isn't ready yet - please try again in a moment.";
            return;
        }

        BillCsvImportResult result = await _billCsvImportService.ImportAsync(_accountId.Value, filePath);
        ImportStatusMessage = $"Imported {result.ImportedRows} of {result.TotalRows} rows ({result.SkippedRows} skipped).";

        await SetScheduledItemsAsync();
    }

    /// <summary>
    /// Sets the ScheduledItems by fetching the data in the database for the primary account.
    /// </summary>
    /// <returns></returns>
    public async Task SetScheduledItemsAsync()
    {
        if (_accountId is null)
            return;

        IEnumerable<ScheduledItem> scheduledItems = await _scheduledItemRepository.GetByAccountAsync(_accountId.Value);
        ScheduledItems = new ObservableCollection<ScheduledItem>(scheduledItems);
    }
}
