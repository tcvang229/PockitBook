using System.Reactive;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// Main Window view model.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IScreen
{
    /// <summary>
    /// The Router associated with this Screen.
    /// Required by the IScreen interface.
    /// </summary>
    public RoutingState Router { get; } = new RoutingState();

    /// <summary>
    /// The command that navigates a user to first view model.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoNext { get; }

    /// <summary>
    /// The command that navigates a user back.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel?> GoBack => Router.NavigateBack;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindowViewModel()
    {
        GoNext = ReactiveCommand.CreateFromObservable(
            () => Router.Navigate.Execute(new BillTrackerViewModel(this)));

        Router.Navigate.Execute(new BillTrackerViewModel(this));
    }
}
