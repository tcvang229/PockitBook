using Avalonia.Controls;
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

        AddBillButton = this.FindControl<Button>("AddBillButton");
        DeleteAllBillsButton = this.FindControl<Button>("DeleteAllBillsButton");

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
        this.BindCommand(
            ViewModel,
            viewModel => viewModel.DeleteAllBillsCommand,
            view => view.DeleteAllBillsButton
        );

        this.BindCommand(
            ViewModel,
            viewModel => viewModel.AddBillCommand,
            view => view.AddBillButton
        );
    }
}