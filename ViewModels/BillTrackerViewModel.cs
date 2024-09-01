using System;
using ReactiveUI;

namespace PockitBook.ViewModels;

/// <summary>
/// The view model for the Bill Tracker view.
/// </summary>
public partial class BillTrackerViewModel : ViewModelBase, IRoutableViewModel
{
    /// <summary>
    /// Reference to IScreen that owns the routable view model.
    /// </summary>
    public IScreen HostScreen { get; }

    /// <summary>
    /// Unique identifier for the routable view model.
    /// </summary>
    public string UrlPathSegment { get; set; } = Guid.NewGuid().ToString().Substring(0, 5);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    public BillTrackerViewModel(IScreen screen)
    {
        HostScreen = screen;
    }
}