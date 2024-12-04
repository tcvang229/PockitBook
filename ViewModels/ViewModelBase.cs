using ReactiveUI;
using System;

namespace PockitBook.ViewModels;

public class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public ViewModelBase()
    {
        Activator = new ViewModelActivator();
        PageLoaded += OnPageLoadedEventHandler;
    }

    private delegate void PageLoadedEvent();
    private event PageLoadedEvent PageLoaded;

    /// <inheritdoc />
    public ViewModelActivator Activator { get; }

    /// <summary>
    /// Invokes the Page Loaded event.
    /// </summary>
    public void InvokePageLoadedEvent()
    {
        PageLoaded?.Invoke();
    }

    /// <summary>
    /// Event handler for Page Loaded event.
    /// </summary>
    protected virtual async void OnPageLoadedEventHandler()
    {
    }
}
