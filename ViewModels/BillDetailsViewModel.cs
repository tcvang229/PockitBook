using System;
using System.Collections.Generic;
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

        var basicBills = new List<BasicBillModel>
        {
            new BasicBillModel{ Name = "Foo", DayOfMonth = 21 },
            new BasicBillModel{ Name = "Zoo", DayOfMonth = 10 },
            new BasicBillModel{ Name = "Koo", DayOfMonth = 3 },
        };

        BasicBills = new ObservableCollection<BasicBillModel>(basicBills);
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
    /// 
    /// </summary>
    public ObservableCollection<BasicBillModel> BasicBills { get; set; } = new();
}