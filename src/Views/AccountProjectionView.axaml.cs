using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The code-behind for the Bill Details View.
/// </summary>
public partial class AccountProjectionView : ReactiveUserControl<AccountProjectionViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public AccountProjectionView()
    {
        AvaloniaXamlLoader.Load(this);

        this.WhenActivated(disposables =>
        {
            BindButtons();
            ViewModel!.InvokePageLoadedEvent();
        });
    }

    /// <summary>
    /// Bind all the navigation buttons to their respective command.
    /// </summary>
    private void BindButtons()
    {
    }
}