using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComercialPerezGonzales.ViewModels;
using ComercialPerezGonzales.ViewModels.POS;

namespace ComercialPerezGonzales.Views.POS;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PosViewModel oldVm)
        {
            oldVm.SolicitarPago -= AbrirPago;
            oldVm.SolicitarRecibo -= AbrirRecibo;
            oldVm.NavigarFlujoCaja -= IrFlujoCaja;
        }
        if (e.NewValue is PosViewModel newVm)
        {
            newVm.SolicitarPago += AbrirPago;
            newVm.SolicitarRecibo += AbrirRecibo;
            newVm.NavigarFlujoCaja += IrFlujoCaja;
        }
    }

    private void AbrirPago(PagoViewModel vm)
    {
        var window = new PagoWindow(vm) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void AbrirRecibo(ReciboViewModel vm)
    {
        var window = new ReciboWindow(vm) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void IrFlujoCaja()
    {
        // Navigate to Flujo de Caja via the parent MainViewModel
        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.NavigateCierreDiaCommand.Execute(null);
        }
    }

    private void CantidadTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is TextBox textBox)
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);
                Keyboard.ClearFocus();
            }
        }
    }

    private void CantidadTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        foreach (char ch in e.Text)
        {
            if (char.IsDigit(ch))
                continue;

            if (ch == '.' || ch == ',')
            {
                var textBox = sender as TextBox;
                if (textBox != null && !textBox.Text.Contains(".") && !textBox.Text.Contains(","))
                    continue;
            }

            e.Handled = true;
            break;
        }
    }

    private void CantidadTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F10 || (e.Key == Key.System && e.SystemKey == Key.F10))
        {
            e.Handled = true;
            if (DataContext is PosViewModel vm && vm.ProcesarVentaCommand.CanExecute(null))
            {
                vm.ProcesarVentaCommand.Execute(null);
            }
        }
        else if (e.Key == Key.F12)
        {
            e.Handled = true;
            if (DataContext is PosViewModel vm && vm.IrFlujoCajaCommand.CanExecute(null))
            {
                vm.IrFlujoCajaCommand.Execute(null);
            }
        }
    }
}
