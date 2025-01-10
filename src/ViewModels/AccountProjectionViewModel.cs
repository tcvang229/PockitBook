using System;
using ReactiveUI;

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
    }

    /// <summary>
    /// Reference to IScreen that owns the routable view model.
    /// </summary>
    public IScreen HostScreen { get; }

    /// <summary>
    /// Unique identifier for the routable view model.
    /// </summary>
    public string UrlPathSegment { get; set; } = $"Account Projection page: {Guid.NewGuid().ToString().Substring(0, 5)}";
}