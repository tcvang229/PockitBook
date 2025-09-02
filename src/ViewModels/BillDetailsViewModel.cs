using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PockitBook.Models;
using PockitBook.Services;
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
    /// <param name="screen"></param>
    /// <param name="dbConnector"></param>
    public BillDetailsViewModel(IScreen screen, DataBaseConnector dbConnector)
    {
        HostScreen = screen;
        _dbConnector = dbConnector;
        AddBillCommand = ReactiveCommand.CreateFromTask(AddBillAsync);

        // Todo: follow factory pattern, that way we could call this method asynchronously
        SetBasicBillsAsync();
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
    /// List of bills.
    /// </summary>
    public ObservableCollection<BasicBillModel> BasicBills { get; set; } = new();

    /// <summary>
    /// Command to add the new bill to the database.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddBillCommand { get; }

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
    /// 
    /// </summary>
    public string AmountDue
    {
        get => _amountDue;
        set => this.RaiseAndSetIfChanged(ref _amountDue, value);
    }

    private DataBaseConnector _dbConnector;
    private string _nameOfnewBill = string.Empty;
    private string _dueDay = string.Empty;
    private string _amountDue = string.Empty;

    /// <summary>
    /// Adds a Basic Bill to the UI and database.
    /// </summary>
    public async Task AddBillAsync()
    {
        BasicBillModel? basicBill = BuildBasicBill(NameOfNewBill, DueDay, AmountDue);

        // TODO: Tell the user that they can't add the new bill because it doesn't meet the requirements
        if (basicBill is null)
            return;

        int rowsAffected = await _dbConnector.AddBasicBillAsync(basicBill);

        // TODO: Tell the user that something went wrong with the database
        if (rowsAffected <= 0)
            return;

        BasicBills.Add(basicBill!);
    }

    /// <summary>
    /// Tries to build a Basic Bill model.
    /// </summary>
    /// <param name="nameOfNewBill"></param>
    /// <param name="dueDayOfMonth"></param>
    /// <param name="amountDue"></param>
    /// <returns></returns>
    public BasicBillModel? BuildBasicBill(string nameOfNewBill, string stringifiedDueDay, string stringifiedAmountDue)
    {
        bool isDueDayOfMonthValid = int.TryParse(stringifiedDueDay, out int dueDay);
        if (isDueDayOfMonthValid && (dueDay > 31 || dueDay < 1))
            return null;

        bool isAmountDueValid = float.TryParse(stringifiedAmountDue, out float amountDue);
        if (!isAmountDueValid)
            return null;

        return new BasicBillModel
        {
            Name = nameOfNewBill,
            DueDayOfMonth = dueDay,
            AmountDue = amountDue
        };
    }

    /// <summary>
    /// Sets the BasicBills by fetching the data in the database.
    /// </summary>
    /// <returns></returns>
    public async Task SetBasicBillsAsync()
    {
        IEnumerable<BasicBillModel>? basicBills = await _dbConnector.GetBasicBillsAsync() ?? Array.Empty<BasicBillModel>();
        BasicBills = new ObservableCollection<BasicBillModel>(basicBills!);
    }
}