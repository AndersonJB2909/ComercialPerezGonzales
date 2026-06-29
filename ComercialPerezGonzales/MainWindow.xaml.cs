using MahApps.Metro.Controls;
using System.Windows.Input;
using ComercialPerezGonzales.ViewModels.POS;

namespace ComercialPerezGonzales;

public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SalirButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F10 || (e.Key == Key.System && e.SystemKey == Key.F10))
        {
            if (DataContext is ViewModels.MainViewModel mainVm && mainVm.CurrentView is PosViewModel posVm)
            {
                e.Handled = true;
                if (posVm.ProcesarVentaCommand.CanExecute(null))
                {
                    posVm.ProcesarVentaCommand.Execute(null);
                }
            }
        }
        else if (e.Key == Key.F12)
        {
            if (DataContext is ViewModels.MainViewModel mainVm && mainVm.CurrentView is PosViewModel posVm)
            {
                e.Handled = true;
                if (posVm.IrFlujoCajaCommand.CanExecute(null))
                {
                    posVm.IrFlujoCajaCommand.Execute(null);
                }
            }
        }
    }
}
