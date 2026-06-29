using MahApps.Metro.Controls;
using System.Windows.Input;
using System.Windows.Controls;
using ComercialPerezGonzales.ViewModels.POS;

namespace ComercialPerezGonzales.Views.POS;

public partial class PagoWindow : MetroWindow
{
    public PagoWindow(PagoViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CerrarSolicitado += Close;
    }

    private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is PagoViewModel vm)
        {
            // Si el panel de nuevo cliente está abierto, no interferir con las pulsaciones
            if (vm.MostrarPanelNuevoCliente) return;

            // Si el usuario está enfocado en un cuadro de texto (ej. código de nota de crédito), no interferir
            if (e.OriginalSource is TextBox) return;

            // Números del 0 al 9 (Teclado principal y Numpad)
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                e.Handled = true;
                string digit = (e.Key >= Key.D0 && e.Key <= Key.D9) 
                    ? (e.Key - Key.D0).ToString() 
                    : (e.Key - Key.NumPad0).ToString();
                
                if (vm.NumpadCommand.CanExecute(digit))
                {
                    vm.NumpadCommand.Execute(digit);
                }
            }
            // Tecla Backspace (Borrar)
            else if (e.Key == Key.Back)
            {
                e.Handled = true;
                if (vm.BorrarNumpadCommand.CanExecute(null))
                {
                    vm.BorrarNumpadCommand.Execute(null);
                }
            }
            // Tecla Escape (Cancelar)
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                if (vm.CancelarCommand.CanExecute(null))
                {
                    vm.CancelarCommand.Execute(null);
                }
            }
            // Tecla Enter (Confirmar)
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (vm.ConfirmarCommand.CanExecute(null))
                {
                    vm.ConfirmarCommand.Execute(null);
                }
            }
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
        e.Handled = !decimal.TryParse(newText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out _)
                    && !decimal.TryParse(newText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _);
    }
}
