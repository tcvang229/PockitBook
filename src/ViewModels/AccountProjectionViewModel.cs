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
    public ICartesianAxis[] XAxes { get; set; } =
    [
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
        Text = "Account Projection Chart",
        TextSize = 25,
        Padding = new LiveChartsCore.Drawing.Padding(10)
    };

    private string _accountBalance = string.Empty;

    private DataBaseConnector _databaseConnector;

    private const int _searchProjectionWindow = 2;

    private DateTime _today = DateTime.Today;

    private async Task UpdateProjectionAsync()
    {
        LineSeries<DateTimePoint> lineSeries = await RecalculateAccountProjectionAsync();
        Series = new ISeries[]
        {
            lineSeries
        };

        XAxes =
        [
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
        IEnumerable<BasicBillModel>? basicBills = await _databaseConnector.GetBasicBillsAsync();
        if (basicBills is null)
            return new LineSeries<DateTimePoint>();

        List<DateTimePoint> linePoints = BuildLinePoints(basicBills);

        return new LineSeries<DateTimePoint>
        {
            Values = linePoints,
            Fill = null,
            GeometrySize = 20
        };
    }

    private List<DateTimePoint> BuildLinePoints(IEnumerable<BasicBillModel> basicBills)
    {
        float? accountBalance = ValidateAccountBalance();
        if (accountBalance is null)
            return [];

        IOrderedEnumerable<BasicBillModel> billsOrdered = basicBills.OrderBy(b => b.DueDayOfMonth);

        List<DateTimePoint> points = new()
        {
            // Start off with today
            new DateTimePoint
            (
                new DateTime(_today.Year, _today.Month, _today.Day),
                accountBalance
            )
        };

        // Building the rest of the line points.
        // Build the rest of the line points.
        for (int i = 0; i < _searchProjectionWindow; i++)
        {
            int month = (_today.Month + i - 1) % 12 + 1;
            int year = _today.Year + (_today.Month + i - 1) / 12;

            foreach (BasicBillModel? bill in billsOrdered)
            {
                var billDueDate = new DateTime(year, month, bill.DueDayOfMonth);
                var dateTimePoint = BuildSinglePoint(bill, billDueDate, accountBalance.Value);

                if (dateTimePoint is null)
                    continue;

                points.Add(dateTimePoint);
                accountBalance = (float)dateTimePoint.Value!;
            }
        }


        return points;
    }


    private DateTimePoint? BuildSinglePoint(BasicBillModel bill, DateTime billDueDate, float accountBalance)
    {
        if (billDueDate < _today)
            return null;

        // Todo: This is a hacky and quick way to get income/pay checks added into the chart. 
        // Will need to redesign the app and flow of adding in the incomes.
        if (bill.Name == "income")
            accountBalance += bill.AmountDue;
        else
            accountBalance -= bill.AmountDue;

        DateTimePoint dateTimePoint = new(billDueDate, accountBalance);
        return dateTimePoint;
    }
}