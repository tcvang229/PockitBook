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
        this.WhenActivated(disposables =>
        {
            // Set up event handlers here?
            // Whatever that needs to be disposed of when
            // leaving MainWindow, add it to disposables
        });
        AvaloniaXamlLoader.Load(this);
    }
}