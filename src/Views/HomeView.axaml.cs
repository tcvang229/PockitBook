using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The code-behind for the Home View.
/// </summary>
public partial class HomeView : ReactiveUserControl<HomeViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public HomeView()
    {
        AvaloniaXamlLoader.Load(this);

        this.WhenActivated(disposables =>
        {
            ViewModel!.InvokePageLoadedEvent();
        });
    }
}