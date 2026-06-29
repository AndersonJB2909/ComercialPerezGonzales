using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using ComercialPerezGonzales.ViewModels.CierreDia;

namespace ComercialPerezGonzales.Views.CierreDia;

public partial class CierreDiaView : UserControl
{
    public CierreDiaView()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is CierreDiaViewModel oldVm)
            {
                oldVm.SolicitarImpresionCierre -= Vm_SolicitarImpresionCierre;
            }
            if (e.NewValue is CierreDiaViewModel newVm)
            {
                newVm.SolicitarImpresionCierre -= Vm_SolicitarImpresionCierre;
                newVm.SolicitarImpresionCierre += Vm_SolicitarImpresionCierre;
            }
        };
    }

    private void Vm_SolicitarImpresionCierre(ComercialPerezGonzales.ViewModels.CierreDia.CierreCajaReportViewModel reportVm)
    {
        var window = new CierreCajaReportWindow(reportVm) { Owner = System.Windows.Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        string newText = textBox.Text.Substring(0, textBox.SelectionStart) + 
                         e.Text + 
                         textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

        // Permitir solo números con hasta un punto o una coma decimal y hasta 2 decimales
        bool isNumeric = Regex.IsMatch(newText, @"^\d*([.,]\d{0,2})?$");
        e.Handled = !isNumeric;
    }

    private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }
}
