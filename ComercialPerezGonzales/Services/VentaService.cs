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

    public Venta ProcesarVenta(List<ItemCarrito> carrito, int? clienteId, string metodoPago, decimal montoRecibido, decimal descuentoGlobal = 0)
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

            if (montoRecibido < total && metodoPago == "EFECTIVO")
                throw new InvalidOperationException($"Monto insuficiente. Falta: {total - montoRecibido:F2}");

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

    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "UND";

    public decimal Cantidad
    {
        get => _cantidad;
        set { _cantidad = value; OnCantidadChanged(); }
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

    public decimal Subtotal => Cantidad * PrecioUnit - Descuento;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public void OnCantidadChanged()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Cantidad)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Subtotal)));
    }
}
