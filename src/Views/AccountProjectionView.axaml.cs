using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The code-behind for the Account Projection View.
/// </summary>
public partial class AccountProjectionView : ReactiveUserControl<AccountProjectionViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public AccountProjectionView()
    {
        AvaloniaXamlLoader.Load(this);

        OverrideBalanceButton = this.FindControl<Button>("OverrideBalanceButton");
        ImportCsvButton = this.FindControl<Button>("ImportCsvButton");

        this.WhenActivated(disposables =>
        {
            BindButtons();
            ViewModel!.InvokePageLoadedEvent();
        });
    }

    /// <summary>
    /// Bind all the buttons to their respective command/handler.
    /// </summary>
    private void BindButtons()
    {
        this.BindCommand(
            ViewModel,
            viewModel => viewModel.OverrideBalanceCommand,
            view => view.OverrideBalanceButton
        );

        ImportCsvButton.Click += OnImportCsvClicked;
    }

    private async void OnImportCsvClicked(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || ViewModel is null)
            return;

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import CSV",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }]
        });

        if (files.Count == 0)
            return;

        string? localPath = files[0].TryGetLocalPath();
        if (localPath is null)
            return;

        await ViewModel.ImportCsvAsync(localPath);
    }
}
