using System.Collections.ObjectModel;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Tablero;

public enum PeriodoGrafico { Diario, Semanal, Mensual, Anual }

public class TableroViewModel : ViewModelBase
{
    private readonly ReporteService _service;
    private readonly VentaService _ventaService;
    private readonly ProductoService _productoService;
    private int _anioSeleccionado = DateTime.Today.Year;
    private ResumenCaja _resumenHoy = new();
    private decimal _totalMes;
    private decimal _totalAnio;
    private PeriodoGrafico _periodo = PeriodoGrafico.Mensual;

    public ObservableCollection<VentaMensual> VentasMensuales { get; } = new();
    public ObservableCollection<ResumenProducto> TopProductos { get; } = new();
    public ObservableCollection<ResumenCliente> TopClientes { get; } = new();
    public ObservableCollection<Producto> AlertasVencimiento { get; } = new();

    public int AnioSeleccionado
    {
        get => _anioSeleccionado;
        set { SetProperty(ref _anioSeleccionado, value); ActualizarGrafico(); }
    }

    public PeriodoGrafico Periodo
    {
        get => _periodo;
        set
        {
            SetProperty(ref _periodo, value);
            OnPropertyChanged(nameof(TituloGrafico));
            OnPropertyChanged(nameof(EtiquetaPeriodo));
            OnPropertyChanged(nameof(EsDiario));
            OnPropertyChanged(nameof(EsSemanal));
            OnPropertyChanged(nameof(EsMensual));
            OnPropertyChanged(nameof(EsAnual));
            ActualizarGrafico();
        }
    }

    public string TituloGrafico => _periodo switch
    {
        PeriodoGrafico.Diario   => "Ventas Últimos 30 Días",
        PeriodoGrafico.Semanal  => $"Ventas Semanales {_anioSeleccionado}",
        PeriodoGrafico.Anual    => $"Ventas Anuales ({_anioSeleccionado - 4}–{_anioSeleccionado})",
        _                       => $"Ventas Mensuales {_anioSeleccionado}",
    };

    public string EtiquetaPeriodo => _periodo switch
    {
        PeriodoGrafico.Diario   => "Día",
        PeriodoGrafico.Semanal  => "Semana",
        PeriodoGrafico.Anual    => "Año",
        _                       => "Mes",
    };

    public bool EsDiario  => _periodo == PeriodoGrafico.Diario;
    public bool EsSemanal => _periodo == PeriodoGrafico.Semanal;
    public bool EsMensual => _periodo == PeriodoGrafico.Mensual;
    public bool EsAnual   => _periodo == PeriodoGrafico.Anual;

    public ResumenCaja ResumenHoy
    {
        get => _resumenHoy;
        set => SetProperty(ref _resumenHoy, value);
    }

    public decimal TotalMes
    {
        get => _totalMes;
        set => SetProperty(ref _totalMes, value);
    }

    public decimal TotalAnio
    {
        get => _totalAnio;
        set => SetProperty(ref _totalAnio, value);
    }

    public RelayCommand AnioAnteriorCommand { get; }
    public RelayCommand AnioSiguienteCommand { get; }
    public RelayCommand ActualizarCommand { get; }
    public RelayCommand PeriodoDiarioCommand   { get; }
    public RelayCommand PeriodoSemanalCommand  { get; }
    public RelayCommand PeriodoMensualCommand  { get; }
    public RelayCommand PeriodoAnualCommand    { get; }

    public TableroViewModel(ReporteService service, VentaService ventaService, ProductoService productoService)
    {
        _service = service;
        _ventaService = ventaService;
        _productoService = productoService;
        AnioAnteriorCommand    = new RelayCommand(() => AnioSeleccionado--);
        AnioSiguienteCommand   = new RelayCommand(() => AnioSeleccionado++);
        ActualizarCommand      = new RelayCommand(Actualizar);
        PeriodoDiarioCommand   = new RelayCommand(() => Periodo = PeriodoGrafico.Diario);
        PeriodoSemanalCommand  = new RelayCommand(() => Periodo = PeriodoGrafico.Semanal);
        PeriodoMensualCommand  = new RelayCommand(() => Periodo = PeriodoGrafico.Mensual);
        PeriodoAnualCommand    = new RelayCommand(() => Periodo = PeriodoGrafico.Anual);
        Actualizar();
    }

    private void ActualizarGrafico()
    {
        VentasMensuales.Clear();
        var datos = _periodo switch
        {
            PeriodoGrafico.Diario  => _service.GetVentasPorDiaReciente(),
            PeriodoGrafico.Semanal => _service.GetVentasPorSemana(_anioSeleccionado),
            PeriodoGrafico.Anual   => _service.GetVentasPorAnio(_anioSeleccionado),
            _                      => _service.GetVentasPorMes(_anioSeleccionado),
        };
        foreach (var v in datos) VentasMensuales.Add(v);
        TotalAnio = VentasMensuales.Sum(v => v.TotalVentas);
        OnPropertyChanged(nameof(TituloGrafico));
    }

    public void Actualizar()
    {
        ActualizarGrafico();

        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        TopProductos.Clear();
        foreach (var p in _service.GetProductosMasVendidos(inicioMes, DateTime.Today))
            TopProductos.Add(p);

        TopClientes.Clear();
        foreach (var c in _service.GetTopClientes(inicioMes, DateTime.Today))
            TopClientes.Add(c);

        ResumenHoy = _service.GetResumenCaja(DateTime.Today);
        TotalMes = _service.GetVentasPorDia(inicioMes, DateTime.Today).Sum(v => v.TotalVentas);

        AlertasVencimiento.Clear();
        int diasThreshold = (DateTime.Today.AddMonths(3) - DateTime.Today).Days;
        foreach (var p in _productoService.GetProductosPorVencer(diasThreshold))
            AlertasVencimiento.Add(p);
    }
}
