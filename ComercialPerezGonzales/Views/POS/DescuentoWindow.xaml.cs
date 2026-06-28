using System;
using System.Windows;
using MahApps.Metro.Controls;

namespace ComercialPerezGonzales.Views.POS;

public partial class DescuentoWindow : MetroWindow
{
    public bool AplicarAlItem { get; private set; }
    public bool EsPorcentaje { get; private set; }
    public decimal Valor { get; private set; }

    public DescuentoWindow(bool tieneItemSeleccionado, string nombreItem)
    {
        InitializeComponent();

        if (tieneItemSeleccionado)
        {
            RbItem.IsEnabled = true;
            TxtItemNombre.Text = $"Artículo: {nombreItem}";
            TxtItemNombre.Visibility = Visibility.Visible;
            RbItem.IsChecked = true; // Auto-select item if selected in cart
        }
        else
        {
            RbItem.IsEnabled = false;
            TxtItemNombre.Visibility = Visibility.Collapsed;
            RbOrden.IsChecked = true;
        }
        
        TxtValor.Focus();
    }

    private void Aplicar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = string.Empty;

        if (!decimal.TryParse(TxtValor.Text.Trim(), out decimal valor) || valor < 0)
        {
            TxtError.Text = "Ingrese un valor numérico mayor o igual a 0.";
            return;
        }

        bool esPorcentaje = RbPorcentaje.IsChecked == true;
        if (esPorcentaje && valor > 100)
        {
            TxtError.Text = "El porcentaje no puede ser mayor al 100%.";
            return;
        }

        AplicarAlItem = RbItem.IsChecked == true;
        EsPorcentaje = esPorcentaje;
        Valor = valor;

        DialogResult = true;
        Close();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
