using System;
using LiveChartsCore.Defaults;
using ReactiveUI;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.VisualElements;
using PockitBook.Services;
using System.Threading.Tasks;
using PockitBook.Models;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore.Kernel.Sketches;

namespace PockitBook.ViewModels;

public partial class AccountProjectionViewModel : ViewModelBase, IRoutableViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="dbConnector"></param>
    public AccountProjectionViewModel(IScreen screen, DataBaseConnector dataBaseConnector)
    {
        HostScreen = screen;
        _databaseConnector = dataBaseConnector;
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
    /// The account balance to add or substract bill amounts from.
    /// </summary>
    public string AccountBalance
    {
        get => _accountBalance;
        set
        {
            this.RaiseAndSetIfChanged(ref _accountBalance, value);
            _ = UpdateProjectionAsync();
        }
    }

    /// <summary>
    /// The x-axis formatting for the cartesian chart.
    /// </summary>
    public ICartesianAxis[] XAxes { get; set; } = [
        new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMMM dd"))
    ];

    /// <summary>
    /// The array of data for graph plots.
    /// </summary>
    public ISeries[] Series { get; set; } = [];

    /// <summary>
    /// The characteristics of the graph.
    /// </summary>
    public LabelVisual Title { get; set; } = new LabelVisual
    {
        Text = "My Chart Title",
        TextSize = 25,
        Padding = new LiveChartsCore.Drawing.Padding(10)
    };

    private string _accountBalance = string.Empty;

    private DataBaseConnector _databaseConnector;

    public long OneDayTicks => TimeSpan.FromDays(1).Ticks;

    private async Task UpdateProjectionAsync()
    {
        LineSeries<DateTimePoint> lineSeries = await RecalculateAccountProjectionAsync();
        Series = new ISeries[]
        {
            lineSeries
        };

        XAxes = [
            new Axis
            {
                Labels = lineSeries.Values?.Select(x => x.DateTime.ToString("yyyy MMM dd")).ToArray()
            }
        ];

        this.RaisePropertyChanged(nameof(Series));
    }

    private float? ValidateAccountBalance()
    {
        bool isAccountBalanceValid = float.TryParse(_accountBalance, out float accountBalance);
        if (!isAccountBalanceValid)
            return null;

        return accountBalance;
    }

    private async Task<LineSeries<DateTimePoint>> RecalculateAccountProjectionAsync()
    {
        float? accountBalance = ValidateAccountBalance();
        if (accountBalance is null)
            return new LineSeries<DateTimePoint>();

        IEnumerable<BasicBillModel>? basicBills = await _databaseConnector.GetBasicBillsAsync();
        if (basicBills is null)
            return new LineSeries<DateTimePoint>();

        // Assume bills are due in the current month/year (you can adjust logic as needed)
        DateTime today = DateTime.Today;
        IOrderedEnumerable<BasicBillModel> billsOrdered = basicBills.OrderBy(b => b.DueDayOfMonth);

        List<DateTimePoint> points = new();

        foreach (BasicBillModel bill in billsOrdered)
        {
            (DateTimePoint dateTimePoint, float newAccountBalance) = HandleBill(bill, today, (float)accountBalance);

            accountBalance = newAccountBalance;
            points.Add(dateTimePoint);
        }

        return new LineSeries<DateTimePoint>
        {
            Values = points,
            Fill = null,
            GeometrySize = 20
        };
    }

    private static (DateTimePoint, float) HandleBill(BasicBillModel bill, DateTime dueDateTime, float accountBalance)
    {
        // Todo: This is a hacky and quick way to get income/pay checks added into the chart. 
        // Will need to redesign the app and flow of adding in the incomes.
        if (bill.Name == "income")
            accountBalance += bill.AmountDue;
        else
            accountBalance -= bill.AmountDue;

        DateTime dueDate = new(dueDateTime.Year, dueDateTime.Month, bill.DueDayOfMonth);
        DateTimePoint dateTimePoint = new(dueDate, accountBalance);

        return (dateTimePoint, accountBalance);
    }
}