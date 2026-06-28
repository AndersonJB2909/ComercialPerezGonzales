using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ComercialPerezGonzales.Data;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Dapper;

namespace ComercialPerezGonzales.ViewModels.POS;

public class DevolucionesViewModel : ViewModelBase
{
    private readonly DevolucionService _devService;
    private readonly VentaRepository _ventaRepo;
    private readonly CierreCajaService _cierreService;

    private string _facturaBusqueda = string.Empty;
    private Venta? _ventaEncontrada;
    private string _motivo = "Producto defectuoso";
    private string _motivoOtro = string.Empty;
    private string _metodoReembolso = "EFECTIVO";
    private string _supervisorPin = string.Empty;
    private string _errorMensaje = string.Empty;
    private string _exitoMensaje = string.Empty;
    private string _notaCreditoGenerada = string.Empty;

    public string FacturaBusqueda
    {
        get => _facturaBusqueda;
        set => SetProperty(ref _facturaBusqueda, value);
    }

    public Venta? VentaEncontrada
    {
        get => _ventaEncontrada;
        set
        {
            SetProperty(ref _ventaEncontrada, value);
            OnPropertyChanged(nameof(TieneFactura));
        }
    }

    public bool TieneFactura => VentaEncontrada != null;

    public string Motivo
    {
        get => _motivo;
        set { SetProperty(ref _motivo, value); OnPropertyChanged(nameof(EsOtroMotivo)); }
    }

    public bool EsOtroMotivo => _motivo == "Otro";

    public string MotivoOtro
    {
        get => _motivoOtro;
        set => SetProperty(ref _motivoOtro, value);
    }

    public string MetodoReembolso
    {
        get => _metodoReembolso;
        set => SetProperty(ref _metodoReembolso, value);
    }

    public string SupervisorPin
    {
        get => _supervisorPin;
        set => SetProperty(ref _supervisorPin, value);
    }

    public string ErrorMensaje
    {
        get => _errorMensaje;
        set { SetProperty(ref _errorMensaje, value); OnPropertyChanged(nameof(HayError)); }
    }

    public bool HayError => !string.IsNullOrEmpty(ErrorMensaje);

    public string ExitoMensaje
    {
        get => _exitoMensaje;
        set { SetProperty(ref _exitoMensaje, value); OnPropertyChanged(nameof(HayExito)); }
    }

    public bool HayExito => !string.IsNullOrEmpty(ExitoMensaje);

    public string NotaCreditoGenerada
    {
        get => _notaCreditoGenerada;
        set { SetProperty(ref _notaCreditoGenerada, value); OnPropertyChanged(nameof(TieneNotaCreditoGenerada)); }
    }

    public bool TieneNotaCreditoGenerada => !string.IsNullOrEmpty(NotaCreditoGenerada);

    public decimal TotalDevolucion => Items.Sum(i => i.SubtotalCalculado);

    public ObservableCollection<ItemDevolucionViewModel> Items { get; } = new();
    
    public ObservableCollection<Devolucion> HistorialDevoluciones { get; } = new();

    private Devolucion? _selectedDevolucion;
    public Devolucion? SelectedDevolucion
    {
        get => _selectedDevolucion;
        set => SetProperty(ref _selectedDevolucion, value);
    }

    public RelayCommand BuscarFacturaCommand { get; }
    public RelayCommand ProcesarDevolucionCommand { get; }
    public RelayCommand LimpiarCommand { get; }
    public RelayCommand CopiarValeCommand { get; }
    public RelayCommand CopiarValeHistorialCommand { get; }

    public DevolucionesViewModel(DevolucionService devService, VentaRepository ventaRepo, CierreCajaService cierreService)
    {
        _devService = devService;
        _ventaRepo = ventaRepo;
        _cierreService = cierreService;

        BuscarFacturaCommand = new RelayCommand(BuscarFactura);
        ProcesarDevolucionCommand = new RelayCommand(ProcesarDevolucion);
        LimpiarCommand = new RelayCommand(LimpiarTodo);
        CopiarValeCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(NotaCreditoGenerada))
                System.Windows.Clipboard.SetText(NotaCreditoGenerada);
        }, () => !string.IsNullOrEmpty(NotaCreditoGenerada));
        CopiarValeHistorialCommand = new RelayCommand(() =>
        {
            var codigo = SelectedDevolucion?.NotaCreditoCodigo;
            if (!string.IsNullOrEmpty(codigo))
                System.Windows.Clipboard.SetText(codigo);
        }, () => !string.IsNullOrEmpty(SelectedDevolucion?.NotaCreditoCodigo));

        CargarHistorial();
    }

    private void BuscarFactura()
    {
        ErrorMensaje = string.Empty;
        ExitoMensaje = string.Empty;
        NotaCreditoGenerada = string.Empty;
        VentaEncontrada = null;
        Items.Clear();

        if (string.IsNullOrWhiteSpace(FacturaBusqueda))
        {
            ErrorMensaje = "Ingrese el número de recibo o factura.";
            return;
        }

        Venta? venta = null;
        try
        {
            using var conn = App.Services.GetRequiredService<DatabaseContext>().CreateConnection();
            var searchStr = FacturaBusqueda.Trim().ToUpper();
            
            int? searchId = null;
            var numericPart = searchStr;
            if (numericPart.StartsWith("V-"))
            {
                numericPart = numericPart.Substring(2);
            }
            else if (numericPart.StartsWith("V"))
            {
                numericPart = numericPart.Substring(1);
            }
            
            if (int.TryParse(numericPart, out int parsedId))
            {
                searchId = parsedId;
            }

            var id = conn.ExecuteScalar<int?>(@"
                SELECT id FROM ventas 
                WHERE numero = @searchStr 
                   OR (id = @searchId AND @searchId IS NOT NULL)", 
                new { searchStr, searchId });

            if (id.HasValue)
            {
                venta = _ventaRepo.GetById(id.Value);
            }
        }
        catch (Exception ex)
        {
            ErrorMensaje = $"Error al buscar: {ex.Message}";
            return;
        }

        if (venta == null)
        {
            ErrorMensaje = $"No se encontró ninguna factura con el número '{FacturaBusqueda}'.";
            return;
        }

        if (venta.Estado == "ANULADA")
        {
            ErrorMensaje = "Esta venta ya fue ANULADA.";
            return;
        }

        if (venta.MetodoPago == "COTIZACION")
        {
            ErrorMensaje = "No se pueden realizar devoluciones sobre cotizaciones.";
            return;
        }

        VentaEncontrada = venta;

        var returnedQuantities = new Dictionary<int, decimal>();
        try
        {
            using var conn = App.Services.GetRequiredService<DatabaseContext>().CreateConnection();
            var list = conn.Query<dynamic>(@"
                SELECT dd.producto_id, SUM(dd.cantidad) as total_qty
                FROM detalle_devoluciones dd
                JOIN devoluciones d ON dd.devolucion_id = d.id
                WHERE d.venta_id = @ventaId
                GROUP BY dd.producto_id", new { ventaId = venta.Id });

            foreach (var r in list)
            {
                returnedQuantities[Convert.ToInt32(r.producto_id)] = Convert.ToDecimal(r.total_qty);
            }
        }
        catch {}

        foreach (var det in venta.Detalles)
        {
            decimal yaDevuelto = returnedQuantities.ContainsKey(det.ProductoId) ? returnedQuantities[det.ProductoId] : 0;
            if (yaDevuelto >= det.Cantidad) continue;

            var itemVm = new ItemDevolucionViewModel(this)
            {
                ProductoId = det.ProductoId,
                Codigo = det.ProductoCodigo ?? string.Empty,
                Nombre = det.ProductoNombre ?? string.Empty,
                CantidadComprada = det.Cantidad,
                CantidadYaDevuelta = yaDevuelto,
                PrecioUnit = det.PrecioUnit,
                VolverAStock = true
            };
            Items.Add(itemVm);
        }

        if (!Items.Any())
        {
            ErrorMensaje = "Todos los productos de esta factura ya han sido devueltos.";
            VentaEncontrada = null;
        }
    }

    private void ProcesarDevolucion()
    {
        ErrorMensaje = string.Empty;
        ExitoMensaje = string.Empty;
        NotaCreditoGenerada = string.Empty;

        if (VentaEncontrada == null) return;

        var itemsADevolver = Items.Where(i => i.Seleccionado).ToList();
        if (!itemsADevolver.Any())
        {
            ErrorMensaje = "Debe seleccionar al menos un producto para devolver.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SupervisorPin))
        {
            ErrorMensaje = "Ingrese el PIN de supervisor para autorizar.";
            return;
        }

        if (!_devService.ValidarPINSupervisor(SupervisorPin))
        {
            ErrorMensaje = "PIN de supervisor incorrecto.";
            return;
        }

        var jornada = _cierreService.GetJornadaHoy();
        if (jornada == null || jornada.EstaCerrado)
        {
            ErrorMensaje = "Debe tener una jornada de caja ABIERTA hoy para procesar devoluciones.";
            return;
        }

        string motivoFinal = EsOtroMotivo ? MotivoOtro : Motivo;
        if (string.IsNullOrWhiteSpace(motivoFinal))
        {
            ErrorMensaje = "El motivo de la devolución es obligatorio.";
            return;
        }

        try
        {
            var itemsServiceFormat = itemsADevolver.Select(i => (
                i.ProductoId,
                (double)i.CantidadADevolver,
                i.VolverAStock ? "STOCK" : "MERMA"
            )).ToList();

            var dev = _devService.ProcesarDevolucion(
                VentaEncontrada.Id,
                jornada.Id,
                motivoFinal,
                MetodoReembolso,
                SupervisorPin,
                "Cajero",
                itemsServiceFormat
            );

            ExitoMensaje = $"✓ Devolución procesada correctamente por un total de {dev.MontoTotal:C2}.";
            if (MetodoReembolso == "NOTA_CREDITO")
            {
                NotaCreditoGenerada = dev.NotaCreditoCodigo ?? string.Empty;
            }

            SupervisorPin = string.Empty;
            MotivoOtro = string.Empty;
            
            CargarHistorial();
            BuscarFactura();
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
        }
    }

    private void LimpiarTodo()
    {
        FacturaBusqueda = string.Empty;
        VentaEncontrada = null;
        Items.Clear();
        ErrorMensaje = string.Empty;
        ExitoMensaje = string.Empty;
        NotaCreditoGenerada = string.Empty;
        SupervisorPin = string.Empty;
        Motivo = "Producto defectuoso";
        MotivoOtro = string.Empty;
        MetodoReembolso = "EFECTIVO";
    }

    public void CargarHistorial()
    {
        try
        {
            HistorialDevoluciones.Clear();
            var list = _devService.ObtenerTodas();
            foreach (var d in list)
            {
                HistorialDevoluciones.Add(d);
            }
        }
        catch (Exception ex)
        {
            ErrorMensaje = $"Error al cargar historial: {ex.Message}";
        }
    }

    public void RaiseTotalChanged()
    {
        OnPropertyChanged(nameof(TotalDevolucion));
    }
}

public class ItemDevolucionViewModel : ViewModelBase
{
    private readonly DevolucionesViewModel _parent;
    private bool _seleccionado;
    private decimal _cantidadADevolver;
    private bool _volverAStock = true;

    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadComprada { get; set; }
    public decimal CantidadYaDevuelta { get; set; }
    public decimal CantidadDisponible => CantidadComprada - CantidadYaDevuelta;
    public decimal PrecioUnit { get; set; }

    public bool Seleccionado
    {
        get => _seleccionado;
        set
        {
            if (SetProperty(ref _seleccionado, value))
            {
                CantidadADevolver = value ? CantidadDisponible : 0;
            }
        }
    }

    public decimal CantidadADevolver
    {
        get => _cantidadADevolver;
        set
        {
            if (value < 0) value = 0;
            if (value > CantidadDisponible) value = CantidadDisponible;

            SetProperty(ref _cantidadADevolver, value);
            OnPropertyChanged(nameof(SubtotalCalculado));
            _parent.RaiseTotalChanged();
        }
    }

    public bool VolverAStock
    {
        get => _volverAStock;
        set => SetProperty(ref _volverAStock, value);
    }

    public decimal SubtotalCalculado => CantidadADevolver * PrecioUnit;

    public ItemDevolucionViewModel(DevolucionesViewModel parent) => _parent = parent;
}
