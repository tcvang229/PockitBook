using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// Main Window view model.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IScreen
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="router"></param>
    /// <param name="backButtonManager"></param>
    public MainWindowViewModel(RoutingState router)
    {
        GoToBillDetailsView = ReactiveCommand.CreateFromObservable(
            () => NavigateForward(new BillDetailsViewModel(this)));

        Router = router;
    }

    /// <summary>
    /// The Router associated with this Screen.
    /// Required by the IScreen interface.
    /// </summary>
    public RoutingState Router { get; }

    /// <summary>
    /// Command to navigate to the previous view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel?> GoToPreviousView => Router.NavigateBack;

    /// <summary>
    /// Command to navigate to the Bill Details view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoToBillDetailsView { get; }

    protected override async void OnPageLoadedEventHandler()
    {
        var waitTime = 2 * 1000;
        await Task.Delay(waitTime);
        NavigateForward(new BillDetailsViewModel(this));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewModel"></param>
    /// <returns></returns>
    private IObservable<IRoutableViewModel> NavigateForward(BillDetailsViewModel viewModel)
    {
        return Router.Navigate.Execute(viewModel);
    }
}
