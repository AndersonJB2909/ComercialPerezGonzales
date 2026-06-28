using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using ComercialPerezGonzales.ViewModels.POS;

namespace ComercialPerezGonzales.Views.POS;

public partial class DevolucionesView : UserControl
{
    public DevolucionesView()
    {
        InitializeComponent();
    }

    private void SupervisorPinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DevolucionesViewModel vm)
        {
            vm.SupervisorPin = ((PasswordBox)sender).Password;
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        string newText = textBox.Text.Substring(0, textBox.SelectionStart) + 
                         e.Text + 
                         textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

        // Permitir solo números enteros o con decimales
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

    private void Limpiar_Click(object sender, RoutedEventArgs e)
    {
        SupervisorPinBox.Password = string.Empty;
    }

    private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb != null) tb.SelectAll();
    }
}
