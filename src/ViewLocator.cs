using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PockitBook.ViewModels;

namespace PockitBook;

/// <summary>
/// Handles locating the views and their respective view models then loads it to the screen.
/// Reference: https://docs.avaloniaui.net/docs/concepts/view-locator
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Creates the view for the view model.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Control? Build(object? data)
    {
        if (data is null) return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Check if the data object is a view model.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
