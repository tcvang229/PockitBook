using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The main window that will be displaying other views.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        BackNavButton = this.FindControl<Button>("BackNavButton");
        BillDetailsNavButton = this.FindControl<Button>("BillDetailsNavButton");
        AccountProjectionNavButton = this.FindControl<Button>("AccountProjectionNavButton");

        this.WhenActivated(disposables =>
        {
            BindNavigationButtons();
            ViewModel!.InvokePageLoadedEvent();
        });
    }

    /// <summary>
    /// Bind all the navigation buttons to their respective command.
    /// </summary>
    private void BindNavigationButtons()
    {
        this.BindCommand(
            ViewModel,
            viewModel => viewModel.GoToPreviousView,
            view => view.BackNavButton
        );

        this.BindCommand(
            ViewModel,
            viewModel => viewModel.GoToBillDetailsView,
            view => view.BillDetailsNavButton
        );

        this.BindCommand(
            ViewModel,
            viewModel => viewModel.GoToAccountProjectionView,
            view => view.AccountProjectionNavButton
        );
    }
}