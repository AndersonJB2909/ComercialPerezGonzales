using System.Collections.ObjectModel;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Reportes;

public class ReportesViewModel : ViewModelBase
{
    private readonly ReporteService _service;
    private readonly VentaService _ventaService;
    private DateTime _desde = DateTime.Today;
    private DateTime _hasta = DateTime.Today;
    private ResumenCaja _resumenCaja = new();

    public ObservableCollection<ResumenVentaDia> VentasPorDia { get; } = new();
    public ObservableCollection<ResumenProducto> TopProductos { get; } = new();

    public DateTime Desde
    {
        get => _desde;
        set { SetProperty(ref _desde, value); Actualizar(); }
    }

    public DateTime Hasta
    {
        get => _hasta;
        set { SetProperty(ref _hasta, value); Actualizar(); }
    }

    public ResumenCaja ResumenCaja
    {
        get => _resumenCaja;
        set => SetProperty(ref _resumenCaja, value);
    }

    public RelayCommand ActualizarCommand { get; }
    public RelayCommand HoyCommand { get; }
    public RelayCommand EsteMesCommand { get; }

    public ReportesViewModel(ReporteService service, VentaService ventaService)
    {
        _service = service;
        _ventaService = ventaService;
        ActualizarCommand = new RelayCommand(Actualizar);
        HoyCommand = new RelayCommand(() => { Desde = DateTime.Today; Hasta = DateTime.Today; });
        EsteMesCommand = new RelayCommand(() =>
        {
            Desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Hasta = DateTime.Today;
        });
        Actualizar();
    }

    private void Actualizar()
    {
        VentasPorDia.Clear();
        foreach (var v in _service.GetVentasPorDia(_desde, _hasta))
            VentasPorDia.Add(v);

        TopProductos.Clear();
        foreach (var p in _service.GetProductosMasVendidos(_desde, _hasta))
            TopProductos.Add(p);

        ResumenCaja = _service.GetResumenCaja(DateTime.Today);
        OnPropertyChanged(nameof(ResumenCaja));
    }
}
