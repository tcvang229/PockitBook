using System;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// The view model for the Home View.
/// </summary>
public partial class HomeViewModel : ViewModelBase, IRoutableViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    public HomeViewModel(IScreen screen)
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

    public string UrlPathSegment { get; set; } = $"Home page: {Guid.NewGuid().ToString().Substring(0, 5)}";
}