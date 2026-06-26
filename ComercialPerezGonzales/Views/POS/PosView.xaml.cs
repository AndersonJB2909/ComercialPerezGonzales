using System.Windows;
using System.Windows.Controls;
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
        }
        if (e.NewValue is PosViewModel newVm)
        {
            newVm.SolicitarPago += AbrirPago;
            newVm.SolicitarRecibo += AbrirRecibo;
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
}
