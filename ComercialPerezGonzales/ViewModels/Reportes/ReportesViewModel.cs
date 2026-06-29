using System.Collections.ObjectModel;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.ViewModels.Base;
using ComercialPerezGonzales.ViewModels.POS;
using ComercialPerezGonzales.Data.Repositories;

namespace ComercialPerezGonzales.ViewModels.Reportes;

public class ReportesViewModel : ViewModelBase
{
    private readonly ReporteService _service;
    private readonly VentaService _ventaService;
    private readonly ConfiguracionRepository _configRepo;
    
    private DateTime _desde = DateTime.Today;
    private DateTime _hasta = DateTime.Today;
    private ResumenCaja _resumenCaja = new();
    private string _periodoSeleccionado = "Hoy";
    private bool _isUpdatingPeriod;

    public ObservableCollection<Venta> HistorialVentas { get; } = new();
    public ObservableCollection<ResumenProducto> TopProductos { get; } = new();

    public Action<ReciboViewModel>? SolicitarReimpresion { get; set; }
    public Action<Venta>? CargarCotizacionPOS { get; set; }

    public string PeriodoSeleccionado
    {
        get => _periodoSeleccionado;
        set
        {
            if (SetProperty(ref _periodoSeleccionado, value))
            {
                AjustarFechasPorPeriodo(value);
                NotifyPeriodSelectionChanged();
            }
        }
    }

    public bool IsHoySelected => PeriodoSeleccionado == "Hoy";
    public bool IsAyerSelected => PeriodoSeleccionado == "Ayer";
    public bool IsEstaSemanaSelected => PeriodoSeleccionado == "Esta semana";
    public bool IsUltimaSemanaSelected => PeriodoSeleccionado == "Última semana";
    public bool IsEsteMesSelected => PeriodoSeleccionado == "Este mes";
    public bool IsUltimoMesSelected => PeriodoSeleccionado == "Último mes";
    public bool IsEsteAnoSelected => PeriodoSeleccionado == "Este año";
    public bool IsUltimoAnoSelected => PeriodoSeleccionado == "Último año";

    public DateTime Desde
    {
        get => _desde;
        set
        {
            if (SetProperty(ref _desde, value))
            {
                if (!_isUpdatingPeriod)
                {
                    _periodoSeleccionado = "Personalizado";
                    OnPropertyChanged(nameof(PeriodoSeleccionado));
                    NotifyPeriodSelectionChanged();
                }
                Actualizar();
            }
        }
    }

    public DateTime Hasta
    {
        get => _hasta;
        set
        {
            if (SetProperty(ref _hasta, value))
            {
                if (!_isUpdatingPeriod)
                {
                    _periodoSeleccionado = "Personalizado";
                    OnPropertyChanged(nameof(PeriodoSeleccionado));
                    NotifyPeriodSelectionChanged();
                }
                Actualizar();
            }
        }
    }

    public ResumenCaja ResumenCaja
    {
        get => _resumenCaja;
        set => SetProperty(ref _resumenCaja, value);
    }

    public RelayCommand ActualizarCommand { get; }
    public RelayCommand SeleccionarPeriodoCommand { get; }
    public RelayCommand ReimprimirCommand { get; }
    public RelayCommand CargarEnPosCommand { get; }

    public ReportesViewModel(ReporteService service, VentaService ventaService, ConfiguracionRepository configRepo)
    {
        _service = service;
        _ventaService = ventaService;
        _configRepo = configRepo;

        ActualizarCommand = new RelayCommand(Actualizar);
        SeleccionarPeriodoCommand = new RelayCommand(param =>
        {
            if (param is string p)
            {
                PeriodoSeleccionado = p;
            }
        });
        
        ReimprimirCommand = new RelayCommand(param => ReimprimirVenta(param as Venta));
        CargarEnPosCommand = new RelayCommand(param => CargarEnPos(param as Venta));

        PeriodoSeleccionado = "Hoy";
    }

    public void AlMostrar()
    {
        if (PeriodoSeleccionado != "Personalizado")
        {
            AjustarFechasPorPeriodo(PeriodoSeleccionado);
        }
        else
        {
            Actualizar();
        }
    }

    private void AjustarFechasPorPeriodo(string periodo)
    {
        _isUpdatingPeriod = true;
        try
        {
            var hoy = DateTime.Today;
            switch (periodo)
            {
                case "Hoy":
                    Desde = hoy;
                    Hasta = hoy;
                    break;
                case "Ayer":
                    Desde = hoy.AddDays(-1);
                    Hasta = hoy.AddDays(-1);
                    break;
                case "Esta semana":
                    int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
                    Desde = hoy.AddDays(-diff);
                    Hasta = hoy;
                    break;
                case "Última semana":
                    int diffL = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
                    Desde = hoy.AddDays(-diffL - 7);
                    Hasta = Desde.AddDays(6);
                    break;
                case "Este mes":
                    Desde = new DateTime(hoy.Year, hoy.Month, 1);
                    Hasta = Desde.AddMonths(1).AddDays(-1);
                    break;
                case "Último mes":
                    Desde = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1);
                    Hasta = new DateTime(hoy.Year, hoy.Month, 1).AddDays(-1);
                    break;
                case "Este año":
                    Desde = new DateTime(hoy.Year, 1, 1);
                    Hasta = new DateTime(hoy.Year, 12, 31);
                    break;
                case "Último año":
                    Desde = new DateTime(hoy.Year - 1, 1, 1);
                    Hasta = new DateTime(hoy.Year - 1, 12, 31);
                    break;
            }
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
    }

    private void NotifyPeriodSelectionChanged()
    {
        OnPropertyChanged(nameof(IsHoySelected));
        OnPropertyChanged(nameof(IsAyerSelected));
        OnPropertyChanged(nameof(IsEstaSemanaSelected));
        OnPropertyChanged(nameof(IsUltimaSemanaSelected));
        OnPropertyChanged(nameof(IsEsteMesSelected));
        OnPropertyChanged(nameof(IsUltimoMesSelected));
        OnPropertyChanged(nameof(IsEsteAnoSelected));
        OnPropertyChanged(nameof(IsUltimoAnoSelected));
    }

    private void Actualizar()
    {
        HistorialVentas.Clear();
        foreach (var v in _ventaService.GetByFecha(_desde, _hasta))
            HistorialVentas.Add(v);

        TopProductos.Clear();
        foreach (var p in _service.GetProductosMasVendidos(_desde, _hasta))
            TopProductos.Add(p);

        ResumenCaja = _service.GetResumenCaja(DateTime.Today);
        OnPropertyChanged(nameof(ResumenCaja));
    }

    private void ReimprimirVenta(Venta? ventaBase)
    {
        if (ventaBase == null) return;
        
        var venta = _ventaService.GetById(ventaBase.Id);
        if (venta == null) return;

        var reciboVm = new ReciboViewModel
        {
            NumeroVenta   = venta.Numero,
            FechaVenta    = venta.CreatedAt,
            TotalVenta    = venta.Total,
            MontoPagado   = venta.MontoRecibido,
            MetodoPago    = venta.MetodoPago,
            PagoEfectivo  = venta.PagoEfectivo,
            PagoTarjeta   = venta.PagoTarjeta,
            PagoTransferencia = venta.PagoTransferencia,
            ReferenciaTransferencia = venta.ReferenciaTransferencia,
            NombreCliente = string.IsNullOrWhiteSpace(venta.ClienteNombre) ? "Cliente General" : venta.ClienteNombre,
            OrdenId       = venta.Id,
            NombreNegocio = _configRepo.GetValor("negocio_nombre")    ?? string.Empty,
            Direccion     = _configRepo.GetValor("negocio_direccion") ?? string.Empty,
            Telefono      = _configRepo.GetValor("negocio_telefono")  ?? string.Empty,
            Rnc           = _configRepo.GetValor("negocio_rut")       ?? string.Empty,
            NombreUsuario = _configRepo.GetValor("usuario_nombre")    ?? string.Empty,
            Encabezado    = _configRepo.GetValor("imp_encabezado")    ?? string.Empty,
            PiePagina     = _configRepo.GetValor("imp_pie")           ?? string.Empty,
            MonedaSimbolo = _configRepo.GetValor("moneda_simbolo")    ?? "$",
            ImpNombreImpresora = _configRepo.GetValor("imp_impresora")           ?? string.Empty,
            ImpTipoPapel       = _configRepo.GetValor("imp_papel")               ?? "80mm",
            ImpCopias          = int.TryParse(_configRepo.GetValor("imp_copias"), out var cp) ? cp : 1,
            ImpMargenArriba    = int.TryParse(_configRepo.GetValor("imp_margen_arriba"),    out var ma) ? ma : 0,
            ImpMargenAbajo     = int.TryParse(_configRepo.GetValor("imp_margen_abajo"),     out var mb) ? mb : 0,
            ImpMargenIzquierda = int.TryParse(_configRepo.GetValor("imp_margen_izquierda"), out var mi) ? mi : 0,
            ImpMargenDerecha   = int.TryParse(_configRepo.GetValor("imp_margen_derecha"),   out var mr) ? mr : 0,
            ImpFuenteFamilia   = _configRepo.GetValor("imp_fuente_familia")      ?? string.Empty,
            ImpFuenteTamano    = int.TryParse(_configRepo.GetValor("imp_fuente_tamano"), out var ft) ? ft : 100,
        };

        foreach (var d in venta.Detalles)
        {
            reciboVm.CartItems.Add(new ItemCarrito
            {
                ProductoId = d.ProductoId,
                Nombre = d.ProductoNombre ?? "N/A",
                Codigo = d.ProductoCodigo ?? "N/A",
                Cantidad = d.Cantidad,
                PrecioUnit = d.PrecioUnit,
                Descuento = d.Descuento
            });
        }

        SolicitarReimpresion?.Invoke(reciboVm);
    }
    private void CargarEnPos(Venta? cotizacion)
    {
        if (cotizacion == null || cotizacion.Estado != "COTIZACION") return;
        
        var ventaCargada = _ventaService.GetById(cotizacion.Id);
        if (ventaCargada != null && CargarCotizacionPOS != null)
        {
            CargarCotizacionPOS(ventaCargada);
        }
    }
}
