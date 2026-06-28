using System.Windows;
using System.Windows.Controls;
using ComercialPerezGonzales.ViewModels.Reportes;
using ComercialPerezGonzales.Views.POS;

namespace ComercialPerezGonzales.Views.Reportes;

public partial class ReportesView : UserControl
{
    public ReportesView()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is ReportesViewModel vm)
            {
                vm.SolicitarReimpresion -= Vm_SolicitarReimpresion;
                vm.SolicitarReimpresion += Vm_SolicitarReimpresion;
            }
        };
    }

    private void Vm_SolicitarReimpresion(ViewModels.POS.ReciboViewModel reciboVm)
    {
        var window = new ReciboWindow(reciboVm) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }
}
