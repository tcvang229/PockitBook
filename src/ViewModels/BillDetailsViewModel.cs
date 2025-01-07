using System;
using System.Collections.ObjectModel;
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
        AddBillCommand = ReactiveCommand.Create(AddBill);
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

    private DataBaseConnector _dbConnector;
    private string _nameOfnewBill = string.Empty;
    private string _dueDay = string.Empty;

    /// <summary>
    /// Adds a Basic Bill to the UI and database.
    /// </summary>
    public void AddBill()
    {
        if (TryBuildBasicBill(NameOfNewBill, DueDay, out var createdBasicBill))
        {
            _dbConnector.AddBasicBill(createdBasicBill!);
            BasicBills.Add(createdBasicBill!);
        }
        else
        {
            // TODO: Tell the user that they can't add the new bill because it doesn't meet the requirements
        }
    }

    /// <summary>
    /// Tries to build a Basic Bill model.
    /// </summary>
    /// <param name="nameOfNewBill"></param>
    /// <param name="dueDayOfMonth"></param>
    /// <param name="createdBasicBill"></param>
    /// <returns></returns>
    public bool TryBuildBasicBill(string nameOfNewBill, string dueDayOfMonth, out BasicBillModel? createdBasicBill)
    {
        if (!int.TryParse(dueDayOfMonth, out var dueDay) || dueDay > 31 || dueDay < 1)
        {
            createdBasicBill = null;
            return false;
        }

        createdBasicBill = new BasicBillModel
        {
            Name = nameOfNewBill,
            DueDayOfMonth = dueDay
        };

        return true;
    }
}