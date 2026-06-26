using MahApps.Metro.Controls;
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
}
