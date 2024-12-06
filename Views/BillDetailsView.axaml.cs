using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The code-behind for the Bill Details View.
/// </summary>
public partial class BillDetailsView : ReactiveUserControl<BillDetailsViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BillDetailsView()
    {
        AvaloniaXamlLoader.Load(this);

        this.WhenActivated(disposables =>
        {
            ViewModel!.InvokePageLoadedEvent();
        });
    }
}