using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class VentaService
{
    private static readonly object _ventaLock = new();
    private readonly VentaRepository _ventaRepo;
    private readonly ProductoRepository _productoRepo;
    private readonly ConfiguracionRepository _configRepo;
    private readonly ProductoConversionService _conversionService;

    public VentaService(VentaRepository ventaRepo, ProductoRepository productoRepo,
        ConfiguracionRepository configRepo, ProductoConversionService conversionService)
    {
        _ventaRepo = ventaRepo;
        _productoRepo = productoRepo;
        _configRepo = configRepo;
        _conversionService = conversionService;
    }

    public Venta ProcesarVenta(List<ItemCarrito> carrito, int? clienteId, string metodoPago, decimal montoRecibido, decimal descuentoGlobal = 0, string? notaCreditoCodigo = null, decimal pagoEfectivo = 0, decimal pagoTarjeta = 0, decimal pagoTransferencia = 0, string? referenciaTransferencia = null)
    {
        if (!carrito.Any()) throw new InvalidOperationException("El carrito está vacío.");

        lock (_ventaLock)
        {
            var impuestoPct = decimal.Parse(_configRepo.GetValor("impuesto_porcentaje") ?? "0");

            var detalles = new List<DetalleVenta>();
            decimal subtotal = 0;

            foreach (var item in carrito)
            {
                var producto = _productoRepo.GetById(item.ProductoId)
                    ?? throw new InvalidOperationException($"Producto ID {item.ProductoId} no encontrado.");

                // Valida stock con lectura fresca (maneja derivados y base automáticamente)
                _conversionService.ValidarStockSuficiente(item.ProductoId, item.Cantidad);

                var lineaSubtotal = item.Cantidad * item.PrecioUnit - item.Descuento;
                subtotal += lineaSubtotal;

                detalles.Add(new DetalleVenta
                {
                    ProductoId = item.ProductoId,
                    ProductoNombre = producto.Nombre,
                    Cantidad = item.Cantidad,
                    PrecioUnit = item.PrecioUnit,
                    Descuento = item.Descuento,
                    Subtotal = lineaSubtotal
                });
            }

            var baseImponible = subtotal - descuentoGlobal;
            var impuesto = Math.Round(baseImponible * (impuestoPct / 100), 2);
            var total = baseImponible + impuesto;
            var cambio = montoRecibido - total;

            if (metodoPago == "NOTA_CREDITO")
            {
                if (string.IsNullOrWhiteSpace(notaCreditoCodigo))
                    throw new InvalidOperationException("Código de Nota de Crédito no proporcionado.");

                var devService = (ComercialPerezGonzales.Services.DevolucionService)App.Services.GetService(typeof(ComercialPerezGonzales.Services.DevolucionService))!;
                var nc = devService.ValidarNotaCredito(notaCreditoCodigo.Trim().ToUpper(), total);
                if (nc == null)
                    throw new InvalidOperationException("Nota de Crédito no válida.");

                devService.ConsumirNotaCredito(notaCreditoCodigo.Trim().ToUpper(), total);
                montoRecibido = total;
                cambio = 0;
            }
            else if (montoRecibido < total && metodoPago == "EFECTIVO")
            {
                throw new InvalidOperationException($"Monto insuficiente. Falta: {total - montoRecibido:F2}");
            }

            // Inicializar desglose si todos son 0 (para compatibilidad de un solo método)
            if (pagoEfectivo == 0 && pagoTarjeta == 0 && pagoTransferencia == 0)
            {
                if (metodoPago == "EFECTIVO") pagoEfectivo = total;
                else if (metodoPago == "TARJETA") pagoTarjeta = total;
                else if (metodoPago == "TRANSFERENCIA") pagoTransferencia = total;
                else if (metodoPago == "NOTA_CREDITO") pagoEfectivo = total; // Tratado como efectivo para reportes
            }

            var venta = new Venta
            {
                Numero = _ventaRepo.GetNextNumero(),
                ClienteId = clienteId,
                Subtotal = subtotal,
                Descuento = descuentoGlobal,
                Impuesto = impuesto,
                Total = total,
                MetodoPago = metodoPago,
                MontoRecibido = montoRecibido,
                Cambio = Math.Max(0, cambio),
                PagoEfectivo = pagoEfectivo,
                PagoTarjeta = pagoTarjeta,
                PagoTransferencia = pagoTransferencia,
                ReferenciaTransferencia = referenciaTransferencia,
                Estado = "COMPLETADA",
                Detalles = detalles
            };

            venta.Id = _ventaRepo.Insert(venta);

            // Descontar stock dentro del lock (derivados descuentan del base)
            foreach (var item in carrito)
                _conversionService.DescontarStock(item.ProductoId, item.Cantidad);

            return venta;
        }
    }

    public Venta ProcesarCotizacion(List<ItemCarrito> carrito, int? clienteId, decimal descuentoGlobal = 0)
    {
        if (!carrito.Any()) throw new InvalidOperationException("El carrito está vacío.");

        lock (_ventaLock)
        {
            var impuestoPct = decimal.Parse(_configRepo.GetValor("impuesto_porcentaje") ?? "0");

            var detalles = new List<DetalleVenta>();
            decimal subtotal = 0;

            foreach (var item in carrito)
            {
                var producto = _productoRepo.GetById(item.ProductoId)
                    ?? throw new InvalidOperationException($"Producto ID {item.ProductoId} no encontrado.");

                // Para cotizaciones NO validamos stock
                var lineaSubtotal = item.Cantidad * item.PrecioUnit - item.Descuento;
                subtotal += lineaSubtotal;

                detalles.Add(new DetalleVenta
                {
                    ProductoId = item.ProductoId,
                    ProductoNombre = producto.Nombre,
                    Cantidad = item.Cantidad,
                    PrecioUnit = item.PrecioUnit,
                    Descuento = item.Descuento,
                    Subtotal = lineaSubtotal
                });
            }

            var baseImponible = subtotal - descuentoGlobal;
            var impuesto = Math.Round(baseImponible * (impuestoPct / 100), 2);
            var total = baseImponible + impuesto;

            var venta = new Venta
            {
                Numero = _ventaRepo.GetNextNumero(),
                ClienteId = clienteId,
                Subtotal = subtotal,
                Descuento = descuentoGlobal,
                Impuesto = impuesto,
                Total = total,
                MetodoPago = "COTIZACION",
                MontoRecibido = 0,
                Cambio = 0,
                Estado = "COTIZACION",
                Detalles = detalles
            };

            // Pasamos descontarStock = false
            venta.Id = _ventaRepo.Insert(venta, descontarStock: false);

            return venta;
        }
    }

    public IEnumerable<Venta> GetByFecha(DateTime desde, DateTime hasta) => _ventaRepo.GetByFecha(desde, hasta);
    public Venta? GetById(int id) => _ventaRepo.GetById(id);
    public (decimal Total, int Cantidad) GetResumenDia(DateTime fecha) => _ventaRepo.GetResumenDia(fecha);
    public void Anular(int id, string motivo) => _ventaRepo.Anular(id, motivo);
}

public class ItemCarrito : System.ComponentModel.INotifyPropertyChanged
{
    private decimal _cantidad;
    private decimal _precioUnit;
    private decimal _descuento;
    private bool _isSelected;

    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "UND";

    public decimal Cantidad
    {
        get => _cantidad;
        set
        {
            if (value <= 0) value = 1;
            _cantidad = value;
            OnCantidadChanged();
        }
    }

    public decimal PrecioUnit
    {
        get => _precioUnit;
        set { _precioUnit = value; OnCantidadChanged(); }
    }

    public decimal Descuento
    {
        get => _descuento;
        set { _descuento = value; OnCantidadChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public bool HasDiscount => Descuento > 0;

    public decimal Subtotal => Cantidad * PrecioUnit - Descuento;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public void OnCantidadChanged()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Cantidad)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Subtotal)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(HasDiscount)));
    }
}
