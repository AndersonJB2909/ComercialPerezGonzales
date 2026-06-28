using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace ComercialPerezGonzales.Views.CierreDia;

public partial class CierreDiaView : UserControl
{
    public CierreDiaView()
    {
        InitializeComponent();
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
