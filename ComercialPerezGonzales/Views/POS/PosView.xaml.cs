using System.Windows;
using System.Windows.Controls;
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
}
