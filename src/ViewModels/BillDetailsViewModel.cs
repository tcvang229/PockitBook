using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public BillDetailsViewModel(IScreen screen, AccountRepository accountRepository, ScheduledItemRepository scheduledItemRepository)
    {
        HostScreen = screen;
        _accountRepository = accountRepository;
        _scheduledItemRepository = scheduledItemRepository;
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
    /// Binding property for the DueDay element.
    /// </summary>
    public string DueDay
    {
        get => _dueDay;
        set => this.RaiseAndSetIfChanged(ref _dueDay, value);
    }

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

    private readonly AccountRepository _accountRepository;
    private readonly ScheduledItemRepository _scheduledItemRepository;
    private int? _accountId;
    private string _nameOfnewBill = string.Empty;
    private string _dueDay = string.Empty;
    private string _amountDue = string.Empty;
    private ScheduledItemType _selectedType = ScheduledItemType.Bill;
    private string _validationError = string.Empty;

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

        ScheduledItem? scheduledItem = BuildScheduledItem(_accountId.Value, NameOfNewBill, DueDay, AmountDue, SelectedType);
        if (scheduledItem is null)
        {
            ValidationError = "Enter a name, a day of month between 1 and 31, and a numeric amount.";
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
        DueDay = string.Empty;
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
    /// Tries to build a ScheduledItem model. A fixed Monthly recurrence is used here since
    /// this page only collects a day-of-month; other recurrences are set directly in the
    /// database until this page grows a fuller recurrence picker.
    /// </summary>
    public ScheduledItem? BuildScheduledItem(
        int accountId,
        string nameOfNewBill,
        string stringifiedDueDay,
        string stringifiedAmountDue,
        ScheduledItemType type)
    {
        if (string.IsNullOrWhiteSpace(nameOfNewBill))
            return null;

        bool isDueDayOfMonthValid = int.TryParse(stringifiedDueDay, out int dueDay);
        if (!isDueDayOfMonthValid || dueDay > 31 || dueDay < 1)
            return null;

        bool isAmountDueValid = decimal.TryParse(stringifiedAmountDue, out decimal amountDue);
        if (!isAmountDueValid)
            return null;

        DateTime today = DateTime.Today;
        int clampedDay = Math.Min(dueDay, DateTime.DaysInMonth(today.Year, today.Month));
        DateTime anchorDate = new(today.Year, today.Month, clampedDay);

        return new ScheduledItem
        {
            AccountId = accountId,
            Name = nameOfNewBill,
            Type = type,
            ExpectedAmount = amountDue,
            Recurrence = RecurrenceType.Monthly,
            AnchorDate = anchorDate,
            StartDate = today,
            EndDate = null,
            IsActive = true
        };
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
