using System.Windows;
using ComercialPerezGonzales.Views.Shared;

namespace ComercialPerezGonzales;

/// <summary>
/// Reemplazo estilizado de MessageBox para toda la aplicación.
/// La API es idéntica a MessageBox.Show para facilitar la migración.
/// </summary>
public static class AppDialog
{
    public static MessageBoxResult Show(
        string message,
        string title = "Información",
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.Information)
    {
        var owner = Application.Current?.MainWindow;
        var dialog = new AppDialogWindow();
        dialog.Configure(message, title, button, icon);

        if (owner != null && owner.IsLoaded)
            dialog.Owner = owner;

        dialog.ShowDialog();
        return dialog.Result;
    }
}
