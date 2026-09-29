using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using PockitBook.Models;
using PockitBook.ViewModels;
using ReactiveUI;

namespace PockitBook.Views;

/// <summary>
/// The code-behind for the Bill Details View.
/// </summary>
public partial class BillDetailsView : ReactiveUserControl<BillDetailsViewModel>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BillDetailsView()
    {
        AvaloniaXamlLoader.Load(this);

        AddBillButton = this.FindControl<Button>("AddBillButton");
        DeleteAllBillsButton = this.FindControl<Button>("DeleteAllBillsButton");
        ImportBillsCsvButton = this.FindControl<Button>("ImportBillsCsvButton");
        ScheduledItemsGrid = this.FindControl<DataGrid>("ScheduledItemsGrid");

        this.WhenActivated(disposables =>
        {
            BindButtons();
            ViewModel!.InvokePageLoadedEvent();
        });
    }

    /// <summary>
    /// Bind all the navigation buttons to their respective command.
    /// </summary>
    private void BindButtons()
    {
        this.BindCommand(
            ViewModel,
            viewModel => viewModel.DeleteAllBillsCommand,
            view => view.DeleteAllBillsButton
        );

        this.BindCommand(
            ViewModel,
            viewModel => viewModel.AddBillCommand,
            view => view.AddBillButton
        );

        if (ScheduledItemsGrid is not null)
            ScheduledItemsGrid.CellEditEnded += OnCellEditEnded;

        ImportBillsCsvButton.Click += OnImportBillsCsvClicked;
    }

    private async void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
            return;

        if (e.Row.DataContext is ScheduledItem item && ViewModel is not null)
            await ViewModel.UpdateBillAsync(item);
    }

    private async void OnImportBillsCsvClicked(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || ViewModel is null)
            return;

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Bills CSV",
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