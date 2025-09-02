using System;
using ReactiveUI;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.VisualElements;
using System.Reflection.Emit;
using LiveChartsCore.Defaults;


namespace PockitBook.ViewModels;

public partial class AccountProjectionViewModel : ViewModelBase, IRoutableViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="dbConnector"></param>
    public AccountProjectionViewModel(IScreen screen)
    {
        HostScreen = screen;
        new ObservablePoint(1, 1);
    }

    /// <summary>
    /// Reference to IScreen that owns the routable view model.
    /// </summary>
    public IScreen HostScreen { get; }

    /// <summary>
    /// Unique identifier for the routable view model.
    /// </summary>
    public string UrlPathSegment { get; set; } = $"Account Projection page: {Guid.NewGuid().ToString().Substring(0, 5)}";

    public ISeries[] Series { get; set; } =
    [
        new LineSeries<double>
        {
            Values = [2, 1, 3, 5, 10],
            Fill = null,
            GeometrySize = 20
        }
    ];

    public LabelVisual Title { get; set; } = new LabelVisual
    {
        Text = "My Chart Title",
        TextSize = 25,
        Padding = new LiveChartsCore.Drawing.Padding(10)
    };
}