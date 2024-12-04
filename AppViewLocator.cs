using System;
using PockitBook.ViewModels;
using PockitBook.Views;
using ReactiveUI;

namespace PockitBook;

/// <summary>
/// Carries the responsibility to choose the right view and view model to display.
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
            _ => throw new Exception("Cannot navigate to page, the page does not exist.")
        };
    }
}