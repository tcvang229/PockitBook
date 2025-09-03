using System;
using ReactiveUI;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.VisualElements;
using PockitBook.Services;
using System.Threading.Tasks;
using PockitBook.Models;
using System.Collections.Generic;


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

    private async Task UpdateProjectionAsync()
    {
        Series = new ISeries[]
        {
            await RecalculateAccountProjectionAsync()
        };

        this.RaisePropertyChanged(nameof(Series));
    }

    private float? ValidateAccountBalance()
    {
        bool isAccountBalanceValid = float.TryParse(_accountBalance, out float accountBalance);
        if (!isAccountBalanceValid)
            return null;

        return accountBalance;
    }

    private async Task<LineSeries<float>> RecalculateAccountProjectionAsync()
    {
        float? accountBalance = ValidateAccountBalance();
        if (accountBalance is null)
            return new LineSeries<float>();

        IEnumerable<BasicBillModel>? basicBills = await _databaseConnector.GetBasicBillsAsync();
        if (basicBills is null)
            return new LineSeries<float>();

        LineSeries<float> lineSeries = new()
        {
            Fill = null,
            GeometrySize = 20
        };

        List<float> calculatedDataPoints = new();
        foreach (BasicBillModel basicBill in basicBills)
        {
            float result = (float)accountBalance - basicBill.AmountDue;
            calculatedDataPoints.Add(result);
            accountBalance = result;
        }

        lineSeries.Values = calculatedDataPoints;

        return lineSeries;
    }
}