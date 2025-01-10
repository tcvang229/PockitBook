using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using PockitBook.Services;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// The Main Window view model. 
/// </summary>
public class MainWindowViewModel : ViewModelBase, IScreen
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="router"></param>
    /// <param name="backButtonManager"></param>
    public MainWindowViewModel(RoutingState router, DataBaseConnector dbConnector, bool isTesting = false)
    {
        GoToBillDetailsView = ReactiveCommand.CreateFromObservable(
            () => NavigateForward(Constants.AppViews.BillDetailsView));

        Router = router;

        _dbConnector = dbConnector;

        if (!isTesting)
            _dbConnector.InitializeDataBaseAsync();
    }

    /// <summary>
    /// The Router associated with this Screen.
    /// Required by the IScreen interface.
    /// </summary>
    public RoutingState Router { get; }

    /// <summary>
    /// Command to navigate to the previous view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoToPreviousView => Router.NavigateBack;

    /// <summary>
    /// Command to navigate to the Bill Details view.
    /// </summary>
    public ReactiveCommand<Unit, IRoutableViewModel> GoToBillDetailsView { get; }

    private readonly DataBaseConnector _dbConnector;

    protected override void OnPageLoadedEventHandler()
    {
        // Show the splash screen.
        Task.Run(async () =>
        {
            var waitTime = 2 * 1000;
            await Task.Delay(waitTime);

            Dispatcher.UIThread.Post(() =>
                NavigateForward(Constants.AppViews.HomeView));
        });
    }

    /// <summary>
    /// Navigates forward to the targeted view.
    /// </summary>
    /// <param name="viewModel"></param>
    /// <returns></returns>
    private IObservable<IRoutableViewModel> NavigateForward(Constants.AppViews viewToNavigate)
    {
        return viewToNavigate switch
        {
            Constants.AppViews.HomeView => Router.Navigate.Execute(new HomeViewModel(this)),
            Constants.AppViews.BillDetailsView => Router.Navigate.Execute(new BillDetailsViewModel(this, _dbConnector)),
            _ => throw new Exception("Cannot navigate to page, the page does not exist.")
        };
    }
}
