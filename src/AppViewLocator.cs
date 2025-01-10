using System;
using PockitBook.ViewModels;
using PockitBook.Views;
using ReactiveUI;

namespace PockitBook;

/// <summary>
/// Carries the responsibility to choose the right view and view model to display.
/// Reference: https://www.reactiveui.net/docs/handbook/routing.html#view-location
/// </summary>
public class AppViewLocator : IViewLocator
{
    /// <inheritdoc />
    public IViewFor ResolveView<T>(T? viewModel, string? contract = null)
    {
        // When adding a new view, add it to this switch statement so the 
        // AppViewLocator knows what view to display within itself.
        return viewModel switch
        {
            BillDetailsViewModel _viewModel => new BillDetailsView { ViewModel = _viewModel },
            AccountProjectionViewModel _viewModel => new AccountProjectionView { ViewModel = _viewModel },
            HomeViewModel _viewModel => new HomeView { ViewModel = _viewModel },
            _ => throw new Exception("Cannot navigate to page, the page does not exist.")
        };
    }
}