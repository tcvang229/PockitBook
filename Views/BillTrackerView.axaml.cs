using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The Bill Tracker view code-behind.
/// </summary>
public partial class BillTrackerView : ReactiveUserControl<BillTrackerViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BillTrackerView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}